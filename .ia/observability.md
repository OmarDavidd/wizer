# Wizer — Observabilidad y logging

## Objetivo

Logs estructurados y correlacionables, desacoplados de la plataforma de destino.

## Arquitectura

```text
ASP.NET Core (ILogger<T>)
   ↓
Serilog → JSON a stdout
   ↓
Docker (logs del contenedor)
   ↓
Grafana Alloy (recolector)
   ↓
Loki
   ↓
Grafana
```

## Regla de desacoplamiento

- El código usa solo `ILogger<T>`.
- Serilog se configura en `Program.cs` y en `appsettings`.
- **Prohibido** enviar logs desde la aplicación directamente a Loki o Grafana (sin el sink de Loki).
- Cambiar de destino no debe requerir cambios de código.

## Structured logging

Message templates, nunca interpolación:

```csharp
_logger.LogInformation("Assignment {AssignmentId} published in class {ClassId}", assignmentId, classId);
```

- Mensajes en inglés.
- Propiedades en PascalCase: `UserId`, `ClassId`, `TeamId`, `FileId`.

## Salida

- Producción: `CompactJsonFormatter` a stdout.
- Desarrollo: consola legible.

## Propiedades comunes

Enriquecidas en cada log: `Application = "wizer-api"`, `Environment`, `CorrelationId`, `UserId` (si hay sesión), `RequestPath` y `MachineName`.

## Correlation ID

- Header `X-Correlation-Id`: si llega uno válido (UUID o ULID) se reutiliza; si no, se genera.
- Se devuelve en la respuesta y en `ProblemDetails.correlationId`.
- En SignalR, cada invocación de hub registra el `ConnectionId` y genera su propio correlation ID.

## Logging de requests HTTP

`UseSerilogRequestLogging` con: método, ruta (la plantilla de ruta, no la URL con query), status, duración y usuario. Sin bodies.

## Niveles

| Nivel | Uso |
|-------|-----|
| `Debug` | Detalle de desarrollo (desactivado en producción) |
| `Information` | Hechos de negocio: clase creada, tarea publicada, entrega enviada, usuario invitado |
| `Warning` | Situación anómala recuperable: 403 o 429, archivo infectado, reintento de escaneo, unión a grupo rechazada |
| `Error` | Fallo de una operación: 5xx, envío de correo fallido, escaneo agotado |
| `Critical` | La aplicación no puede funcionar: no hay base de datos al arrancar |

Overrides: `Microsoft.AspNetCore` y `Microsoft.EntityFrameworkCore` en `Warning`.

## Lo que NUNCA se registra

- Contraseñas, códigos de verificación, tokens de invitación o de reset.
- JWT, refresh tokens, cookies y headers `Authorization`.
- Connection strings, credenciales de MinIO, API keys.
- **Contenido de los mensajes del chat, escenas de pizarra y contenido de archivos.**
- Bodies de requests y responses.
- URLs firmadas (contienen la firma).

Los correos se registran enmascarados (`a***@uni.edu`) solo cuando son necesarios para diagnosticar.

## Excepciones

- Las no controladas se registran una sola vez, en el middleware global, como `Error` con la excepción.
- Las de negocio (4xx) no se registran como error (`errors.md`).

## Health checks

| Endpoint | Verifica |
|----------|----------|
| `/health/live` | El proceso responde |
| `/health/ready` | PostgreSQL, MinIO y ClamAV disponibles |

Los usa Docker Compose (`healthcheck`) y no requieren autenticación. Responden solo el estado, sin detalles internos.

## Métricas, trazas y errores del frontend

- **MVP: solo logs y health checks.**
- OpenTelemetry (métricas y trazas) y la captura de errores del frontend (Sentry o similar) quedan para después del MVP. Si se agregan, no deben acoplar el código de negocio.

## Grafana Alloy y Loki

- Alloy lee los logs de los contenedores con la etiqueta `logging=alloy` y los envía a Loki.
- La configuración vive en `infra/alloy/config.alloy`.
- Etiquetas de Loki de baja cardinalidad: `app`, `env` y `level`. Nunca `UserId` ni `CorrelationId` como etiqueta (van como campos del JSON).

## Pruebas

- Una prueba de integración verifica que el `CorrelationId` aparece en la respuesta y en los ProblemDetails.
- Una prueba verifica que el logging de requests no incluye bodies ni cookies.
