# AGENTS.md — Wizer

Punto de entrada para cualquier agente de IA (Claude Code, Codex, OpenCode u otro) que trabaje en este repositorio.

La arquitectura, las convenciones, la infraestructura, la estrategia de pruebas y el flujo de trabajo están definidos en `.ia/`. **Leer y respetar esos documentos antes de modificar el proyecto es obligatorio.**

---

## 1. Qué es Wizer

Plataforma web para clases semipresenciales: varias sedes (campus) conectadas a una misma clase, con chat en tiempo real, equipos, pizarra colaborativa, tareas, entregas y archivos.

- Requisitos funcionales: `docs/01-requisitos.md`
- Contexto para agentes: `.ia/project.md`

---

## 2. Orden de lectura obligatorio

Antes de cualquier cambio:

1. `.ia/README.md`
2. `.ia/project.md`
3. `.ia/architecture.md`
4. `.ia/workflow.md`

Después, los documentos de la tarea:

| Tarea | Leer además |
|-------|-------------|
| Backend | `backend.md`, `database.md`, `api.md`, `errors.md`, `testing.md`, `conventions.md` |
| Frontend | `frontend.md`, `api.md`, `errors.md`, `testing.md`, `conventions.md`, `design/README.md` |
| Chat, presencia, pizarra, notificaciones en vivo | `realtime.md`, `backend.md`, `frontend.md` |
| Archivos y entregas | `files.md`, `security.md`, `business-rules.md` |
| Autenticación o sesiones | `authentication.md`, `security.md`, `permissions.md` |
| Permisos o reglas de negocio | `permissions.md`, `business-rules.md` |
| Base de datos | `database.md`, `backend.md`, `conventions.md` |
| Docker, CI, despliegue | `infrastructure.md`, `observability.md` |
| Logs | `observability.md`, `backend.md` |
| UI / diseño | `design/README.md` y los archivos que indique |

---

## 3. Stack oficial (resumen)

| Capa | Tecnología |
|------|------------|
| Backend | C# · .NET 10 (LTS) · ASP.NET Core Web API · EF Core + Npgsql · FluentValidation · SignalR · Serilog |
| Frontend | React 19 · Vite · TypeScript estricto · TanStack Router · TanStack Query · Zustand · React Hook Form + Zod · Tailwind CSS v4 · shadcn/ui · Axios · Orval · Excalidraw |
| Datos | PostgreSQL · Garage (S3) · ClamAV |
| Pruebas | xUnit · Moq · Testcontainers · Vitest · React Testing Library · MSW · Playwright |
| Infraestructura | Docker · Docker Compose (dev / prod / ci) · GitHub Actions · Home Lab |
| Observabilidad | `ILogger<T>` + Serilog JSON → Grafana Alloy → Loki → Grafana |

El detalle y lo que está prohibido introducir están en `.ia/README.md`.

**Comandos:** `make help` en la raíz lista todo (levantar la infraestructura, la API, el frontend, las pruebas, el lint y la regeneración del cliente de la API). El detalle está en `.ia/infrastructure.md` §Comandos.

---

## 4. Reglas de idioma

- **Código en inglés**: clases, métodos, variables, DTOs, endpoints, tablas, columnas, logs, tests y mensajes técnicos.
- **Interfaz en español**: todo texto visible para el usuario.
- **Documentación en español**: `.ia/`, `docs/`, `odd/` (este último es local y no se versiona).
- **Commits en inglés** con Conventional Commits.

---

## 5. Nunca sin preguntar

El agente DEBE pedir confirmación antes de:

- hacer `git push`, abrir, aprobar o mergear un PR;
- crear, modificar o eliminar migraciones de base de datos;
- agregar, quitar o actualizar dependencias (NuGet o npm);
- cambiar el contrato público de la API o de los hubs de SignalR;
- cambiar la arquitectura, la autenticación, la autorización o el modelo de datos;
- ejecutar operaciones destructivas sobre datos o sobre el repositorio;
- tocar la configuración de CI, Docker Compose de producción o secretos.

Los commits locales en la rama de trabajo están permitidos cuando la tarea lo requiera.

---

## 6. Carpetas que no se tocan

- `wizer-backend/` y `wizer-frontend/`: **proyecto anterior**, solo como referencia histórica. No copiar su arquitectura (usaba MediatR, AutoMapper y Supabase Auth, todos descartados).

---

## 7. Flujo de trabajo

El flujo completo está en `.ia/workflow.md`. Resumen:

1. Leer los documentos que corresponden.
2. Inspeccionar el código existente antes de crear algo nuevo.
3. Para features grandes, usar el documento ODD en `odd/tasks/<feature>.md` (local, no se sube).
4. Prueba primero (RED → GREEN) en reglas de negocio.
5. Cambio mínimo, sin refactors no pedidos.
6. Correr las pruebas relacionadas y reportar el resultado real.
7. Resumir qué cambió, qué se probó y qué queda pendiente.

---

## 8. Precedencia

1. Instrucción explícita y actual del usuario.
2. `.ia/` (este repositorio).
3. Código existente.

Si `.ia/` y el código se contradicen, el agente no elige en silencio: informa la contradicción y pregunta cuando afecta la arquitectura, la seguridad, los datos o la API.
