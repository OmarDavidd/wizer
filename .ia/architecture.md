# Wizer — Arquitectura

## Vista general

```text
                         Navegador
                             │
                             ▼
              ┌──────────────────────────────┐
              │  Reverse proxy (un origen)   │
              │  /          → frontend       │
              │  /api       → backend        │
              │  /hubs      → backend (WS)   │
              │  /storage   → MinIO          │
              └──────────────────────────────┘
                │            │            │
                ▼            ▼            ▼
          ┌─────────┐  ┌───────────┐  ┌───────┐
          │Frontend │  │  Backend  │  │ MinIO │
          │ (nginx, │  │ ASP.NET   │  │       │
          │  SPA)   │  │ Core API  │  └───────┘
          └─────────┘  │ + SignalR │      ▲
                       └───────────┘      │ (URLs firmadas)
                         │   │   │        │
              ┌──────────┘   │   └───────────────┐
              ▼              ▼                    ▼
        ┌──────────┐   ┌─────────┐          ┌─────────┐
        │PostgreSQL│   │ ClamAV  │          │ Resend  │
        └──────────┘   └─────────┘          │ (SMTP/  │
                                            │  API)   │
                                            └─────────┘
```

**Un solo origen**: frontend, API, hubs y descargas comparten dominio detrás del reverse proxy. Esto permite cookies `SameSite=Strict`, evita CORS en producción y deja que SignalR use las cookies de sesión. En desarrollo, el proxy de Vite reproduce la misma topología.

## Monorepo

```text
wizer/
├── AGENTS.md
├── .ia/                      reglas para agentes
├── docs/                     requisitos, planificación, riesgos
├── odd/tasks/                documentos de feature (ODD, local, ignorado por git)
├── apps/
│   ├── backend/
│   │   ├── Wizer.slnx
│   │   ├── src/Wizer.Api/
│   │   └── tests/
│   │       ├── Wizer.UnitTests/
│   │       └── Wizer.IntegrationTests/
│   └── frontend/
│       ├── src/
│       ├── e2e/              Playwright
│       └── orval.config.ts
├── infra/                    reverse proxy, Alloy, scripts de backup
├── docker-compose.dev.yml
├── docker-compose.prod.yml
├── docker-compose.ci.yml
├── .env.example
└── .github/workflows/
```

## Backend

Monolito modular. Un proyecto `Wizer.Api` con módulos de negocio en `Modules/`.

```text
src/Wizer.Api/
├── Modules/
│   ├── Identity/          registro, login, Google, tokens, verificación, recuperación
│   ├── Users/             perfil, perfil académico
│   ├── Campuses/          campus de la institución
│   ├── Classes/           clases, código de acceso, membresías
│   ├── Teams/             equipos y miembros
│   ├── Chat/              canales, mensajes, reacciones, menciones, accesos del docente
│   ├── Whiteboard/        pizarras de equipo
│   ├── Assignments/       tareas y material
│   ├── Submissions/       entregas y calificaciones
│   ├── Files/             metadatos, subidas, escaneo y descargas
│   ├── Notifications/     notificaciones dentro de la plataforma
│   └── Admin/             gestión de usuarios docentes y administradores
├── Infrastructure/
│   ├── Persistence/       AppDbContext, migraciones, seed
│   ├── Storage/MinIO/
│   ├── Antivirus/ClamAV/
│   ├── Email/             Resend y SMTP (Mailpit)
│   ├── Authentication/    JWT, cookies, Google
│   ├── Realtime/          registro de hubs, rastreador de presencia
│   └── DependencyInjection.cs
├── Shared/
│   ├── Exceptions/
│   ├── Errors/            ProblemDetails y códigos
│   ├── Auth/              ICurrentUser, políticas
│   ├── Pagination/
│   ├── Time/              IClock
│   └── Ids/               generación de UUID v7
└── Program.cs
```

Detalles: `backend.md`. Tiempo real: `realtime.md`.

### Dependencias entre módulos

```text
Identity ──► Users
Classes  ──► Users, Campuses
Teams    ──► Classes
Chat     ──► Classes, Teams, Notifications
Whiteboard ► Teams, Classes
Assignments ► Classes, Files, Notifications
Submissions ► Assignments, Teams, Files, Notifications
Files    ──► Classes (cuota y permisos)
Admin    ──► Identity, Users, Campuses
```

Siempre mediante `I<Module>ModuleService`. **Prohibidas las dependencias circulares.** Si dos módulos se necesitan mutuamente, uno de los dos expone un evento en proceso (`backend.md` §Eventos).

## Frontend

SPA con React y Vite. Arquitectura por features.

```text
src/
├── app/                   router, providers, layouts
├── routes/                definición de rutas (TanStack Router)
├── features/
│   ├── auth/
│   ├── classes/
│   ├── teams/
│   ├── chat/
│   ├── whiteboard/
│   ├── assignments/
│   ├── submissions/
│   ├── files/
│   ├── notifications/
│   ├── profile/
│   └── admin/
├── components/
│   ├── ui/                shadcn/ui (genéricos)
│   └── common/            compuestos genéricos (EmptyState, Avatar, PageHeader…)
├── api/
│   ├── generated/         salida de Orval (no editar a mano)
│   ├── client.ts          instancia de Axios
│   └── errors.ts          normalización de errores
├── realtime/              conexiones SignalR compartidas
├── stores/                stores globales de Zustand
└── lib/                   utilidades sin dominio
```

Detalles: `frontend.md`.

## Autenticación

```text
Login (correo o Google)
→ POST /api/v1/auth/...
→ el backend emite cookies HttpOnly (wz_access, wz_refresh)
→ el frontend consulta GET /api/v1/auth/me
→ las conexiones SignalR viajan con las mismas cookies
```

Detalles: `authentication.md`.

## Archivos

```text
Cliente → pide URL de subida → PUT directo a MinIO → confirma
→ ClamAV escanea en segundo plano → archivo limpio y disponible
Cliente → pide URL de descarga (solo limpio y autorizado) → GET a MinIO
```

Detalles: `files.md`.

## Docker

| Servicio | Dev | Prod | CI |
|----------|-----|------|----|
| proxy (Caddy) | ✓ | ✓ | ✓ |
| frontend | Vite dev server | nginx con el build | build |
| backend | `dotnet watch` | imagen publicada | imagen |
| migrator | — (auto en dev) | one-shot | one-shot |
| postgres | ✓ | ✓ | ✓ |
| minio | ✓ | ✓ | ✓ |
| clamav | ✓ | ✓ | ✓ |
| mailpit | ✓ | — | ✓ |
| alloy | opcional | ✓ | — |

Detalles: `infrastructure.md`.

## Observabilidad

```text
ILogger<T> → Serilog (JSON, stdout) → Grafana Alloy → Loki → Grafana
```

Detalles: `observability.md`.

## Decisiones arquitectónicas (registro)

| ID | Decisión | Fecha |
|----|----------|-------|
| ADR-01 | Monolito modular sin MediatR, igual que mezcal | 2026-10-20 |
| ADR-02 | SPA con Vite, sin SSR | 2026-10-20 |
| ADR-03 | Un solo origen detrás de un reverse proxy | 2026-10-20 |
| ADR-04 | SignalR en memoria, una instancia, sin Redis | 2026-10-20 |
| ADR-05 | Mutaciones por REST; hubs solo para eventos y señales efímeras | 2026-10-20 |
| ADR-06 | Excalidraw con sincronización propia por SignalR y snapshots en PostgreSQL | 2026-10-20 |
| ADR-07 | UUID v7 como clave primaria | 2026-10-20 |
| ADR-08 | Rol de clase en la membresía; rol de plataforma en el usuario | 2026-10-20 |
| ADR-09 | Archivos con URLs firmadas y ClamAV asíncrono | 2026-10-20 |
| ADR-10 | Cliente de la API generado con Orval | 2026-10-20 |
| ADR-11 | GitHub Actions en lugar de Jenkins | 2026-10-20 |

Una decisión nueva que cambie la arquitectura se agrega a esta tabla, con autorización del equipo.
