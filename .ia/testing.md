# Wizer — Reglas de pruebas

## Objetivo

Estrategia de pruebas automatizadas del backend y el frontend.

## Principios

1. Probar el **comportamiento observable** (resultado, errores, efectos persistidos y eventos emitidos), no los detalles internos.
2. **TDD obligatorio en las reglas de negocio** de los Services: primero la prueba que falla (RED), después la implementación (GREEN), después el refactor.
3. Cada regla de `business-rules.md` tiene al menos una prueba, y su nombre o comentario cita el ID (`BR-SUB-03`).
4. Una prueba nunca se borra o se saltea solo para que el suite pase.
5. Las pruebas son deterministas: la hora sale de `IClock` y los IDs de `IIdGenerator`; nada de `Thread.Sleep`.

## Pirámide

| Nivel | Backend | Frontend |
|-------|---------|----------|
| Unitarias | xUnit + Moq | Vitest + React Testing Library |
| Integración | `WebApplicationFactory` + Testcontainers (PostgreSQL, MinIO) | Vitest + RTL + MSW (containers) |
| Tiempo real | `HubConnection` contra `TestServer` | Vitest con un hub simulado |
| E2E | — | Playwright contra `docker-compose.ci.yml` |

## Backend

### Ubicación

```text
apps/backend/tests/
├── Wizer.UnitTests/
│   └── Modules/<Module>/
│       ├── Services/<Service>Tests.cs
│       ├── Validators/<Validator>Tests.cs
│       └── Classes/<Helper>Tests.cs
└── Wizer.IntegrationTests/
    ├── Infrastructure/      WizerApiFactory, contenedores, autenticación de prueba
    └── Modules/<Module>/
        ├── <Feature>EndpointsTests.cs
        └── <Hub>Tests.cs
```

### Unitarias

Prioridad:

- reglas de negocio de los Services (plazos, estados, cupos, pertenencia);
- validators;
- clases con lógica (`JoinCodeGenerator`, `SceneReconciler`, `PresenceTracker`, `CursorEncoder`);
- casos exitosos, inválidos, inexistentes, conflictos y límites.

Nombres: `Method_Scenario_ExpectedResult`, por ejemplo `SubmitAsync_AfterDeadlineWithoutLate_ThrowsDeadlinePassed`.

No crear pruebas triviales de Controllers.

### Integración

- `WizerApiFactory` levanta la API con PostgreSQL y MinIO **reales** usando Testcontainers. Un contenedor por colección de pruebas, y la base se limpia entre pruebas con Respawn.
- Autenticación de prueba: un helper hace login real y conserva las cookies.
- Cubren: autorización (401, 403 y 404 por pertenencia), contrato de errores (`code`), persistencia, índices únicos y concurrencia (409).
- Los repositories se prueban **solo** acá, contra PostgreSQL real, nunca con mocks de EF Core ni con el proveedor InMemory.
- ClamAV se reemplaza por un doble configurable (limpio o infectado).
- El correo se captura con un `IEmailSender` en memoria.

### Tiempo real

- Hubs: conexión autenticada, unión a grupos con y sin permiso, recepción del evento después de un POST, y límites de frecuencia.
- `SceneReconciler`: tabla de casos (versión mayor, empate por nonce, tombstones).

## Frontend

### Ubicación

Junto al código: `MessageBubble.test.tsx` al lado de `MessageBubble.tsx`. Setup, handlers de MSW y factories en `src/test/`.

### Qué probar

- **Presentacionales**: render por props, interacción y accesibilidad (roles y labels).
- **Containers**: con MSW; estados de carga, vacío, error y éxito.
- **Hooks**: `renderHook` con un `QueryClient` de prueba.
- **Stores** de Zustand: transiciones (por ejemplo, el outbox del chat).
- **Formularios**: validación Zod, errores del backend aplicados al campo y envío.
- **Errores**: el mapa de `code` → mensaje.

Consultar por rol y texto (`getByRole`, `getByLabelText`). `data-testid` solo cuando no haya una alternativa accesible.

### E2E (Playwright)

En `apps/frontend/e2e/`, contra el stack de `docker-compose.ci.yml` (con Mailpit para leer los códigos de verificación).

Flujos críticos del MVP:

1. Registro → verificación por código → login.
2. El docente crea una clase → el alumno se une con el código.
3. Chat: dos navegadores; un mensaje aparece en el otro; edición y reacción.
4. El docente crea una tarea → el alumno entrega un PDF → el docente califica → el alumno ve la nota.
5. Pizarra: dos navegadores; un trazo aparece en el otro.
6. Equipos: el docente arma equipos al azar.

## Cobertura

- Sin gate global de cobertura.
- **Objetivo: 80 % de líneas** en `Modules/*/Services`, `Validators` y `Classes`. CI publica el reporte (Coverlet + ReportGenerator, y `vitest --coverage`) en el resumen del PR.
- La cobertura no reemplaza las pruebas de reglas: una regla sin prueba es un hueco aunque la cobertura sea alta.

## Datos de prueba

- Builders y factories (`ClassBuilder`, `makeMessage()`), sin fixtures JSON gigantes.
- Datos mínimos por prueba. Nada compartido y mutable entre pruebas.

## Ejecución

```bash
# Backend (desde apps/backend)
dotnet test tests/Wizer.UnitTests
dotnet test tests/Wizer.IntegrationTests       # requiere Docker
dotnet format Wizer.slnx --verify-no-changes

# Frontend (desde apps/frontend)
npm test
npm run typecheck
npm run lint
npm run e2e                                    # requiere el stack de CI levantado
```

## Antes de terminar una tarea

- Correr las pruebas relacionadas y reportar el resultado **real**.
- No afirmar que una prueba pasó si no se ejecutó.
- Si una prueba falla y no se puede arreglar dentro de la tarea, reportarla con la salida.
