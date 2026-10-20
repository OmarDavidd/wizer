# Wizer — Roles y permisos

## Dos niveles de rol

| Nivel | Dónde vive | Valores | Para qué sirve |
|-------|------------|---------|----------------|
| **Rol de plataforma** | `users.platform_role` | `admin`, `teacher`, `student` | Qué puede crear la cuenta (clases, usuarios) |
| **Rol de clase** | `class_memberships.role` | `teacher`, `student` | Qué puede hacer dentro de una clase concreta |

- Solo una cuenta `teacher` puede **crear** clases. Al crear una, queda como `teacher` de esa clase.
- Cualquier cuenta (incluido un docente) puede **unirse** a la clase de otro como `student`.
- Cada clase tiene **exactamente un** `teacher` (sin co-docentes en el MVP).
- Un `admin` no es miembro de las clases. Su acceso a ellas se limita a la administración (`§Administración`).

En este documento, "docente de la clase" significa la membresía `teacher` de esa clase, y "alumno de la clase" la membresía `student`.

## Matriz

| Acción | Admin | Docente de la clase | Alumno de la clase | Miembro del equipo | Otro usuario |
|--------|:-----:|:-------------------:|:------------------:|:------------------:|:------------:|
| Crear una clase | ✗ | (rol de plataforma `teacher`) | ✗ | — | ✗ |
| Ver la clase y sus miembros | ✗ | ✓ | ✓ | — | ✗ (404) |
| Editar, archivar o regenerar el código | ✗ | ✓ | ✗ | — | ✗ |
| Eliminar la clase | ✗ | ✓ (con confirmación) | ✗ | — | ✗ |
| Unirse con código | — | — | — | — | ✓ |
| Quitar a un alumno | ✗ | ✓ | ✗ | — | ✗ |
| Salir de la clase | — | ✗ | ✓ | — | — |
| Crear, editar y borrar equipos | ✗ | ✓ | ✗ | — | ✗ |
| Asignar alumnos a equipos | ✗ | ✓ | ✗ | — | ✗ |
| Chat general: leer y escribir | ✗ | ✓ | ✓ | — | ✗ |
| Chat de equipo: leer y escribir | ✗ | con acceso concedido | ✗ | ✓ | ✗ |
| Pedir acceso al chat de un equipo | ✗ | ✓ | ✗ | — | ✗ |
| Aprobar, rechazar o revocar ese acceso | ✗ | ✗ | ✗ | ✓ | ✗ |
| Editar o borrar un mensaje propio | — | ✓ | ✓ | ✓ | — |
| Borrar un mensaje ajeno en el chat general | ✗ | ✓ | ✗ | — | ✗ |
| Pizarra de equipo: ver y editar | ✗ | ✓ | ✗ | ✓ | ✗ |
| Crear, editar y borrar tareas | ✗ | ✓ | ✗ | — | ✗ |
| Ver las tareas | ✗ | ✓ | ✓ | — | ✗ |
| Ver todas las entregas de una tarea | ✗ | ✓ | ✗ | — | ✗ |
| Ver la entrega propia o del propio equipo | — | — | ✓ | ✓ | ✗ |
| Editar, enviar o anular la entrega propia o del equipo | — | — | ✓ | ✓ (cualquier miembro) | ✗ |
| Calificar | ✗ | ✓ | ✗ | — | ✗ |
| Subir material de clase | ✗ | ✓ | ✓ | — | ✗ |
| Descargar archivos de la clase | ✗ | ✓ | ✓ | — | ✗ |
| Borrar un archivo | ✗ | ✓ (cualquiera) | solo los propios | — | ✗ |
| Crear cuentas docentes o de administrador | ✓ | ✗ | ✗ | — | ✗ |
| Desactivar usuarios | ✓ | ✗ | ✗ | — | ✗ |
| Gestionar campus | ✓ | ✗ | ✗ | — | ✗ |

## Pertenencia (ownership)

Toda operación sobre un recurso de una clase valida, en el **Service**:

1. que el recurso exista y no esté borrado;
2. que el usuario actual sea miembro de la clase de ese recurso;
3. el rol de clase requerido;
4. para los recursos de equipo, que el usuario sea miembro del equipo (o docente, según la matriz).

La pertenencia se resuelve con `IClassModuleService.GetMembershipAsync(classId, userId)` e `ITeamModuleService.IsMemberAsync(teamId, userId)`. Nunca se confía en un `classId` o `teamId` sin verificarlo.

## Clases archivadas

Una clase archivada queda **en solo lectura** para todos: no se envían mensajes, no se editan pizarras ni se crean ni envían tareas o entregas. Se puede leer y descargar. Las mutaciones responden `400 CLASS_ARCHIVED`. El docente puede desarchivarla.

## Administración

- El `admin` gestiona usuarios y campus.
- No ve chats, pizarras ni entregas.
- No puede iniciar sesión como otro usuario (no hay impersonación).

## Respuestas HTTP esperadas

| Caso | Respuesta |
|------|-----------|
| Sin sesión | 401 |
| Rol de plataforma insuficiente (por ejemplo, un alumno crea una clase) | 403 `FORBIDDEN` |
| No es miembro de la clase del recurso | 404 (no revelar que existe) |
| Es miembro pero no tiene el rol de clase necesario | 403 `FORBIDDEN` o `NOT_CLASS_TEACHER` |
| Docente sin acceso concedido al chat del equipo | 403 `FORBIDDEN` |

## Frontend

- Muestra u oculta acciones según el rol de clase que viene en `ClassDetailResponse.myRole`.
- Ocultar un botón no es seguridad: el backend valida siempre.

## Regla para agentes

Todo endpoint o método de hub nuevo:

1. declara su política de rol de plataforma (o `[AllowAnonymous]` justificado);
2. valida la pertenencia en el Service;
3. tiene pruebas para el caso exitoso, el no miembro (404) y el rol insuficiente (403).

Cambiar esta matriz requiere autorización del equipo.
