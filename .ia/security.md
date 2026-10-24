# Wizer — Seguridad

## 1. Principios

1. **La identidad sale de la sesión.** El backend nunca acepta un `userId`, un `role` o un `teacherId` del cliente para decidir quién actúa.
2. **Autorización en el backend**, por rol y por pertenencia, en cada endpoint y en cada método de hub.
3. **Mínimo privilegio y negar por defecto**: los controllers y los hubs llevan `[Authorize]` por defecto.
4. **No revelar información**: 404 para los recursos de otras clases, y respuestas idénticas en registro y recuperación de contraseña.
5. **Defensa en profundidad** para los archivos: tipo, bytes mágicos, antivirus y URLs firmadas de vida corta.

## 2. Secretos

- Nunca en código, en el repositorio, en logs ni en el bundle del frontend.
- Variables de entorno, con `.env` local ignorado por git. `.env.example` documenta todas las variables, con valores ficticios.
- Secretos de la aplicación: clave de firma de JWT, credenciales de PostgreSQL, credenciales de Garage, API key de Resend y client secret de Google (el client ID es público).
- En el frontend solo van variables `VITE_*` públicas, como `VITE_GOOGLE_CLIENT_ID`. Nunca un secreto con el prefijo `VITE_`.
- **trufflehog** se ejecuta en CI sobre cada PR (`infrastructure.md`).

## 3. Logging

Ver `observability.md`. Nunca registrar: contraseñas, tokens, cookies, headers `Authorization`, códigos de verificación, connection strings, contenido de mensajes del chat, escenas de pizarra ni contenido de archivos.

## 4. Validación de entrada

- FluentValidation en todos los requests.
- Longitudes máximas en todos los textos (también en la base).
- Los enums se validan contra los valores permitidos.
- Los enlaces de las entregas solo aceptan `https://` y se muestran con `rel="noopener noreferrer"`.

## 5. Contenido generado por usuarios (XSS)

- El chat y las consignas son **texto plano con formato mínimo**: saltos de línea, enlaces autodetectados y menciones. **No se renderiza HTML ni Markdown arbitrario.**
- React escapa por defecto: **prohibido `dangerouslySetInnerHTML`** con contenido de usuarios.
- Los nombres de archivo se muestran como texto. En las descargas se envían con `Content-Disposition` codificado (RFC 6266).

## 6. Archivos

Ver `files.md`. Resumen: lista blanca de tipos, bytes mágicos, sin SVG, ClamAV, claves de objeto sin el nombre original, bucket privado y URLs firmadas de 5 a 10 min.

## 7. Rate limiting

Con el rate limiter integrado de ASP.NET Core, particionado por usuario autenticado (o por la IP real cuando no hay sesión):

| Endpoint | Límite |
|----------|--------|
| `POST /auth/login` | Bloqueo por correo: 5 fallos en 5 min (`authentication.md`) |
| `POST /auth/register`, `/forgot-password`, `/resend-verification` | 5 por IP cada 15 min |
| `POST /classes/join` | 10 por usuario por minuto (evita adivinar códigos por fuerza bruta) |
| `POST /chat/.../messages` | 20 por usuario cada 10 s |
| `POST /files/uploads` | 30 por usuario por minuto |
| Resto de las mutaciones | 120 por usuario por minuto |

- Los métodos de hub tienen sus propios límites (`realtime.md`).
- La API está detrás del reverse proxy: se configuran `ForwardedHeaders` **solo** con el proxy conocido como confiable. Sin eso, todas las IPs serían la del proxy.

## 8. Bloqueo de cuenta

5 fallos por correo en 5 minutos → `429 TOO_MANY_LOGIN_ATTEMPTS`. Un reset de contraseña exitoso desbloquea la cuenta.

## 9. Contraseñas

- Se guardan con hash usando `PasswordHasher<TUser>` de ASP.NET Core Identity (PBKDF2), sin adoptar todo Identity.
- 10 caracteres como mínimo, con letra y número. No se limita el máximo por debajo de 128.

## 10. PostgreSQL y EF Core

- Solo consultas parametrizadas: LINQ o `FromSqlInterpolated`. **Prohibido** `FromSqlRaw` con concatenación.
- El usuario de la base de la aplicación no es superusuario.

## 11. Mass assignment

- Los requests son DTOs específicos, nunca entidades.
- No se mapean desde el request campos como `role`, `institutionId`, `teacherId`, `status`, `grade` o `isLate`.

## 12. Cookies, CORS y CSRF

Ver `authentication.md`: cookies `HttpOnly`, `Secure` y `SameSite=Strict`, un solo origen y sin CORS en producción.

## 13. Headers de seguridad

Los aplica el reverse proxy:

- `Content-Security-Policy`: `default-src 'self'`; scripts de `'self'` y `https://accounts.google.com`; `frame-src https://accounts.google.com`; `connect-src 'self'` (incluye `wss:` del mismo origen); `img-src 'self' data: blob:`.
- `Strict-Transport-Security`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin` y `Permissions-Policy` restrictiva.

## 14. Dependencias

- Lockfiles versionados (`package-lock.json` y `packages.lock.json`).
- Dependabot activo para NuGet, npm, GitHub Actions y las imágenes de Docker Compose (PRs semanales).
- Agregar una dependencia requiere la confirmación del equipo.

## 15. Datos personales

- No hay alumnos menores de edad (contexto universitario).
- Los datos personales (correo, campus y perfil académico) solo los ven los miembros de las clases compartidas y los administradores.
- Un usuario desactivado conserva su autoría, pero no aparece en las búsquedas.
