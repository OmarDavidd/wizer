# Wizer — Reglas de la API

## Objetivo

Definir un contrato REST consistente entre el backend ASP.NET Core y el frontend.

## General

- REST sobre JSON.
- Base path: `/api/v1`.
- Las respuestas exitosas devuelven el DTO directamente, sin un envoltorio `ApiResponse<T>`.
- Los errores siguen `errors.md` (ProblemDetails + `code`).
- El OpenAPI es la fuente de verdad: se genera con `Microsoft.AspNetCore.OpenApi` y se documenta con Scalar en `/api/docs` (solo en desarrollo).
- El frontend consume la API mediante el cliente generado por Orval.

## Nombres de recursos

- En inglés, en plural y en kebab-case: `/classes`, `/teams`, `/assignments`, `/chat/channels`, `/file-uploads`.
- Anidar como máximo un nivel cuando el padre define el acceso: `/classes/{classId}/teams`, `/assignments/{assignmentId}/submissions`.
- Una vez creado, el recurso se direcciona por su propio ID: `/teams/{teamId}`, no `/classes/{c}/teams/{t}`.
- Las propiedades JSON van en camelCase.

## Endpoints principales (referencia)

```text
POST   /auth/register                 POST /auth/login
POST   /auth/google                   POST /auth/refresh
POST   /auth/logout                   GET  /auth/me
POST   /auth/verify-email             POST /auth/resend-verification
POST   /auth/forgot-password          POST /auth/reset-password
POST   /auth/set-password             (invitación de docente)

GET    /classes                       POST /classes
GET    /classes/{id}                  PATCH /classes/{id}
POST   /classes/{id}/archive          POST /classes/{id}/unarchive
POST   /classes/{id}/join-code        (regenerar)
POST   /classes/join                  { code }
GET    /classes/{id}/members          DELETE /classes/{id}/members/{userId}
DELETE /classes/{id}/membership       (salir de la clase)
GET    /classes/{id}/presence

GET    /classes/{id}/teams            POST /classes/{id}/teams
POST   /classes/{id}/teams/auto-assign
PATCH  /teams/{id}                    DELETE /teams/{id}
PUT    /teams/{id}/members/{userId}   DELETE /teams/{id}/members/{userId}
GET    /teams/{id}/activity

GET    /chat/channels/{id}/messages?before=&limit=
POST   /chat/channels/{id}/messages
PATCH  /chat/messages/{id}            DELETE /chat/messages/{id}
PUT    /chat/messages/{id}/reactions/{emoji}
DELETE /chat/messages/{id}/reactions/{emoji}
POST   /teams/{id}/chat-access-requests
POST   /chat-access-requests/{id}/approve
POST   /chat-access-requests/{id}/reject
DELETE /teams/{id}/chat-access        (revocar)

GET    /teams/{id}/whiteboard

GET    /classes/{id}/assignments      POST /classes/{id}/assignments
GET    /assignments/{id}              PATCH /assignments/{id}
DELETE /assignments/{id}
GET    /assignments/{id}/submissions?status=&search=&page=&pageSize=
GET    /assignments/{id}/submissions/summary
GET    /assignments/{id}/my-submission
PUT    /assignments/{id}/my-submission           (borrador: archivos y enlaces)
POST   /assignments/{id}/my-submission/submit
POST   /assignments/{id}/my-submission/unsubmit
PUT    /submissions/{id}/grade
PUT    /submissions/{id}/member-grades/{userId}
GET    /me/assignments?status=pending

POST   /files/uploads                 POST /files/{id}/complete
GET    /files/{id}/download-url       DELETE /files/{id}
GET    /classes/{id}/files

GET    /notifications?before=&limit=  POST /notifications/{id}/read
POST   /notifications/read-all        GET  /notifications/unread-count

GET    /me/profile                    PATCH /me/profile
GET    /campuses

GET    /admin/users                   POST /admin/users   (invita a un docente o administrador)
PATCH  /admin/users/{id}              POST /admin/users/{id}/deactivate
POST   /admin/campuses                PATCH /admin/campuses/{id}
```

Si un endpoint nuevo no encaja en este esquema, consultar antes de crearlo.

## Métodos HTTP

| Método | Uso | Respuesta típica |
|--------|-----|------------------|
| GET | Leer | 200 |
| POST | Crear o ejecutar una acción | 201 + `Location`, o 200/202/204 para acciones |
| PUT | Reemplazar, o una operación idempotente sobre un subrecurso | 200 o 204 |
| PATCH | Modificar campos parciales | 200 con el recurso |
| DELETE | Borrar o revocar | 204 |

Las acciones que no son CRUD se expresan como sub-recurso verbal: `/archive`, `/submit`, `/approve`.

## Paginación

**Por cursor**, para flujos cronológicos (mensajes, notificaciones, actividad):

```text
GET /chat/channels/{id}/messages?before=<cursor>&limit=50
→ { "items": [...], "nextCursor": "..." | null }
```

- El cursor es opaco (Base64Url de `created_at` + `id`).
- `limit` va de 1 a 100 (por defecto 50).

**Por offset**, para listas administrativas (entregas, usuarios, archivos):

```text
GET /assignments/{id}/submissions?page=1&pageSize=25
→ { "items": [...], "page": 1, "pageSize": 25, "totalCount": 132 }
```

- `pageSize` va de 1 a 100 (por defecto 25).

## Filtros y orden

- Filtros como query params con nombres explícitos: `status=late`, `search=ana`.
- Orden con `sort=field` o `sort=-field`, solo sobre los campos permitidos por cada endpoint.

## Fechas

- ISO 8601 con zona (`2026-10-22T23:59:00Z`). El backend siempre responde en UTC.
- El frontend muestra la hora local del usuario.
- Las fechas límite se envían en UTC. El frontend convierte desde la hora local que eligió el docente.

## Idempotencia

- `PUT`, `DELETE` y las acciones de reacción son idempotentes.
- Crear un mensaje usa `clientMessageId` (`realtime.md`).
- `submit` sobre una entrega ya enviada devuelve 200 con el estado actual, sin error.

## Autenticación y autorización

- Todas las rutas requieren sesión, salvo registro, login, Google, refresh, verificación y recuperación de contraseña.
- Autorización en dos niveles: **política de rol** (`[Authorize(Policy = "Teacher")]`) en el Controller y **pertenencia al recurso** en el Service.
- Para recursos de otra clase, responder 404, no 403, cuando revelar que el recurso existe sería información sensible (`errors.md`).

## Rate limiting

Ver `security.md`. Los límites superados responden 429 con código `RATE_LIMITED` y el header `Retry-After`.

## Versionado

- `v1` durante todo el MVP.
- Un cambio incompatible del contrato se coordina entre backend y frontend en el mismo PR y se regenera el cliente con Orval.
- Agregar campos opcionales no rompe el contrato. Quitar o renombrar campos sí.
