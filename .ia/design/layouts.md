# Layouts

## AuthLayout (acceso)

- Pantalla completa partida **en diagonal**: el panel de marca (`brand`) a la izquierda, recortado con `clip-path: polygon(0 0, 58% 0, 42% 100%, 0 100%)`, y el formulario a la derecha sobre `ground`.
- Panel de marca: isotipo y nombre, titular en `display`, bajada en `on-brand-muted` y el nombre de la institución abajo.
- Formulario: ancho máximo de 400px, centrado verticalmente.
- Menos de 860px: el corte pasa a ser **horizontal**: `clip-path: polygon(0 0, 100% 0, 100% 380px, 0 460px)`, con la marca arriba y el formulario abajo.

## AppShell (páginas globales: Inicio, Mis tareas, Notificaciones, Perfil, Admin)

```text
┌──────────┬───────────────────────────────────────┐
│ Sidebar  │  main (padding 32–40px)               │
│ 248px    │  ├ header: saludo/título + acciones   │
│          │  └ secciones                          │
└──────────┴───────────────────────────────────────┘
```

- Contenedor flex con wrap: barra lateral `flex: 1 1 248px` y contenido `flex: 999 1 560px; min-width: 0`.
- En móvil, la barra lateral se convierte en una barra superior con menú (`responsive.md`).

## ClassLayout (todo lo que está dentro de una clase)

```text
┌──────────────────────────────────────────────────┐
│ Header brand: ← Mis clases · código · nombre      │
│ docente · horario · campus · avatares             │
│ [Inicio] Equipos  Tareas  Archivos  Pizarra       │
├──────────────────────────────────────────────────┤
│ contenido (max-width 1360px, padding 28–32px)     │
└──────────────────────────────────────────────────┘
```

- Header de marca en todo el ancho, con el contenido en un máximo de 1360px.
- La pestaña activa se "funde" con el fondo `ground`.
- El inicio de la clase muestra el header completo (nombre en 38px, metadatos y avatares). Las subpáginas, uno compacto (nombre en 30px).
- Las pestañas hacen scroll horizontal en móvil.

## Inicio de la clase: dos columnas

Columna izquierda `flex: 1 1 440px` (próximas tareas y equipos) y chat `flex: 1 1 520px` con altura fija de 640px y scroll interno. En móvil se apilan, con el chat primero.

## Tareas del docente: lista + detalle

Lista de tareas `flex: 1 1 300px` y detalle `flex: 999 1 640px`. En móvil, la lista pasa a ser un selector arriba.

## Tarea del alumno: contenido + lateral

Artículo `flex: 999 1 560px` y lateral "Tu entrega" `flex: 1 1 340px`. En móvil, el lateral va después de la consigna.

## WhiteboardLayout

Pantalla completa sin AppShell ni ClassLayout: una barra superior blanca (volver, nombre de la pizarra, "En vivo", participantes, Chat del equipo, Exportar) y el lienzo debajo, que ocupa todo el resto.

## Contenedores

| Contexto | Ancho máximo | Padding horizontal |
|----------|--------------|--------------------|
| ClassLayout | 1360px | 32px (16px en móvil) |
| AppShell main | fluido | 40px (16px en móvil) |
| Formularios | 400–420px | — |
| Texto largo | 68ch | — |

## Grillas

- Tarjetas de clase: `repeat(auto-fill, minmax(300px, 1fr))`, gap de 18px.
- Entregas de la semana: `repeat(auto-fit, minmax(240px, 1fr))`, gap de 14px.
- Equipos: `repeat(auto-fill, minmax(240px, 1fr))`, gap de 16px.
- Tiles de estadísticas: `repeat(auto-fit, minmax(140px, 1fr))`, gap de 12px.
