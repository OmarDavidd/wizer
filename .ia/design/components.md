# Componentes y patrones

Base: shadcn/ui adaptado a los tokens. Los componentes genéricos viven en `components/ui` y `components/common`. Los de negocio, en su feature.

## Botones (`Button`)

| Variante | Fondo / texto | Uso |
|----------|---------------|-----|
| `brand` | `brand` / blanco | Acción principal de la marca ("Nueva clase", "Exportar PNG") |
| `dark` | `ink` / blanco | Acción decisiva de un formulario o un flujo ("Entrar", "Entregar tarea", "+ Nuevo equipo") |
| `secondary` | `surface` + borde `border-strong` 1.5px / `ink` | Alternativas ("Unirme con código", "Armar al azar") |
| `accent` | `accent` / `ink` | Acción junto a un input de código ("Unirme") o un CTA sobre fondo oscuro |
| `ghost` | transparente / `text-2` | Íconos de barras (deshacer, zoom) |
| `danger` | `danger-bg` / `danger` | Acciones destructivas |

- Tamaños: `md` 44px de alto (por defecto), `lg` 50–52px (CTA de formulario), `sm` 32–36px (solo en barras densas, con un área táctil de 44 px con padding).
- Radio `lg` (12px) en md y lg, `md` (10px) en sm.
- Un botón que solo tiene ícono lleva `aria-label`.
- Máximo **un** botón `brand` o `dark` por bloque visual.

## Inputs

- Alto 48px, radio 12px, borde 1.5px `border-strong`, fondo `surface`, texto Inter 15px.
- `label` visible arriba (Inter 600 13px). Si se oculta visualmente, se usa `sr-only`, nunca se omite.
- Error: borde `danger`, mensaje 13px debajo, `aria-invalid` y `aria-describedby`.
- **Input de código de clase**: borde punteado 1.5px, Inter 600, tracking de 0.12–0.2em, mayúsculas, `maxLength=6`.

## Control segmentado (`SegmentedControl`)

Track `track` con padding de 4px y radio 10–12px. Segmento activo `surface`, Inter 600; inactivo transparente, Inter 500. Se usa en: Iniciar sesión / Crear cuenta, General / Mi equipo, Activas / Archivadas y en los filtros de entregas.

## Chips y badges

- **Pill** (radio pill, padding 4–8 × 10–14px, 12–14px): estados ("Entregada", "En vivo", "Tarde").
- **Chip cuadrado** (radio 8px, `chip`): datos ("24 alumnos", "6 equipos").
- **Contador** (pill de 22px de alto, `ink`/blanco o `accent`/`ink`): no leídos en la navegación.

Mapa de estados de una entrega:

| Estado | Fondo | Texto |
|--------|-------|-------|
| Calificada | `success-bg` | `success` |
| Por calificar | `accent-softer` | `#6B4500` |
| Entregó tarde | `danger-bg` | `danger` |
| Sin entregar / Borrador | `chip` | `text-2` |

## Tarjetas

- **Panel**: `surface`, borde `border` 1px, radio 20px, padding 22px.
- **Tarjeta de clase**: un header de 104px con el color de la clase (código en pill semitransparente y nombre en `card-title` blanco), y un cuerpo con metadatos y chips. Es un enlace entero.
- **Tarjeta urgente** (próxima entrega): fondo `accent-soft` y bloque de fecha con fondo `accent`; o variante `ink` con texto blanco para destacar la más próxima.
- **Ítem de tarea**: radio 14px, fondo `ground` o `accent-soft` si es urgente. Ícono en un cuadrado de 44px.
- **Panel oscuro** (actividad del equipo): fondo `ink`, texto blanco, metadatos `#A3A7B1`.
- Prohibido: borde de color a la izquierda, degradados, sombras en las tarjetas normales.

## Avatares

Círculo con iniciales (2 letras, Inter 600 11–14px) en un fondo pastel (`tokens.md`). Tamaños: 28, 30, 32, 34 y 36px. Los grupos se superponen con un margen de -8px y un borde de 2px del color de fondo, con "+N" al final.

## Navegación

- **Barra lateral** (inicio y páginas globales): 248px de ancho, `surface`, borde derecho. Ítems de 44px con radio 10px. El activo usa `brand-soft` y texto `brand` 600. Debajo, el grupo "Clases" o "Mis equipos" con un cuadrado de color de 10px. El usuario va abajo en un bloque `ground`.
- **Pestañas de la clase**: sobre el header de marca. La activa tiene fondo `ground` con radio superior de 12px, como si continuara la página; las inactivas, texto blanco.

## Chat

- **Burbuja propia**: alineada a la derecha, `brand`, texto blanco, radio `16 16 4 16`.
- **Burbuja ajena**: avatar de 32px, nombre 600 + "· campus · hora" en `subtle`, burbuja `bubble` con radio `4 16 16 16`.
- **Editado**: "(editado)" en 11px `subtle`. **Eliminado**: "Mensaje eliminado" en itálica `subtle`, sin burbuja de color.
- **Reacciones**: pills de 24px bajo la burbuja (emoji + número). La reacción propia lleva borde `brand`.
- **Mención**: `@Nombre` en 600 con fondo `brand-soft`.
- **Escribiendo**: "Jorge está escribiendo…" en itálica 13px `muted`.
- **Mensaje pendiente**: opacidad 0.6 y un ícono de reloj. **Fallido**: ícono de alerta en `danger` con "Reintentar".
- **Compositor**: botón para adjuntar (44px, `chip`), input de 44px y botón de enviar `dark` de 44px.
- **Presencia**: punto de 8px `success-dot` + "11 conectados" en `success`.
- **Aviso de acceso del docente**: una banda `accent-soft` sobre los mensajes: "La docente tiene acceso a este chat · Revocar".

## Tablas (entregas)

- Contenedor con borde `border` y radio 14px, `overflow: auto`, encabezado sticky en `surface-muted` (12px 600 `muted`).
- Filas de 52px con borde superior `border` y celdas con padding de 10 × 16px.
- La nota es un input de 56px, centrado, Inter 600.
- En móvil la tabla tiene scroll horizontal dentro de su caja (ancho mínimo de 640px).

## Tiles de estadísticas

Radio 14px, padding 14 × 16px. Etiqueta en 12px 600 y número en `stat`. Variantes por color: `brand-soft`, `accent-soft`, `chip` y `danger-bg`.

## Barra de progreso

Alto 6px, track `#EEEBE3`, relleno `brand`, radio pill.

## Zona para soltar archivos

Borde punteado de 2px `border-dashed`, radio 14px, ícono de subida de 26px, título 600 14px y ayuda 12px ("PDF, DOCX o imágenes · hasta 25 MB"). Al arrastrar encima: borde `brand` y fondo `brand-soft`.

## Archivos

Ícono de tipo en un cuadrado de 36–40px con radio 9–10px y la extensión en 10–11px 700. PDF: `danger-bg` + `danger-strong`; DOC: `info-bg` + `info`; imágenes y PNG: `success-bg` + `success`.

## Estados vacíos (`EmptyState`)

Ícono lucide de 28px en `text-2`, título 600 15px, una línea de ayuda y una acción. Ejemplo: "Todavía no hay tareas · Crea la primera tarea".

## Pizarra

- Barra de herramientas vertical a la izquierda: un grupo `surface` con radio 16px y botones de 44px. La herramienta activa usa `brand-soft` con el ícono en `brand`.
- Barra inferior flotante: colores (círculos de 30px, el activo con anillo `brand-subtle`) y grosores. Usa `shadow-float`.
- Cursores remotos: flecha del color del participante y una etiqueta pill con el nombre.
- Se usa la UI propia de Excalidraw ajustada con estas reglas (`UIOptions`). No se construye un canvas propio.

## Íconos

`lucide-react` con stroke 2, en tamaños 16, 18 y 20 (en barras), y 26 o 28 (estados vacíos y zonas para soltar). Sin emojis como íconos (salvo las reacciones del chat).
