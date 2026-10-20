# Wizer — Sistema de diseño

## Fuente de verdad

El diseño oficial es el **lienzo de Claude Design**:
https://claude.ai/artifact/NQ6M6BcQuCqcjo6Qtc6kak

| Artboard | Pantalla |
|----------|----------|
| 01 · Acceso | Login y registro (pantalla partida en diagonal) |
| 02 · Docente · Mis clases | Inicio del docente |
| 03 · Clase · Inicio y chat | Inicio de la clase, chat general y del equipo |
| 04 · Docente · Equipos | Armado de equipos |
| 05 · Docente · Tareas y entregas | Sección por tarea, con la tabla de entregas |
| 06 · Pizarra de equipo | Pizarra (Excalidraw) |
| A1 · Alumno · Inicio | Pendientes, equipo y clases |
| A2 · Alumno · Tarea y entrega | Consigna y entrega |

Logo: `assets/logo.svg`.

Si el lienzo y estos documentos se contradicen, **mandan estos documentos** (tokens y reglas). El lienzo es la referencia visual de la composición.

## Orden de lectura para agentes

1. `rules.md`
2. `tokens.md`
3. `typography.md`
4. `components.md`
5. `layouts.md`
6. `pages.md`
7. `responsive.md`

## Regla principal

**No rediseñar Wizer.** Toda interfaz nueva extiende los patrones existentes y reutiliza los tokens, los componentes y los layouts documentados. No introducir colores, tipografías, radios, sombras ni íconos nuevos si ya existe un equivalente.

## Personalidad visual

- **Cálida y académica, sin ser infantil**: fondo crema, un ciruela profundo como color de marca y amarillo para lo urgente.
- **Clara antes que decorada**: sin degradados de fondo, sin bordes de color a la izquierda en las tarjetas, sin emojis como íconos.
- **Humana**: avatares con iniciales en tonos pastel y el campus visible junto a cada persona.
- **El tiempo real se nota**: puntos de "en vivo", cursores con nombre y "escribiendo…".
