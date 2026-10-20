# Responsive

## Breakpoints

Los de Tailwind CSS v4 por defecto, igual que mezcal:

```text
sm  = 640px
md  = 768px
lg  = 1024px
xl  = 1280px
2xl = 1536px
```

## Filosofía

**Mobile-first.** Ancho mínimo soportado: **360px**, sin scroll horizontal de la página.

## Reglas por layout

| Elemento | < `lg` (1024px) | ≥ `lg` |
|----------|-----------------|--------|
| Barra lateral (AppShell) | Barra superior con el logo, campana y menú hamburguesa (un drawer con la navegación) | Barra lateral fija de 248px |
| Header de la clase | Nombre en 26px, metadatos en 2 líneas, avatares ocultos | Completo |
| Pestañas de la clase | Scroll horizontal | En línea |
| Inicio de la clase | Una columna, con el chat primero | Dos columnas |
| Tareas del docente | Selector de tarea arriba y detalle debajo | Lista + detalle |
| Tabla de entregas | Scroll horizontal en su caja | Completa |
| Pizarra | Herramientas en una barra inferior horizontal y colores en un popover | Herramientas verticales a la izquierda |

| Elemento | < 860px | ≥ 860px |
|----------|---------|---------|
| Acceso | Corte horizontal: marca arriba, formulario abajo | Diagonal |

## Contenedores

```text
px-4         (16px) en móvil
sm:px-6
lg:px-8 / lg:px-10
```

## Táctil

- Áreas táctiles de 44 × 44px como mínimo.
- Nada que dependa solo del hover: las acciones de los mensajes (editar, borrar, reaccionar) están en un menú "⋯" que también funciona con un toque largo.
- En la pizarra, Excalidraw maneja el pinch-zoom. No interceptar esos gestos.

## Reglas

- Sin alturas fijas con `%`. Usar `dvh` solo en layouts de pantalla completa (pizarra y acceso).
- Los grupos de botones hacen wrap.
- Los textos largos se cortan con `truncate` o `line-clamp`, nunca desbordan.
- Probar cada pantalla nueva a 360, 768 y 1280px antes de cerrarla.
