# Wizer — Manejo de errores

Contrato de errores consistente entre ASP.NET Core y el frontend.

## Objetivo

Todos los endpoints y hubs devuelven errores predecibles y seguros. El frontend decide su comportamiento por `code`, nunca interpretando mensajes de texto.

## Formato

`ProblemDetails` (RFC 9457):

```json
{
  "type": "https://wizer.app/errors/validation-error",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/classes",
  "code": "VALIDATION_ERROR",
  "correlationId": "01J...",
  "errors": {
    "name": ["Name is required."]
  }
}
```

Los textos del backend van en inglés. El frontend muestra mensajes en español a partir del `code`.

## Campos

| Campo | Regla |
|-------|-------|
| `type` | Identificador estable del tipo de problema |
| `title` | Título corto |
| `status` | Código HTTP |
| `detail` | Detalle seguro para el cliente |
| `instance` | Path de la petición |
| `code` | Código estable en inglés (SCREAMING_SNAKE_CASE) |
| `correlationId` | Correlación con los logs |
| `errors` | Errores por campo (solo en validación) |

## Status codes

| Status | Uso |
|--------|-----|
| 400 | Entrada inválida, transición de estado inválida, regla de negocio violada |
| 401 | No hay sesión válida. El frontend intenta un refresh, una sola vez |
| 403 | Hay sesión pero no permiso. No usar 401 para esconder un problema de autorización |
| 404 | No existe, o no se debe revelar que existe (recursos de otra clase) |
| 409 | Conflicto con el estado actual (duplicado, concurrencia) |
| 413 | Archivo o request demasiado grande |
| 415 | Tipo no soportado |
| 429 | Rate limit, con el header `Retry-After` |
| 500 | Error inesperado. Nunca incluye el stack trace en producción |

**No se usa 422.** Toda la validación responde 400.

## Códigos

### Generales
```text
BAD_REQUEST              (cualquier 4xx sin código propio; nunca se reporta un 4xx como INTERNAL_ERROR)
VALIDATION_ERROR
UNAUTHORIZED
FORBIDDEN
RESOURCE_NOT_FOUND
METHOD_NOT_ALLOWED
CONFLICT
CONCURRENCY_CONFLICT
PAYLOAD_TOO_LARGE
UNSUPPORTED_MEDIA_TYPE
RATE_LIMITED
INTERNAL_ERROR
SERVICE_UNAVAILABLE
```

Se implementan en `apps/backend/src/Wizer.Api/Shared/Errors/`. Si una respuesta de error no trae `code` (por ejemplo, un 405 que genera el framework), se asigna el código por defecto de su status .

El campo `type` siempre se deriva del `code`: `https://wizer.app/errors/` + el código en minúsculas con guiones (`CLASS_NOT_FOUND` → `https://wizer.app/errors/class-not-found`).

### Identity
```text
EMAIL_ALREADY_EXISTS
INVALID_CREDENTIALS
EMAIL_NOT_VERIFIED
INVALID_VERIFICATION_CODE
VERIFICATION_PASSWORD_MISMATCH
INVALID_RESET_CODE
INVALID_OR_EXPIRED_TOKEN
ACCOUNT_INACTIVE
TOO_MANY_LOGIN_ATTEMPTS
GOOGLE_TOKEN_INVALID
GOOGLE_ACCOUNT_NOT_ALLOWED
CAMPUS_REQUIRED
```

### Clases y equipos
```text
CLASS_NOT_FOUND
JOIN_CODE_INVALID
ALREADY_CLASS_MEMBER
CLASS_ARCHIVED
NOT_CLASS_TEACHER
TEAM_NOT_FOUND
TEAM_FULL
STUDENT_ALREADY_IN_TEAM
```

### Chat
```text
MESSAGE_NOT_FOUND
MESSAGE_EDIT_FORBIDDEN
CHAT_ACCESS_REQUEST_PENDING
INVALID_MENTION
```

### Pizarra
```text
WHITEBOARD_TOO_LARGE
```

### Tareas y entregas
```text
ASSIGNMENT_NOT_FOUND
SUBMISSION_DEADLINE_PASSED
SUBMISSION_LOCKED
SUBMISSION_EMPTY
GRADE_OUT_OF_RANGE
GRADING_TYPE_MISMATCH
```

### Archivos
```text
UPLOAD_INVALID_TYPE
UPLOAD_TOO_LARGE
STORAGE_QUOTA_EXCEEDED
FILE_NOT_AVAILABLE
FILE_REJECTED
```

Agregar un código solo cuando el frontend necesite distinguir un comportamiento o el dominio lo justifique. Un código nuevo se documenta aquí y en el mapa de mensajes del frontend en el mismo PR.

## Backend

- Un middleware global convierte las excepciones en ProblemDetails (`Shared/Errors/ProblemDetailsMapper`).
- Los Controllers no llevan `try/catch` repetitivos.
- `BusinessRuleException(code, detail)` cubre las reglas de negocio con su código.
- Los errores de FluentValidation producen `VALIDATION_ERROR` con `errors` por campo (las claves en camelCase).
- Cada error se registra con `code`, `correlationId`, ruta y usuario. Nunca con payloads sensibles.
- 4xx → `Warning` solo si es relevante para la seguridad (403 o 429); los demás 4xx no se registran como error. 5xx → `Error` con la excepción.

## Hubs de SignalR

- Los errores de un hub se lanzan como `HubException` con el mensaje `"{CODE}: {detail}"`.
- El cliente los parsea en `realtime/errors.ts` y los trata con el mismo mapa de códigos.

## Frontend

- `api/errors.ts` normaliza cualquier error a `{ kind: 'network' | 'http', status, code, fieldErrors, correlationId }`.
- `lib/errorMessages.ts` mapea `code` → mensaje en español. Si el código no está en el mapa, muestra un mensaje genérico por status.
- Comportamiento:
  - 401 → flujo central de sesión (refresh, una sola vez);
  - 403 → pantalla o estado "No tienes permiso";
  - 404 → Not Found;
  - 409 → mensaje específico del conflicto;
  - 429 → informar y esperar, sin reintentos agresivos;
  - `VALIDATION_ERROR` → `setError` en el formulario.
- El `correlationId` se muestra en el detalle de los errores 500 ("Código de soporte: …").

## TanStack Query

- Las queries reintentan solo en errores de red o 5xx (2 veces).
- Las mutaciones **no se reintentan automáticamente**, salvo las idempotentes (por ejemplo, el envío de mensajes con `clientMessageId`, que maneja el outbox).

## UX

Mensajes claros y accionables:

```text
No pudimos entregar la tarea.
El plazo venció el jueves a las 23:59.
```

Nunca:

```text
System.InvalidOperationException...
```

## Pruebas

Cada endpoint importante cubre: validación, 401, 403/404 por pertenencia, conflicto relevante y el caso exitoso. Las pruebas verifican `status` y `code`, no frases completas.
