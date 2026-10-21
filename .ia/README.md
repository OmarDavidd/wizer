# Wizer — Instrucciones para IA

Este directorio contiene las reglas de arquitectura, desarrollo y operación que deben respetar los agentes de IA que trabajan en Wizer.

## Regla principal

Antes de crear, modificar o eliminar código, el agente DEBE leer este archivo y después los documentos de `.ia/` relacionados con la tarea.

El agente NO debe introducir tecnologías, patrones, librerías o cambios de arquitectura sin autorización explícita.

Si una decisión no está documentada y afecta de forma importante al proyecto, el agente pide confirmación antes de implementarla.

## Stack oficial

### Backend
- C# · .NET 10 (LTS)
- ASP.NET Core Web API
- Entity Framework Core + Npgsql
- FluentValidation
- SignalR (tiempo real)
- JWT en cookies HttpOnly + login con Google
- Serilog
- ASP.NET Core Rate Limiting (integrado)
- Microsoft.AspNetCore.OpenApi + Scalar (documentación de la API)
- xUnit · Moq · Testcontainers

### Frontend
- React 19 + Vite (SPA)
- TypeScript en modo estricto
- TanStack Router (rutas tipadas)
- TanStack Query (estado del servidor)
- Zustand (estado global del cliente)
- React Hook Form + Zod
- Tailwind CSS v4
- shadcn/ui
- Axios
- Orval (cliente y tipos generados desde OpenAPI)
- `@microsoft/signalr`
- Excalidraw (pizarra)
- lucide-react (íconos)
- Vitest · React Testing Library · MSW · Playwright

### Datos y almacenamiento
- PostgreSQL
- MinIO (archivos)
- ClamAV (escaneo antivirus)

### Infraestructura
- Docker + Docker Compose (`docker-compose.dev.yml`, `docker-compose.prod.yml`, `docker-compose.ci.yml`)
- GitHub Actions (CI)
- Home Lab self-hosted, sin depender de Coolify
- Reverse proxy con un solo origen (`/`, `/api`, `/hubs`, `/storage`)

### Correo
- Resend en producción
- Mailpit en desarrollo y CI

### Observabilidad
- `ILogger<T>` en el código, Serilog como proveedor
- JSON estructurado a stdout
- Grafana Alloy → Loki → Grafana
- Correlation ID por petición

### Decisiones de stack

| Tema | Alternativa descartada | Wizer | Motivo |
|------|--------|-------|--------|
| Frontend | Next.js | Vite SPA + TanStack Router | Todo está detrás del login y es muy de tiempo real; no hace falta SSR |
| Cliente HTTP | Servicios a mano | Orval genera tipos, schemas Zod y hooks | Front y back nunca se desincronizan |
| Tiempo real | — | SignalR | Chat, presencia, pizarra y notificaciones |
| CI | Jenkins | GitHub Actions | Sin servidor de CI que mantener |
| Pruebas de integración | Unitarias | + Testcontainers y Playwright | El tiempo real y los archivos no se validan con mocks |
| Orígenes | Front y API separados | Un solo origen tras reverse proxy | Cookies `SameSite=Strict` sin CORS, y SignalR con cookies |

## Convenciones de idioma

- Código interno en inglés: clases, métodos, variables, DTOs, endpoints, tablas, columnas, logs y tests.
- Interfaz visible en español.
- Documentación de `.ia/`, `docs/` y `odd/` en español.
- PostgreSQL en `snake_case`. C# en PascalCase (tipos y propiedades) y camelCase (variables). TypeScript en camelCase, componentes en PascalCase.

## Arquitectura oficial

### Backend
Monolito modular. Organización por módulos de negocio dentro de `Modules/`.

Flujo normal:

`Controller -> IService -> Service -> IRepository -> Repository -> EF Core -> PostgreSQL`

Flujo de tiempo real:

`Service -> I<Module>Notifier -> Hub (IHubContext) -> clientes`

No se usa MediatR ni CQRS. No hay Generic Repository.

### Frontend
Arquitectura por features dentro de `src/features/`, con el patrón container/presentational.

Flujo normal:

`Route -> Container -> Hook (generado o propio) -> TanStack Query -> Axios -> API`

Flujo de tiempo real:

`Hub connection (shared) -> feature realtime hook -> actualización de caché de TanStack Query`

## Reglas no negociables

1. No cambiar la arquitectura sin autorización.
2. No introducir MediatR, CQRS, AutoMapper ni Generic Repository.
3. No poner lógica de negocio en Controllers ni en Hubs.
4. No acceder a repositories ni `DbSet` de otros módulos.
5. Los módulos se comunican mediante interfaces públicas (`I<Module>ModuleService`) y Dependency Injection.
6. No exponer entidades de EF Core fuera de su módulo ni por la API.
7. No guardar access tokens ni refresh tokens en localStorage, sessionStorage, IndexedDB ni Zustand.
8. Los tokens viven en cookies HttpOnly emitidas por el backend.
9. La identidad del usuario sale **siempre** de la sesión autenticada, nunca de un `userId` enviado por el cliente.
10. Toda operación protegida valida la autorización en el backend: rol de plataforma **y** pertenencia a la clase o al equipo.
11. Las mutaciones van por REST. Los hubs de SignalR solo emiten eventos y reciben señales efímeras (escribiendo, cursores, trazos de pizarra).
12. TanStack Query administra los datos remotos. Zustand guarda solo estado no sensible del cliente.
13. El cliente de la API se genera con Orval. No escribir a mano tipos que ya genera Orval.
14. Los componentes de `components/ui` son genéricos. Los de negocio viven en su feature.
15. Los archivos se suben y descargan con URLs firmadas. Ningún archivo se descarga antes de pasar el escaneo antivirus.
16. Los logs usan structured logging y message templates. Nunca registrar contraseñas, tokens, cookies, secretos ni contenido de mensajes privados.
17. Todo servicio corre con Docker Compose. El proyecto se despliega sin Coolify.
18. Las reglas de negocio de los Services se escriben con prueba primero (TDD).
19. La UI respeta `design/`. No inventar colores, tipografías, radios ni sombras.
20. Las reglas de `business-rules.md` y `permissions.md` no se modifican sin autorización.
21. Los errores de la API siguen el contrato de `errors.md`.
22. Pedir confirmación antes de push, PR, migraciones o nuevas dependencias (`workflow.md` §12).

## Qué documento leer según la tarea

| Tema | Documento |
|------|-----------|
| Contexto funcional | `project.md` |
| Arquitectura general | `architecture.md` |
| Backend | `backend.md` |
| Frontend | `frontend.md` |
| Tiempo real (SignalR, pizarra) | `realtime.md` |
| Archivos (MinIO, ClamAV) | `files.md` |
| Base de datos | `database.md` |
| API | `api.md` |
| Errores | `errors.md` |
| Autenticación | `authentication.md` |
| Roles y permisos | `permissions.md` |
| Reglas de negocio | `business-rules.md` |
| Seguridad | `security.md` |
| Pruebas | `testing.md` |
| Logging | `observability.md` |
| Docker, CI, despliegue | `infrastructure.md` |
| Convenciones | `conventions.md` |
| Flujo para agentes | `workflow.md` |
| Diseño UI/UX | `design/README.md` |

## Regla de precedencia

Si el código existente y `.ia/` se contradicen, el agente no asume cuál es correcto. Informa la contradicción y pide confirmación cuando afecta la arquitectura, la seguridad, los datos o los contratos de la API.
