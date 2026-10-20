# Tokens

Definidos como variables CSS en el `@theme` de Tailwind v4 (`src/app/theme.css`). Los componentes usan los tokens, **nunca** valores hexadecimales sueltos.

## Color de marca

| Token | Valor | Uso |
|-------|-------|-----|
| `--color-brand` | `#5B216B` | Color principal: header de la clase, panel de acceso, botones de marca, enlaces, burbujas propias del chat, foco |
| `--color-brand-hover` | `#4A1A57` | Hover y activo del color de marca |
| `--color-brand-soft` | `#F1ECFE` | Fondo del ítem activo en la navegación, tile "Entregadas", herramienta activa de la pizarra |
| `--color-brand-subtle` | `#DDD6FE` | Avatares del docente, texto secundario sobre el color de marca |
| `--color-on-brand-muted` | `#EDE9FE` | Texto secundario sobre fondo de marca |
| `--color-violet` | `#6D28D9` | Acento secundario (detalles del logo, barras de progreso) |

## Acento

| Token | Valor | Uso |
|-------|-------|-----|
| `--color-accent` | `#FFC857` | Lo urgente o destacado: la próxima entrega, el botón "Unirme", el isotipo |
| `--color-accent-soft` | `#FFF6DE` | Fondo de una tarjeta urgente |
| `--color-accent-softer` | `#FFF1CF` | Chips de "por calificar" y "sin leer" |
| `--color-accent-ink` | `#8A5A00` | Texto sobre los fondos de acento |

## Neutros

| Token | Valor | Uso |
|-------|-------|-----|
| `--color-ground` | `#F6F5F1` | Fondo de la app |
| `--color-surface` | `#FFFFFF` | Tarjetas, paneles, inputs |
| `--color-surface-muted` | `#FBFAF7` | Encabezados de tabla, filas de lista |
| `--color-chip` | `#F0EEE7` | Chips neutros, segmentos inactivos |
| `--color-track` | `#EAE8E1` | Fondo de los controles segmentados |
| `--color-bubble` | `#F3F1EB` | Burbujas de chat ajenas |
| `--color-border` | `#E6E3DA` | Bordes de tarjetas |
| `--color-border-strong` | `#D9D6CC` | Bordes de inputs y botones secundarios |
| `--color-border-dashed` | `#CFCBBF` | Zonas para soltar archivos, "crear" |
| `--color-ink` | `#16181D` | Texto principal, botón oscuro |
| `--color-text-2` | `#3F434D` | Texto secundario fuerte, íconos |
| `--color-muted` | `#5B5F6B` | Metadatos |
| `--color-subtle` | `#6B6F7A` | Etiquetas pequeñas, horas |

## Estados

| Token | Valor | Uso |
|-------|-------|-----|
| `--color-success-bg` / `--color-success` | `#DCFCE7` / `#14532D` | Calificada, entregada, "en vivo" |
| `--color-success-dot` | `#16A34A` | Punto de presencia |
| `--color-danger-bg` / `--color-danger` | `#FEE2E2` / `#7F1D1D` | Tarde, error |
| `--color-danger-strong` | `#991B1B` | Etiquetas de tipo PDF, texto de error |
| `--color-info-bg` / `--color-info` | `#DBEAFE` / `#1E3A8A` | Etiquetas de tipo documento |

## Colores de clase

Asignados al crear la clase (rotan en este orden). Cada uno lleva texto blanco encima:

```text
#5B216B  ciruela (marca)
#0F766E  verde azulado
#C2410C  naranja
#1E3A8A  azul
#86198F  magenta
#3F434D  grafito
```

## Avatares

Fondos pastel asignados por hash del ID de usuario, con texto `--color-ink`:

```text
#FFC857 #99F6E4 #FECACA #E9D5FF #FED7AA #BAE6FD
#FDE68A #BFDBFE #FBCFE8 #BBF7D0 #DDD6FE #FEF3C7
```

## Radios

| Token | Valor | Uso |
|-------|-------|-----|
| `--radius-xs` | 7px | Segmento interno |
| `--radius-sm` | 8px | Chips cuadrados |
| `--radius-md` | 10px | Ítems de navegación, botones pequeños |
| `--radius-lg` | 12px | Inputs, botones, ítems de lista |
| `--radius-xl` | 16px | Tarjetas de tareas y archivos |
| `--radius-2xl` | 20px | Paneles y tarjetas de clase |
| `--radius-pill` | 999px | Pills, avatares, badges |

## Espaciado

Base de 4 px (escala por defecto de Tailwind). Patrones:

- padding de página: 32 px (con el header de la clase) o 40 px (con la barra lateral); 16 px en móvil;
- padding de paneles: 22 a 26 px;
- separación entre secciones: 20 a 32 px;
- separación dentro de las listas: 10 a 14 px.

## Sombras

Casi no se usan: las tarjetas se separan por borde, no por sombra.

| Token | Valor | Uso |
|-------|-------|-----|
| `--shadow-float` | `0 8px 20px rgba(22,24,29,.08)` | Barras flotantes de la pizarra, popovers, menús |
| `--shadow-sticky` | `0 6px 14px rgba(22,24,29,.12)` | Notas adhesivas |

## Foco

`outline: 2px solid var(--color-brand); outline-offset: 2px` en todo elemento interactivo con `:focus-visible`.

## Contraste

Todas las combinaciones de esta tabla cumplen AA para texto normal. Las combinaciones nuevas se verifican antes de usarlas.
