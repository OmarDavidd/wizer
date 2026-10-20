# Tipografía

## Familias

| Rol | Familia | Pesos |
|-----|---------|-------|
| Display (títulos, números destacados, marca) | **Bricolage Grotesque** | 500, 700 |
| Texto e interfaz | **Inter** | 400, 500, 600 |

- Se cargan autoalojadas con `@fontsource-variable/*`, no desde Google Fonts en runtime.
- Fallback: `system-ui, sans-serif`.
- La pizarra usa la fuente manuscrita propia de Excalidraw. No se agregan otras.

## Escala

| Token | Familia / peso | Tamaño / interlineado | Uso |
|-------|----------------|-----------------------|-----|
| `display` | Bricolage 700 | 56px / 1.02, tracking -0.02em | Titular del panel de acceso |
| `h1` | Bricolage 700 | 40px / 1.1, tracking -0.02em | Título de página ("Buen día, Ana") |
| `h1-class` | Bricolage 700 | 38px (inicio) · 30px (subpáginas) | Nombre de la clase en el header |
| `h2` | Bricolage 700 | 26–34px | Título de una tarea o sección principal |
| `card-title` | Bricolage 700 | 22px / 1.1 | Nombre en las tarjetas de clase |
| `stat` | Bricolage 700 | 28px | Números de los tiles de estadísticas |
| `section` | Inter 600 | 16px | Títulos de sección ("Esta semana") |
| `body` | Inter 400 | 15px / 1.55–1.65 | Consignas y texto largo (máx. 68ch) |
| `ui` | Inter 500–600 | 14px | Botones, ítems de lista, navegación |
| `meta` | Inter 400 | 13px | Metadatos ("Arquitectura · 18/24 entregas") |
| `caption` | Inter 500–600 | 12px | Horas, chips pequeños, badges |
| `overline` | Inter 600 | 12px, mayúsculas, tracking 0.06em | Etiquetas de grupo en la barra lateral |

## Reglas

- Bricolage solo en títulos y números destacados. Nunca en párrafos ni en botones.
- No usar tamaños de texto de menos de 12 px.
- Para el texto largo, máximo 68ch de ancho.
- El código de acceso de la clase va en Inter 600 con tracking de 0.12–0.2em y en mayúsculas.

## Color del texto

| Contexto | Token |
|----------|-------|
| Principal | `--color-ink` |
| Secundario fuerte | `--color-text-2` |
| Metadatos | `--color-muted` |
| Etiquetas pequeñas y horas | `--color-subtle` |
| Sobre el color de marca | blanco y `--color-on-brand-muted` |
| Enlaces | `--color-brand`; en hover, `--color-brand-hover` |
