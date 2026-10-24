# Wizer — Autenticación

ASP.NET Core con JWT en cookies HttpOnly, login con Google y cuentas de docente creadas por invitación.

## Objetivo

Autenticación segura sin exponer los tokens a JavaScript, con verificación de correo obligatoria e identidad única por usuario (el proyecto anterior la partía entre Supabase y la base local).

## Tipos de cuenta

| Cuenta | Cómo se crea |
|--------|--------------|
| Alumno | Registro público con correo y contraseña, o login con Google |
| Docente | Un administrador lo invita; el docente establece su contraseña con un enlace de un solo uso |
| Administrador | Lo crea otro administrador (por invitación). El primero se siembra desde variables de entorno |

**Nunca** aceptar un rol enviado por el cliente en el registro.

## Cookies

| Cookie | Contenido | Path | Vida |
|--------|-----------|------|------|
| `wz_access` | JWT de acceso | `/` | 15 min |
| `wz_refresh` | Token de refresh opaco | `/api/v1/auth` | 7 días, o 30 con "recordarme" |

- `HttpOnly`, `Secure` y `SameSite=Strict`.
- Un solo origen tras el reverse proxy (`architecture.md`): sin CORS en producción. En desarrollo, el proxy de Vite mantiene el mismo origen.
- La cookie `wz_access` también autentica el handshake de SignalR.
- No desactivar `Secure` en producción para resolver problemas de despliegue.

## Access token

- JWT firmado (HS256 con una clave de 256 bits o más desde un secreto).
- Claims: `sub` (user id), `role` (rol de plataforma), `inst` (institution id), `jti`.
- **No** incluye clases ni roles de clase: la pertenencia se resuelve en cada Service.

## Refresh token

- 32 bytes aleatorios en Base64Url. En la base solo se guarda su **hash SHA-256** (`refresh_tokens`).
- **Rotación** en cada refresh: el token anterior queda revocado y enlazado al nuevo (`replaced_by_id`).
- **Detección de reutilización**: si llega un token ya rotado, se revoca toda la familia de ese token y el usuario debe volver a iniciar sesión.
- Logout: revoca el token actual y borra las cookies.

## Registro (alumno)

`POST /api/v1/auth/register` `{ fullName, email, password, campusId }`

- Responde **siempre `202` sin cuerpo**, exista o no el correo, para no revelar qué cuentas existen.
- Si la cuenta es nueva, o existe pero no está verificada, envía un código de verificación.
- Contraseña: 10 caracteres como mínimo, al menos una letra y un número, y que no sea igual al correo.

## Verificación de correo

Una cuenta sin verificar no puede iniciar sesión (`users.email_verified_at` en `null`).

- Login con la contraseña correcta y sin verificar: `403 EMAIL_NOT_VERIFIED`, se envía un código y no se emiten cookies. Con la contraseña incorrecta responde el `401` de siempre.
- `POST /auth/verify-email` `{ email, code, password }` verifica e inicia sesión (`200` + cookies).
  - Código incorrecto, vencido o usado: `400 INVALID_VERIFICATION_CODE`.
  - Código válido con otra contraseña: `400 VERIFICATION_PASSWORD_MISMATCH`, y la UI ofrece recuperar la contraseña. Así, quien registró un correo ajeno no puede quedarse con la cuenta.
- `POST /auth/resend-verification` `{ email }` responde siempre `202`.

### Reglas del código (verificación y recuperación)

- 6 dígitos. En la base solo se guarda su hash SHA-256, ligado al usuario y al propósito (`verification_codes`).
- Vence en 15 minutos y admite 5 intentos. Cada intento se reserva de forma atómica antes de comparar, y la comparación es en tiempo constante.
- Hay un solo código activo por usuario y propósito. Para reenviar hay que esperar 60 segundos.
- Es de un solo uso.

## Login con Google

```text
Frontend: Google Identity Services → credential (ID token)
→ POST /api/v1/auth/google { idToken, rememberMe }
Backend: valida la firma, `aud` = GoogleClientId, `iss`, la expiración y `email_verified = true`
```

- Si existe un `external_logins` con ese `sub` de Google: inicia sesión.
- Si no existe, pero hay un usuario con ese correo: se vincula **solo** si el correo de la cuenta local ya estaba verificado. Si no, `409 EMAIL_ALREADY_EXISTS` y se pide iniciar sesión con contraseña primero.
- Si no existe ninguno: crea una cuenta de **alumno**, con el correo verificado (lo garantiza Google) y sin contraseña.
- Una cuenta de docente o administrador puede vincular Google desde su perfil, pero **nunca se crea** una cuenta docente con Google.
- `GOOGLE_TOKEN_INVALID` si el token no valida.

## Invitación de docentes y administradores

1. El administrador llama a `POST /admin/users` `{ fullName, email, role: teacher|admin, campusId }`.
2. Se crea el usuario inactivo, sin contraseña, y un `invitation_tokens` de un solo uso (32 bytes, se guarda el hash) que vence en 72 h.
3. Se envía un correo con el enlace `/{FRONTEND_URL}/activar?token=…`.
4. `POST /auth/set-password` `{ token, password }` establece la contraseña, marca el correo como verificado, activa la cuenta e inicia sesión.
5. El administrador puede reenviar la invitación, lo que invalida la anterior.

## Recuperación de contraseña

- `POST /auth/forgot-password` `{ email }` responde siempre `202`. Si la cuenta existe y está activa, envía un código. Un fallo de envío solo se registra en el log.
- `POST /auth/reset-password` `{ email, code, newPassword }` responde `204`, o `400 INVALID_RESET_CODE`.
- Un reset exitoso: revoca **todos** los refresh tokens, marca el correo como verificado y desbloquea el limitador de login. No inicia sesión.
- El envío del correo se hace en segundo plano, para no revelar por tiempos si la cuenta existe.

## Cambio de contraseña con sesión

`POST /me/password` `{ currentPassword, newPassword }`: revoca los demás refresh tokens y emite uno nuevo para la sesión actual.

## Renovación de sesión (frontend)

```text
API → 401
→ un único POST /auth/refresh compartido entre las peticiones concurrentes
→ éxito: repite la petición original una vez
→ fallo: limpia la caché de sesión y redirige a /ingresar?next=…
```

La conexión de SignalR, si se corta por un token vencido, se reintenta después del refresh.

## Bloqueo por intentos fallidos

Reglas:

- 5 fallos por correo dentro de una ventana de 5 minutos → `429 TOO_MANY_LOGIN_ATTEMPTS` hasta que la ventana termina.
- El conteo es **por correo** (normalizado: trim y minúsculas), exista o no la cuenta.
- Cada intento se reserva de forma atómica antes de verificar la contraseña.
- Los contadores viven en memoria (una sola instancia) y se reinician con el proceso.
- El abuso por IP contra muchas cuentas se limita en el reverse proxy (`security.md`).

## CSRF

- Cookies `SameSite=Strict` y un solo origen. Sin CORS en producción.
- Sin token antiforgery. Es un riesgo aceptado: si en el futuro hay subdominios no confiables bajo el mismo dominio, agregar validación de `Origin` en las mutaciones.

## Frontend

- TanStack Query: `useMe()` (`GET /auth/me`) es la fuente de la sesión.
- Zustand: solo datos no sensibles derivados (para preferencias de UI). Nunca tokens.
- Axios: `withCredentials: true`, refresh centralizado y nunca imprimir cookies.
- Google: el botón de Google Identity Services se carga solo en las pantallas de acceso.

## Pruebas mínimas

- Registro: responde `202` aunque el correo exista, y nunca acepta un rol.
- Login correcto, credenciales incorrectas, cuenta sin verificar y cuenta inactiva.
- Cookies con los flags esperados.
- Refresh válido, rotado, reutilizado (revoca la familia) y vencido.
- Logout.
- Códigos: incorrecto, vencido, reutilizado e intentos agotados.
- Google: token inválido, cuenta nueva (alumno), vinculación con correo verificado y conflicto con correo no verificado.
- Invitación: token válido, vencido y reutilizado.
- Bloqueo después de 5 fallos.
- Endpoint protegido sin sesión → 401. Rol incorrecto → 403.
- El handshake de SignalR sin cookie se rechaza.
