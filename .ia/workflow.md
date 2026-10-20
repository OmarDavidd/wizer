# Wizer — Flujo de trabajo para agentes de IA

## Objetivo

Cómo debe trabajar cualquier agente (Claude Code, Codex, OpenCode u otro) antes, durante y después de modificar el proyecto, para evitar cambios de arquitectura accidentales, duplicación, dependencias innecesarias y decisiones que contradigan al equipo.

## 1. Antes de modificar código

Leer `.ia/README.md`, `project.md`, `architecture.md`, este archivo, y los documentos de la tarea (tabla en `AGENTS.md` §2).

## 2. Inspeccionar antes de crear

Antes de crear algo, buscar lo que ya existe:

- el módulo o la feature responsable;
- interfaces, services y repositories existentes;
- DTOs, tipos generados por Orval y schemas;
- componentes en `components/ui`, `components/common` y la feature;
- utilidades, helpers y convenciones en uso.

No crear una segunda implementación de algo que ya existe. Cuando varias partes modifican el mismo estado compartido (por ejemplo, agregar un miembro a una clase), reutilizar **la misma** función del Service, nunca reimplementarla.

## 3. Identificar la frontera

**Backend**: ¿a qué módulo pertenece? ¿Es responsabilidad del Controller, el Service, el Repository o Infrastructure? ¿Necesita otro módulo? ¿Ya existe la interfaz pública para eso? ¿Debe emitir un evento en tiempo real?

**Frontend**: ¿a qué feature pertenece? ¿Es container o presentational? ¿El estado es local, global del cliente o remoto? ¿Lo cubre un hook generado por Orval?

## 4. ODD (features grandes)

Se usa Organic Driven Development, igual que en mezcal:

- **Tarea chica** (entendida, de riesgo acotado y retomable solo con el diff): se hace directo, sin documento.
- **`odd/` es local**: está en `.gitignore` y no se sube al repositorio. Cada dev mantiene sus propios documentos de feature.
- **Feature grande**: `odd/tasks/<feature>.md` antes de escribir código, con:
  - un encabezado de 2 o 3 líneas (objetivo, rama, runners);
  - `## Specs`: `S1…Sn`, citando literalmente lo que pidió el usuario y los IDs de reglas (`BR-…`) y requisitos (`CHAT-…`);
  - `## Tasks`: una tabla con el ID, los `S#`, la ruta, el estado y el commit;
  - `## Log`: `L1` = el pedido original textual, y después las correcciones, la evidencia y las decisiones, con fecha.
- Las tareas se marcan como hechas solo con la evidencia observada (pruebas corridas, commit).

## 5. Prueba primero

En las reglas de negocio de los Services y las clases con lógica:

1. Escribir la prueba que falla (RED) y verla fallar.
2. Implementar lo mínimo (GREEN).
3. Refactorizar con las pruebas en verde.

Por cada comportamiento existente que toque el cambio (opciones, parsers o helpers compartidos), agregar una prueba que demuestre que sigue funcionando igual.

## 6. Cambio mínimo

- Modificar solo lo necesario para la tarea.
- Nada de refactors no pedidos en áreas no relacionadas.
- No actualizar dependencias ni versiones por iniciativa propia.

## 7. Respetar los límites

Backend:

```text
Controller → IService → Service → IRepository → Repository → EF Core
Service A → I<ModuleB>ModuleService → Service B         (nunca → Repository B)
Service → I<Module>Notifier → Hub                        (nunca lógica en el Hub)
```

Frontend:

```text
Route → Container → Hook (Orval o propio) → TanStack Query → Axios → API
Hub event → realtime hook → queryClient.setQueryData / invalidateQueries
```

## 8. Seguridad

Antes de terminar, revisar que el cambio no introduzca:

- un `userId` del cliente usado como identidad;
- un endpoint o método de hub sin autorización o sin verificar la pertenencia;
- tokens en el almacenamiento del navegador;
- secretos en el código o en variables `VITE_*`;
- logs con secretos o contenido privado;
- `dangerouslySetInnerHTML` con contenido de usuarios;
- archivos descargables sin escaneo.

## 9. Base de datos

1. Identificar la entidad y su módulo.
2. Modificar la entidad y su `Configuration`.
3. Revisar las relaciones, los índices y las restricciones.
4. **Pedir confirmación antes de crear la migración.**
5. No editar migraciones aplicadas.

## 10. API y contrato

- Mantener `/api/v1`, los DTOs y ProblemDetails.
- Un cambio de contrato se coordina en el mismo PR: backend → OpenAPI → `npm run api:generate` → frontend.
- **Pedir confirmación** antes de cambiar o quitar algo del contrato público (agregar campos opcionales no requiere confirmación).

## 11. Validación antes de terminar

```text
[ ] La tarea pedida está completa.
[ ] La arquitectura se respetó.
[ ] No se introdujeron tecnologías no autorizadas.
[ ] No hay duplicación evidente.
[ ] El código está en inglés y la UI en español (tuteo).
[ ] No se expusieron secretos.
[ ] El contrato de la API sigue las reglas y el cliente está regenerado.
[ ] Los logs siguen el structured logging.
[ ] Las pruebas se crearon o actualizaron (RED observado en las reglas nuevas).
[ ] Las pruebas se ejecutaron: resultado real reportado.
[ ] Los textos de ayuda y los docs se actualizaron si cambió un comando, una opción o un mensaje.
```

## 12. Detenerse y preguntar

El agente **pide confirmación** antes de:

- `git push`, abrir, aprobar o mergear PRs;
- crear, modificar o eliminar **migraciones**;
- agregar, quitar o actualizar dependencias;
- cambiar el contrato público de la API o de los hubs;
- cambiar la arquitectura, la autenticación, la autorización, la seguridad o el modelo de datos;
- cambiar la estrategia de despliegue, la CI, Docker de producción o el almacenamiento;
- cambios destructivos sobre datos o sobre el repositorio;
- implementar una decisión no documentada con consecuencias relevantes.

También pregunta cuando hay dos decisiones razonables con consecuencias distintas y ninguna está documentada.

## 13. Lo que no requiere preguntar

Decisiones locales y reversibles que respetan las reglas:

- nombres internos coherentes;
- extraer una función privada pequeña;
- la organización interna de un componente;
- implementar una validación ya definida;
- elegir un componente existente de shadcn/ui;
- commits locales en la rama de trabajo.

No convertir cada detalle en una pregunta.

## 14. Contradicciones

Si `.ia/`, el código, los requisitos y la tarea se contradicen:

1. identificar la contradicción;
2. no ocultarla;
3. priorizar la instrucción explícita actual del usuario cuando la resuelve;
4. si afecta la arquitectura, la seguridad, los datos o la API y no está resuelta, preguntar.

No "corregir" requisitos en silencio según las preferencias del agente.

## 15. Cierre de una tarea

Resumen breve:

- qué se modificó;
- los archivos principales;
- las pruebas ejecutadas y su resultado **real**;
- las decisiones pendientes o los riesgos;
- `Risk: none` o `Risk: <qué puede romperse y por qué>`.

No afirmar que algo se probó si no se ejecutó.
