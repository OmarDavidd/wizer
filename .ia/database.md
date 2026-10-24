# Wizer — Reglas de base de datos

## Objetivo

Reglas para modelar y evolucionar la base de datos PostgreSQL con EF Core.

## Stack

- PostgreSQL
- EF Core + Npgsql
- Un solo `AppDbContext`
- Migraciones de EF Core

## Idioma y nombres

- Tablas y columnas en inglés y en `snake_case` (convención aplicada con `UseSnakeCaseNamingConvention()`).
- Tablas en plural: `school_classes`, `class_memberships`, `chat_messages`.
- Claves foráneas: `<entidad>_id` (`class_id`, `team_id`).
- Booleanos: `is_`, `has_`, `allow_` (`is_active`, `allow_late_submissions`).
- Fechas: sufijo `_at` (`created_at`, `submitted_at`, `due_at`).
- Índices: `ix_<tabla>_<columnas>`. Únicos: `ux_<tabla>_<columnas>`.

## Primary keys

- `uuid`, generado en la aplicación con **UUID v7** (`Guid.CreateVersion7()` detrás de `IIdGenerator`).
- Motivo: se puede exponer en URLs y en eventos de tiempo real sin revelar cuántos registros hay, y mantiene el orden por fecha (buen rendimiento en los índices).
- No usar `int` autoincremental en tablas expuestas.

## Foreign keys

- Siempre declaradas en la `Configuration`.
- Comportamiento de borrado explícito. Por defecto `Restrict`. `Cascade` solo para hijos sin vida propia (por ejemplo, `message_reactions` → `chat_messages`).
- Nunca borrar en cascada entidades con valor académico (entregas, notas, archivos).

## Tablas del MVP

| Módulo | Tablas |
|--------|--------|
| Identity | `users`, `refresh_tokens`, `verification_codes`, `external_logins`, `invitation_tokens` |
| Users | `academic_profiles` |
| Campuses | `institutions`, `campuses` |
| Classes | `school_classes`, `class_memberships`, `class_campuses` |
| Teams | `teams`, `team_members` |
| Chat | `chat_channels`, `chat_messages`, `message_reactions`, `message_mentions`, `chat_access_grants` |
| Whiteboard | `whiteboards` |
| Assignments | `assignments` |
| Submissions | `submissions`, `submission_member_grades`, `submission_links` |
| Files | `files` |
| Notifications | `notifications` |

Relaciones de los archivos: `files` usa `context_type` + `context_id` (polimórfico) y además `class_id` para calcular la cuota y filtrar. La integridad del contexto se valida en el Service. Los adjuntos del chat tienen como contexto el canal y además `chat_message_id`, que los liga a su mensaje.

Las entidades y sus `Configuration` viven en `Modules/<Módulo>/Entities` y `Modules/<Módulo>/Configurations`. Los nombres de las entidades coinciden con la tabla, salvo `StoredFile` (`files`) y `TeamWhiteboard` (`whiteboards`), que evitan chocar con `System.IO.File` y con el nombre del módulo.

## Reglas clave del modelo

- `users.platform_role`: `admin` | `teacher` | `student`.
- `class_memberships.role`: `teacher` | `student`. Hay un índice único parcial que garantiza **un solo `teacher` por clase**.
- `class_memberships (class_id, user_id)` es único.
- `team_members (team_id, user_id)` es único, y un alumno está en **un solo equipo por clase**: `team_members` guarda `class_id`, con un índice único `(class_id, user_id)`.
- `school_classes.join_code` es único entre las clases no archivadas (índice único parcial `WHERE archived_at IS NULL AND deleted_at IS NULL`).
- `chat_messages (sender_id, client_message_id)` es único (idempotencia).
- `submissions`: única por `(assignment_id, student_id)` en tareas individuales y por `(assignment_id, team_id)` en tareas de equipo (dos índices únicos parciales).
- `whiteboards.team_id` es único.
- `institution_id` está presente en `users`, `campuses` y `school_classes`. En el MVP hay una sola institución sembrada, pero los Services ya filtran por la institución del usuario actual.
- `team_members` tiene una FK compuesta `(team_id, class_id)` → `teams (id, class_id)`: el `class_id` del miembro no puede diferir del de su equipo.
- `chat_channels.type`: `class` (uno por clase), `team` (uno por equipo) o `assignment` (hilo de consultas, uno por tarea). Un `CHECK` exige que `team_id` y `assignment_id` coincidan con el tipo.
- `chat_access_grants`: un solo pedido `pending` por equipo (índice único parcial).
- `users.campus_id` admite null solo para el primer administrador de producción, que se siembra antes de que existan campus.
- Los enums se guardan en `snake_case` (`pass_fail`, `pending_scan`) con `HasSnakeCaseConversion()` y `HasEnumCheck<T>()` de `Infrastructure/Persistence/EnumMapping`.

## Tipos

| Dato | Tipo PostgreSQL |
|------|-----------------|
| IDs | `uuid` |
| Fechas | `timestamptz` (siempre UTC) |
| Dinero o notas | `numeric(5,2)` |
| Enums | `text` + `CHECK` (convertidos a string en EF Core; nunca enteros) |
| Escena de pizarra | `jsonb` |
| Texto largo | `text` con un límite validado en la aplicación |

## Soft delete

| Entidad | Estrategia |
|---------|------------|
| `school_classes` | `archived_at` (archivado manual) + `deleted_at` |
| `assignments` | `deleted_at` |
| `submissions` | Nunca se borran |
| `files` | `deleted_at`. El objeto se purga a los 30 días |
| `chat_messages` | `deleted_at` (se muestra "Mensaje eliminado") |
| `users` | `is_active = false` (desactivación, no borrado) |
| `teams` | Borrado físico solo si no tiene entregas; si tiene, `deleted_at` |
| `notifications`, `message_reactions`, `refresh_tokens`, `verification_codes` | Borrado físico |

El soft delete se aplica con un filtro global (`HasQueryFilter(x => x.DeletedAt == null)`). Para ignorarlo hace falta `IgnoreQueryFilters()` explícito y justificado.

Excepción: `chat_messages` **no** tiene filtro global, porque el historial tiene que mostrar "Mensaje eliminado". Las consultas devuelven las filas borradas y ocultan su contenido.

## Auditoría

- `created_at` en todas las tablas.
- `updated_at` en las entidades que se editan (clases, tareas, entregas, mensajes, equipos, perfiles).
- `created_by` / `graded_by` solo donde la autoría importa: `assignments.created_by`, `files.uploaded_by`, `submissions.graded_by`.
- `created_at` y `updated_at` los asigna `AuditInterceptor` usando `IClock`. Un `created_at` ya asignado se respeta (el seed escribe fechas pasadas).
- La autoría (`created_by_id`, `uploaded_by_id`, `graded_by_id`) la asigna el Service desde `ICurrentUser`, porque los workers y el seed no tienen usuario actual.
- Las claves `uuid` vacías reciben un UUID v7 de Npgsql al insertar. Si el Service necesita el id antes de guardar, usa `IIdGenerator`.

## Concurrencia

Token de concurrencia con `xmin` en `submissions`, `assignments`, `teams` y `whiteboards`. Un conflicto responde 409 `CONCURRENCY_CONFLICT`.

## Índices

Crear índices para los patrones de acceso reales. Como mínimo:

- `chat_messages (channel_id, created_at DESC, id DESC)`, para la paginación por cursor;
- `notifications (user_id, created_at DESC)` y `(user_id) WHERE read_at IS NULL`;
- `class_memberships (user_id)`;
- `submissions (assignment_id, status)`;
- `files (class_id) WHERE deleted_at IS NULL`, para la cuota.

## Migraciones

- Se crean con `make migration name=<PascalCaseName>`, que corre `dotnet ef migrations add` y las deja en `Infrastructure/Persistence/Migrations`. `dotnet-ef` es una herramienta local del repo (`dotnet-tools.json`); `make setup` la instala con `dotnet tool restore`.
- `dotnet ef` usa `AppDbContextDesignTimeFactory`: crear una migración no necesita la base ni el host.
- Las migraciones son código generado (`.editorconfig`): no se editan a mano. La prueba `Migrations_AppliedToEmptyDatabase_MatchTheModel` falla si el modelo cambió y falta la migración.
- **El agente pide confirmación antes de crear, modificar o eliminar una migración** (`workflow.md` §12).
- Nunca editar una migración que ya se aplicó en producción: se crea una nueva.
- Desarrollo: se aplican al arrancar si `Database:ApplyMigrationsOnStartup=true` (ya activado en `launchSettings.json`, que usan `make api` y el IDE).
- Producción: las aplica el servicio `migrator` (EF migration bundle) antes de que arranque la API. La API **no** migra en producción.
- Toda migración que transforma datos existentes se revisa en el PR y documenta cómo revertirla.

## Seed

- `Infrastructure/Persistence/Seed/DevSeeder`, solo en desarrollo (`Seed:Enabled=true`, que activa `make api`):
  - 1 institución y 3 campus;
  - 1 administrador (`admin@wizer.local`), 2 docentes (`docente1@`, `docente2@`) y 30 alumnos (`alumno01@` … `alumno30@`), todos con dominio `wizer.local`, verificados y activos;
  - la contraseña común sale de `Seed:Password`, que `make api` toma de `SEED_PASSWORD` (`.env`, con un valor por defecto en el `Makefile`). Sin contraseña, la API no arranca con el seed activado;
  - 3 clases (códigos `WZR2A4`, `WZR3B5`, `WZR4C6`) con 3 equipos cada una y alumnos sin equipo, tareas en borrador, abiertas, vencidas con tardías y calificadas, entregas (una tarde y una con nota 0 sin entrega), mensajes y una pizarra con contenido.
- El seed es idempotente: si ya existe una institución, no hace nada. Para volver a sembrar: `make infra-reset`.
- Las contraseñas usan el `PasswordHasher<T>` de ASP.NET Core (PBKDF2). El login de Identity tiene que verificar con el mismo hasher.
- Producción solo siembra la institución y el primer administrador, desde variables de entorno.

## Normalización

Tercera forma normal por defecto. Desnormalizar solo con una métrica que lo justifique y documentándolo en la `Configuration`.
