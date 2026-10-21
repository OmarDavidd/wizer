# Wizer — Reglas del frontend

## Objetivo

Reglas obligatorias para desarrollar el frontend: una SPA con React y Vite que consume la API REST y los hubs de SignalR del backend.

## Stack

| Librería | Uso |
|----------|-----|
| React 19 + Vite 8 | SPA |
| TypeScript 6 (`strict`, `noUncheckedIndexedAccess`) | Tipado |
| TanStack Router | Rutas tipadas, loaders y guards |
| TanStack Query | Estado del servidor y caché |
| Zustand | Estado global del cliente (no sensible) |
| React Hook Form + Zod | Formularios y validación |
| Tailwind CSS v4 | Estilos |
| shadcn/ui | Componentes base |
| Axios | Cliente HTTP (mutator de Orval) |
| Orval | Genera tipos, schemas Zod y hooks de TanStack Query desde OpenAPI |
| `@microsoft/signalr` | Tiempo real |
| `@excalidraw/excalidraw` | Pizarra |
| `react-pdf` | Vista previa de PDF |
| lucide-react | Íconos |
| Vitest + React Testing Library + MSW 3 | Pruebas (MSW 3: `server.listen({ onUnhandledFrame: 'error' })`) |
| Playwright | Pruebas E2E (S4) |
| oxlint + Prettier | Lint y formato |
| `@fontsource-variable/*` | Fuentes autoalojadas (Bricolage Grotesque, Inter) |

No agregar librerías sin autorización (`workflow.md` §12).

## Idioma

- Código, nombres de archivos, tipos y variables en inglés.
- Textos visibles en español, escritos en el componente.
- Las rutas (URLs) que ve el usuario van en español: `/clases`, `/tareas`, `/perfil`.

## Estructura

```text
apps/frontend/src/
├── main.tsx                   monta <Providers /> e importa theme.css
├── routeTree.gen.ts           generado por el plugin del router (versionado, no editar)
├── app/
│   ├── providers.tsx          QueryClient (reintentos según errors.md) + Router
│   ├── theme.css              Tailwind v4 + tokens de design/tokens.md en @theme
│   └── layouts/               AppShell, ClassLayout, AuthLayout (design/layouts.md)
├── routes/                    rutas por archivo (__root.tsx, index.tsx y las de cada feature)
├── features/<feature>/
├── components/
│   ├── ui/                    primitivas de shadcn ajustadas a los tokens
│   └── common/                genéricos compuestos: Avatar, EmptyState…
├── api/
│   ├── generated/             Orval (NO EDITAR): endpoints/, model/, zod/
│   ├── client.ts              Axios (withCredentials) + mutator `customInstance`
│   └── errors.ts              normalización de ProblemDetails (errors.md)
├── realtime/                  conexiones de SignalR
├── stores/                    Zustand globales (session, ui)
├── lib/                       utilidades sin dominio (`cn`, fechas)
└── test/                      setup.ts, msw/ (handlers, server), helpers de render
```

Las rutas usan el alias `@/` (configurado en `vite.config.ts` y `tsconfig.app.json`). Los componentes de shadcn se agregan con `npx shadcn@latest add <componente>` (`components.json` ya apunta a `src/components/ui` y a `theme.css`) y después se ajustan a los tokens.

## Arquitectura por features

```text
features/chat/
├── containers/        se conectan a datos (hooks) y pasan props
├── components/        presentacionales: solo props, sin hooks de datos
├── hooks/             hooks propios (realtime, composiciones de queries)
├── stores/            Zustand de la feature (si hace falta)
├── schemas/           Zod de formularios propios (si Orval no alcanza)
├── utils/
├── constants.ts
└── index.ts           API pública de la feature
```

### Container / presentational

- **Containers** (`ChatPanelContainer`): usan hooks, manejan carga y error, y deciden qué renderizar.
- **Presentational** (`ChatPanel`, `MessageBubble`): reciben props y emiten callbacks. Sin `useQuery`, sin stores y sin acceso a la API.
- Las pruebas de componentes presentacionales no necesitan MSW. Los containers se prueban con MSW.

### Límites entre features

- Una feature importa de otra solo desde su `index.ts`.
- `components/ui` y `components/common` no importan de `features/`.
- Los componentes de negocio (`ClassCard`, `TeamBoard`, `SubmissionTable`) viven en su feature, nunca en `components/ui`.

## Rutas

TanStack Router con rutas basadas en archivos.

| Ruta | Pantalla | Acceso |
|------|----------|--------|
| `/ingresar`, `/registro`, `/verificar-correo`, `/recuperar-contrasena` | Acceso | Anónimo |
| `/inicio` | Inicio (docente: Mis clases; alumno: pendientes y equipo) | Autenticado |
| `/clases/$classId` | Clase: inicio y chat | Miembro |
| `/clases/$classId/equipos` | Equipos | Miembro |
| `/clases/$classId/equipos/$teamId/pizarra` | Pizarra | Miembro del equipo o docente |
| `/clases/$classId/tareas` | Tareas (docente: sección por tarea) | Miembro |
| `/clases/$classId/tareas/$assignmentId` | Tarea y entrega | Miembro |
| `/clases/$classId/archivos` | Archivos | Miembro |
| `/tareas` | Mis tareas (alumno) | Autenticado |
| `/notificaciones` | Notificaciones | Autenticado |
| `/perfil` | Perfil | Autenticado |
| `/admin/usuarios`, `/admin/campus` | Administración | Admin |

- Las rutas protegidas usan `beforeLoad` con la sesión (`/auth/me`). Sin sesión, redirigen a `/ingresar?next=…`.
- `next` solo acepta rutas internas: empieza con un único `/` y no apunta a pantallas de acceso. Así se evita un open redirect.
- Ocultar un botón no es seguridad: el backend siempre valida.

## Estado

| Tipo de estado | Herramienta |
|----------------|-------------|
| Datos del servidor | TanStack Query (hooks de Orval) |
| Eventos en tiempo real | Actualizan la caché de TanStack Query (`setQueryData` o `invalidateQueries`) |
| Global del cliente no sensible | Zustand (`ui`, borradores, outbox del chat) |
| Persistido no sensible | Zustand `persist` (pestaña elegida, panel colapsado) |
| Local | `useState` / `useReducer` |
| Formularios | React Hook Form |

No duplicar en Zustand datos que ya están en TanStack Query.

## API

- El cliente se genera con **Orval** desde el OpenAPI del backend (`orval.config.ts`): tipos, schemas Zod y hooks de TanStack Query.
- `src/api/generated/` (`endpoints/`, `model/`, `zod/`) no se edita a mano. Se regenera con `make api-client` (compila el backend, que escribe `apps/backend/openapi/wizer-api.json`, y corre Orval con `httpClient: axios`).
- CI verifica que el cliente generado esté al día (`infrastructure.md`).
- Axios (`api/client.ts`) usa `baseURL: '/api/v1'` y `withCredentials: true`.
- Un interceptor maneja el 401: hace **un** refresh compartido entre peticiones concurrentes, repite la petición original una vez y, si falla, limpia la sesión y redirige a `/ingresar`.
- Los errores se normalizan en `api/errors.ts` según `errors.md`. Las features no parsean errores por su cuenta.

## Tiempo real

Ver `realtime.md`. En resumen:

- Una conexión por hub (`chat`, `whiteboard`, `notifications`), creada en `src/realtime/` y compartida.
- Las features se suscriben con hooks (`useClassChatEvents(classId)`) que actualizan la caché.
- Al reconectar se vuelve a unir a los grupos y se resincroniza.

## Formularios

- React Hook Form + `zodResolver`.
- Si el endpoint tiene schema de Orval, reutilizarlo y extenderlo para los mensajes en español. No duplicar reglas.
- Los errores de campo del backend (`errors`) se aplican con `setError`.

## Estilos

- Tailwind CSS v4 con los tokens de `design/tokens.md` definidos en `@theme`.
- No usar colores hexadecimales sueltos en los componentes: usar los tokens.
- Diseño mobile-first desde 360 px (`design/responsive.md`).
- shadcn/ui se adapta a los tokens. No crear un segundo sistema de componentes.

## Accesibilidad

- Elementos semánticos: `button`, `a`, `label` + `input` y `table` para datos tabulares.
- Los botones que solo tienen ícono llevan `aria-label`.
- Foco visible y navegación completa con teclado (incluida la pizarra, en lo que Excalidraw permita).
- Contraste AA. Áreas táctiles de 44 px como mínimo.

## `data-testid`

Obligatorio en los elementos que usan las pruebas E2E y de integración: formularios, botones de acción, listas y estados (vacío, error, cargando). Formato en kebab-case con el prefijo de la feature: `data-testid="chat-message-input"`, `data-testid="submission-submit-button"`. Ver `design/rules.md`.

## Estados de UI

Toda vista que carga datos contempla: **cargando** (skeleton), **vacío** (EmptyState con acción), **error** (mensaje + reintentar) y **sin permiso** (403).

## Rendimiento

- División de código por ruta (lazy routes).
- Excalidraw y `react-pdf` se cargan solo en sus rutas.
- Las listas largas (mensajes, entregas) se virtualizan cuando superan unos 100 elementos.

## Pruebas

Ver `testing.md`.
