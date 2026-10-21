# Wizer — Reglas del backend

## Objetivo

Reglas obligatorias para desarrollar el backend. Es C# con ASP.NET Core Web API, organizado como monolito modular.

## Stack

- .NET 10 (LTS) · C#
- ASP.NET Core Web API
- Entity Framework Core + Npgsql
- PostgreSQL
- FluentValidation
- SignalR
- JWT en cookies HttpOnly · Google ID token
- Serilog
- Rate limiting integrado de ASP.NET Core
- Microsoft.AspNetCore.OpenApi + Scalar
- xUnit · Moq · Testcontainers

## Idioma del código

Todo en inglés: namespaces, clases, interfaces, métodos, variables, propiedades, DTOs, excepciones, logs y endpoints.

```text
SchoolClass          (no "Class": choca con la palabra reservada)
ClassMembership
Team
ChatMessage
Assignment
Submission
IClassService
IClassRepository
CreateClassRequest
ClassDetailResponse
```

## Estructura

```text
apps/backend/src/Wizer.Api/
├── Modules/
├── Infrastructure/
├── Shared/
├── Program.cs
└── appsettings.json
```

No crear carpetas globales de negocio (`Controllers/`, `Services/`, `Repositories/`, `Entities/`) en la raíz.

## Configuración de los proyectos

- `apps/backend/Directory.Build.props` define para todos los proyectos: `net10.0`, `Nullable`, `ImplicitUsings`, warnings como errores en `Release` y **lock files** (`packages.lock.json`, versionados). La CI restaura con `--locked-mode`: si se agrega un paquete, el lock file actualizado va en el mismo commit.
- **OpenAPI en build**: `Microsoft.Extensions.ApiDescription.Server` escribe `apps/backend/openapi/wizer-api.json` en cada build. Ese archivo se versiona y es la entrada de Orval. Por eso nada en el arranque puede exigir infraestructura al registrar servicios (la connection string se lee de forma diferida).
- **Solo JSON** (a implementar con el primer endpoint): en `AddControllers` se quita el `StringOutputFormatter` y el media type `text/json` de los formatters JSON, así el contrato no declara `text/plain` ni `text/json`. **No** se usa un filtro global `[Produces]`: pisaría el `application/problem+json` de las respuestas de error del framework (cubrirlo con una prueba de integración). `application/*+json` se mantiene porque es lo que permite escribir `problem+json`.
- La API escucha en `http://localhost:5080` en desarrollo (`launchSettings.json`).
- **Ninguna connection string ni contraseña se versiona** (ni siquiera las de desarrollo: el repo es público y los escáneres de secretos las marcan). `make api` arma `ConnectionStrings__Wizer` con las variables de `.env` (o los valores por defecto del Compose). Para correr la API desde el IDE, guardarla una vez con user-secrets:
  ```bash
  dotnet user-secrets --project apps/backend/src/Wizer.Api init
  dotnet user-secrets --project apps/backend/src/Wizer.Api set ConnectionStrings:Wizer "<connection string>"
  ```
  La connection string es la misma que imprime `make -n api` (host `localhost`, y base, usuario, contraseña y puerto de tu `.env`).
- Documentación interactiva con Scalar en `/api/docs` (solo en desarrollo; se agrega con el primer endpoint). Hoy el documento OpenAPI se sirve en `/openapi/v1.json`.

## Estructura de un módulo

Cada módulo usa solo las carpetas que necesita:

```text
Modules/
└── Classes/
    ├── Controllers/
    ├── Hubs/                (solo si el módulo tiene tiempo real)
    ├── Services/
    ├── Repositories/
    ├── Interfaces/
    ├── Entities/
    ├── Configurations/
    ├── DTOs/
    │   ├── Requests/
    │   └── Responses/
    ├── Validators/
    ├── Mappers/
    ├── Events/              (eventos en proceso que publica el módulo)
    ├── Classes/             (auxiliares propias del módulo)
    └── DependencyInjection.cs
```

No crear carpetas vacías.

## Flujo normal

```text
Controller → IService → Service → IRepository → Repository → EF Core → PostgreSQL
```

### Controllers
- Reciben la request, leen la ruta, el query y el body, llaman al Service y devuelven el resultado HTTP.
- Llevan `[Authorize]` por defecto. Los endpoints anónimos se marcan explícitamente con `[AllowAnonymous]`.
- NO contienen reglas de negocio, NO usan `AppDbContext` y NO llaman a repositories.
- NO reciben el ID del usuario actual como parámetro: lo obtienen de `ICurrentUser`.

### Services
- Contienen la lógica de negocio y la **autorización por recurso** (pertenencia a la clase o al equipo).
- Pueden depender de: repositories del mismo módulo, `I<Otro>ModuleService`, interfaces de infraestructura (`IFileStorage`, `IEmailSender`, `IClock`), notifiers de tiempo real y `ILogger<T>`.

### Repositories
- Uno por agregado (`IClassRepository`, `ITeamRepository`, `IChatMessageRepository`…).
- Prohibido el Generic Repository (`IRepository<T>`).
- Solo acceso a datos. Sin reglas de negocio.
- Las consultas de lectura usan `AsNoTracking()` y proyecciones cuando no se va a modificar la entidad.

## Usuario actual

```csharp
public interface ICurrentUser
{
    Guid Id { get; }
    PlatformRole Role { get; }
    Guid InstitutionId { get; }
}
```

Se resuelve desde los claims del JWT. Es la **única** fuente de identidad. Un `userId` que llegue en el body o en el query y represente "quién hace la acción" es un error de diseño.

## Comunicación entre módulos

Mediante interfaces públicas y DI:

```text
SubmissionService → IAssignmentModuleService → AssignmentModuleService
```

Prohibido:
- usar repositories o `DbSet` de otro módulo;
- devolver entidades de EF Core a otro módulo (los contratos devuelven DTOs o records);
- crear dependencias circulares.

### Eventos en proceso

Para efectos secundarios que no deben acoplar módulos (por ejemplo, "se publicó una tarea" → notificar a los alumnos), se usan eventos en proceso:

```csharp
public sealed record AssignmentPublished(Guid AssignmentId, Guid ClassId, string Title);

public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct);
}

public interface IDomainEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}
```

- La implementación es propia (unas 40 líneas en `Shared/Events`), no MediatR.
- Los handlers se ejecutan **después** de confirmar la transacción. Si un handler falla, se registra en el log y no revierte la operación principal.
- Se usan solo para efectos secundarios: notificaciones, actividad del equipo y difusión en tiempo real.

## MediatR, CQRS y AutoMapper

Prohibidos. El proyecto anterior los usaba y se descartaron.

## Entities

- Viven en su módulo: `Modules/Classes/Entities/SchoolClass.cs`.
- Clave primaria `Guid` generada con UUID v7 (`IIdGenerator` en `Shared/Ids`).
- Encapsulan sus invariantes cuando es razonable: `SchoolClass.RegenerateJoinCode(IJoinCodeGenerator)`, `Submission.Submit(DateTimeOffset now, Assignment rules)`.
- Nunca se devuelven por la API.

## Configurations

`IEntityTypeConfiguration<T>` dentro del módulo. Definen la tabla, las claves, las relaciones, los índices, las restricciones únicas, las longitudes, el comportamiento de borrado y los filtros globales (`HasQueryFilter` para soft delete). `AppDbContext` las aplica con `ApplyConfigurationsFromAssembly`.

## DTOs

```text
DTOs/Requests/CreateClassRequest.cs
DTOs/Responses/ClassSummaryResponse.cs
DTOs/Responses/ClassDetailResponse.cs
```

- Son `record`.
- Un DTO por escenario. No crear DTOs gigantes compartidos.
- Las respuestas paginadas usan `PagedResponse<T>` o `CursorPage<T>` (`api.md`).

## Validators

FluentValidation, uno por request: `CreateClassRequest` → `CreateClassRequestValidator`.

- La validación de formato y entrada va en el Validator.
- Las reglas de negocio van en el Service.
- Se registran automáticamente y se ejecutan con un filtro global antes del Controller.

## Mappers

Manuales, con métodos de extensión estáticos:

```csharp
public static class ClassMapper
{
    public static ClassSummaryResponse ToSummary(this SchoolClass entity) => new(...);
}
```

Sin lógica de negocio.

## Transacciones

- Una operación de negocio de varios pasos usa **una** transacción (`IUnitOfWork` o `AppDbContext.Database.BeginTransactionAsync` desde el Service).
- Los eventos en proceso se publican después de confirmar.
- La concurrencia optimista usa `xmin` de PostgreSQL como token de concurrencia en las entidades editables por varias personas (entregas, pizarras y tareas).

## Tiempo de servidor

Nunca usar `DateTime.Now` ni `DateTime.UtcNow` directamente: usar `IClock` (`Shared/Time`), que se reemplaza en las pruebas. Todas las fechas son `DateTimeOffset` en UTC.

## Dependency Injection por módulo

```csharp
public static IServiceCollection AddClassesModule(this IServiceCollection services)
{
    services.AddScoped<IClassService, ClassService>();
    services.AddScoped<IClassRepository, ClassRepository>();
    services.AddScoped<IClassModuleService, ClassModuleService>();
    services.AddSingleton<IJoinCodeGenerator, JoinCodeGenerator>();
    return services;
}
```

`Program.cs` se mantiene corto: llama a `AddInfrastructure()`, a `Add<Module>Module()` y mapea los controllers y los hubs.

## Infrastructure

```text
Infrastructure/
├── Persistence/        AppDbContext, Migrations/, Seed/
├── Storage/MinIO/      IFileStorage
├── Antivirus/ClamAV/   IAntivirusScanner
├── Email/              IEmailSender (Resend, SMTP)
├── Authentication/     tokens, cookies, Google
├── Realtime/           IPresenceTracker, mapeo de hubs
└── DependencyInjection.cs
```

MinIO, ClamAV y el correo son infraestructura, no módulos de negocio. Los módulos los usan mediante interfaces.

## Trabajo en segundo plano

- Se usa `BackgroundService` con `System.Threading.Channels` para trabajos internos: escaneo antivirus, guardado de snapshots de pizarra y recordatorios de vencimiento.
- Sin colas externas en el MVP.
- Los trabajos son idempotentes: si el proceso se reinicia, un trabajo pendiente se recupera desde el estado en la base (por ejemplo, archivos con `scan_status = pending`).

## Errores

Excepciones comunes en `Shared/Exceptions/`:

```text
NotFoundException
ForbiddenException
ConflictException
BusinessRuleException (con code)
ValidationException (de FluentValidation)
```

Un middleware global las convierte en `ProblemDetails` con `code` y `correlationId`. Ver `errors.md`.

## Logging

`ILogger<T>` con message templates:

```csharp
_logger.LogInformation("Student {UserId} joined class {ClassId}", userId, classId);
```

Nunca interpolación. Nunca registrar contraseñas, tokens, cookies, el contenido de los mensajes del chat ni datos personales innecesarios. Ver `observability.md`.

## Async

Toda operación de I/O es async, con sufijo `Async`, y propaga el `CancellationToken`.

## Pruebas

Ver `testing.md`. Reglas de negocio de los Services con TDD.
