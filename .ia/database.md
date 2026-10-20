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

Relaciones de los archivos: `files` usa `context_type` + `context_id` (polimórfico) y además `class_id` para calcular la cuota y filtrar. La integridad del contexto se valida en el Service.

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

## Auditoría

- `created_at` en todas las tablas.
- `updated_at` en las entidades que se editan (clases, tareas, entregas, mensajes, equipos, perfiles).
- `created_by` / `graded_by` solo donde la autoría importa: `assignments.created_by`, `files.uploaded_by`, `submissions.graded_by`.
- Los valores los asigna un interceptor de `SaveChanges` usando `IClock` e `ICurrentUser`.

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

- Se crean con `dotnet ef migrations add <PascalCaseName>` desde `apps/backend`.
- **El agente pide confirmación antes de crear, modificar o eliminar una migración** (`workflow.md` §12).
- Nunca editar una migración que ya se aplicó en producción: se crea una nueva.
- Desarrollo: se aplican al arrancar si `Database:ApplyMigrationsOnStartup=true`.
- Producción: las aplica el servicio `migrator` (EF migration bundle) antes de que arranque la API. La API **no** migra en producción.
- Toda migración que transforma datos existentes se revisa en el PR y documenta cómo revertirla.

## Seed

- `Infrastructure/Persistence/Seed/DevSeeder`, solo en desarrollo (`Seed:Enabled=true`):
  - 1 institución y 3 campus;
  - 1 administrador, 2 docentes y 30 alumnos (contraseña común documentada en `.env.example`);
  - 3 clases con equipos ya armados, tareas en distintos estados, entregas, mensajes y una pizarra con contenido.
- El seed es idempotente: no duplica si ya existe.
- Producción solo siembra la institución y el primer administrador, desde variables de entorno.

## Normalización

Tercera forma normal por defecto. Desnormalizar solo con una métrica que lo justifique y documentándolo en la `Configuration`.
