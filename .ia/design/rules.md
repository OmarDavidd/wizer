# Reglas de diseño para agentes de IA

## Antes de modificar la UI

1. Leer `README.md`, `tokens.md` y `components.md`.
2. Ubicar la pantalla en `pages.md` y en el lienzo.
3. Buscar si ya existe un componente equivalente en `components/ui`, `components/common` o en la feature.

## Obligatorio

- Usar los tokens (`tokens.md`). **Nunca** hexadecimales, pixeles de color o fuentes sueltas en los componentes.
- Reutilizar los componentes existentes y las variantes documentadas.
- Bricolage Grotesque solo en títulos y números. Inter en todo lo demás.
- Íconos de lucide-react.
- Contemplar los estados de cargando, vacío, error y sin permiso.
- Accesibilidad: elementos semánticos, `label`, `aria-label` en los botones de solo ícono, foco visible y contraste AA.
- Mobile-first y probado desde 360px.
- Textos en español neutro con tuteo.

## `data-testid`

Obligatorio en:

- inputs y botones de envío de formularios;
- botones de acción principal de cada pantalla;
- contenedores de listas e ítems repetidos (con el ID: `data-testid="submission-row-{id}"`);
- los estados vacío, de error y de carga;
- los elementos que verifican las pruebas E2E (mensajes del chat, celdas de la tabla de entregas, la pizarra).

Formato: kebab-case con el prefijo de la feature (`chat-`, `class-`, `team-`, `assignment-`, `submission-`, `file-`, `whiteboard-`, `auth-`, `nav-`).

## Prohibido

- Inventar colores, sombras, radios o tipografías.
- Degradados de fondo, bordes de color a la izquierda en tarjetas, glassmorphism y emojis como íconos.
- Un segundo botón primario (`brand` o `dark`) en el mismo bloque.
- `div` u `span` con `onClick` como botón.
- Texto de menos de 12px.
- Interfaces que solo funcionan con hover.
- Modificar `components/ui` para un caso de negocio (se crea un componente en la feature).

## Regla de componentes

- Si un patrón aparece en **dos o más features**, pasa a `components/common`.
- `components/ui` solo contiene primitivas de shadcn ajustadas a los tokens.

## Diseño nuevo

Si una pantalla no existe en el lienzo:

1. componerla con los layouts y componentes existentes;
2. **pedir confirmación** si hace falta un patrón visual nuevo;
3. documentar el patrón nuevo en `components.md` en el mismo PR.

## Checklist

```text
[ ] Solo tokens, sin valores sueltos
[ ] Componentes reutilizados
[ ] Estados de cargando, vacío, error y sin permiso
[ ] 360 / 768 / 1280 px revisados
[ ] Navegación con teclado y foco visible
[ ] Contraste AA
[ ] data-testid en los elementos de prueba
[ ] Copy en español neutro con tuteo
```
