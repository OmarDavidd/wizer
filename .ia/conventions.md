# Wizer — Convenciones de código

## Idioma

| Qué | Idioma |
|-----|--------|
| Código, nombres, comentarios, logs, tests | Inglés |
| Textos de la interfaz | Español neutro, tratando al usuario de **tú** ("Inicia sesión", "Elige un equipo") |
| `.ia/`, `docs/`, `odd/` | Español |
| Commits, nombres de ramas y PRs | Inglés (PR con descripción en español permitida) |

## C#

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| Clases, records, interfaces, métodos, propiedades | PascalCase | `ClassService`, `JoinAsync` |
| Interfaces | Prefijo `I` | `IClassRepository` |
| Variables y parámetros | camelCase | `classId` |
| Campos privados | `_camelCase` | `_repository` |
| Constantes | PascalCase | `MaxTeamSize` |
| Métodos async | Sufijo `Async` | `GetByIdAsync` |
| DTOs | `<Verb><Noun>Request`, `<Noun><Variant>Response` | `CreateAssignmentRequest`, `SubmissionSummaryResponse` |
| Validators | `<Request>Validator` | `CreateAssignmentRequestValidator` |
| Eventos | Sustantivo + verbo en pasado | `SubmissionGraded` |

- `Nullable` habilitado y warnings como errores en `Release`.
- `sealed` por defecto en las clases que no están pensadas para herencia.
- Constructores primarios para la inyección de dependencias cuando sea legible.
- Un tipo público por archivo.
- `var` cuando el tipo es evidente.

## TypeScript / React

| Elemento | Convención | Ejemplo |
|----------|------------|---------|
| Componentes | PascalCase, archivo `.tsx` con el mismo nombre | `ClassCard.tsx` |
| Hooks | `use` + camelCase | `useClassChatEvents.ts` |
| Stores de Zustand | `use<Name>Store` | `useChatOutboxStore` |
| Schemas Zod | camelCase + `Schema` | `createTeamSchema` |
| Tipos | PascalCase, sin prefijo `I` | `ChatMessage` |
| Constantes | SCREAMING_SNAKE_CASE | `MAX_FILE_SIZE_BYTES` |
| Archivos no componentes | camelCase | `formatDueDate.ts` |
| Carpetas | kebab-case | `features/chat/components` |

- `strict: true`, sin `any` (se usa `unknown` y se acota). `@ts-expect-error` solo con un comentario que lo explique.
- Componentes funcionales con props tipadas mediante `type <Component>Props`.
- Named exports. El default export solo donde el router lo pide.
- Imports con el alias `@/` desde `src/`.
- Lint con **oxlint** (`.oxlintrc.json`, plugins react, react-hooks, jsx-a11y, typescript, import y vitest) y formato con **Prettier** (sin `;`, comillas simples, 100 columnas).

## PostgreSQL

`snake_case`, tablas en plural y claves `uuid`. Ver `database.md`.

## API

Recursos en plural y kebab-case, JSON en camelCase y `/api/v1`. Ver `api.md`.

## Booleanos

Prefijos `is`, `has`, `can`, `allow`: `isLate`, `hasTeam`, `canEdit`, `allowLateSubmissions`.

## Fechas

- Backend: `DateTimeOffset` en UTC, a través de `IClock`.
- Frontend: se formatean con `Intl.DateTimeFormat('es', …)` en la zona del navegador, mediante utilidades de `lib/dates.ts`.

## IDs

- `Guid` (UUID v7) en el backend y `string` en el frontend.
- Nunca exponer IDs incrementales.

## Nullability

- C#: `?` explícito. No devolver `null` en colecciones: se devuelve una vacía.
- TypeScript: `undefined` para lo opcional; `null` solo cuando el contrato de la API lo define.

## Comentarios

- Explican el **por qué**, no el qué.
- Un comentario que repite el código se borra.
- `TODO` solo con contexto: `// TODO(S3 · Chat acceso docente): ...` (referencia a la tarjeta de Trello; `odd/` no se versiona).

## Logging

Message templates y propiedades en PascalCase. Ver `observability.md`.

## Errores

`code` en SCREAMING_SNAKE_CASE. Ver `errors.md`.

## Git

- Ramas: `main` (producción), `develop` (integración), `feat/<scope>-<short>`, `fix/<scope>-<short>`, `chore/...`, `docs/...`.
- Los PR de feature van a `develop`. Las releases son un PR de `develop` a `main`.
- **Conventional Commits**: `feat(chat): add message reactions`, `fix(submissions): reject submit after deadline`.
- Scopes = los módulos o las features: `identity`, `classes`, `teams`, `chat`, `whiteboard`, `assignments`, `submissions`, `files`, `notifications`, `admin`, `infra`, `ci`, `docs`.
- PRs de unas **400 líneas** escritas como guía (sin contar el código generado). Si una feature crece más, se divide en PRs encadenados.
- Sin atribución de IA en los commits.
