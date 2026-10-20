# Wizer — Reglas de negocio

Las reglas funcionales del MVP. Los agentes no las cambian sin autorización. Cada regla tiene un ID para referenciarla en pruebas y en documentos ODD.

## 1. Actores

Ver `permissions.md`. Hay tres roles de plataforma (admin, docente y alumno) y dos roles de clase (docente y alumno).

## 2. Cuentas

- **BR-ACC-01** El registro público y el login con Google crean solo cuentas de alumno.
- **BR-ACC-02** Las cuentas de docente y de administrador las crea un administrador mediante invitación.
- **BR-ACC-03** Sin el correo verificado no se puede iniciar sesión.
- **BR-ACC-04** Un usuario desactivado no puede iniciar sesión, y sus sesiones activas se revocan. Sus mensajes, entregas y archivos se conservan.
- **BR-ACC-05** Cada usuario pertenece a un campus, que se muestra junto a su nombre en el chat y en las listas.

## 3. Clases

- **BR-CLS-01** Solo una cuenta docente crea clases, y queda como el único docente de la clase.
- **BR-CLS-02** Al crearse, la clase recibe un código de **6 caracteres** del alfabeto `23456789ABCDEFGHJKMNPQRSTUVWXYZ`, sin caracteres ambiguos (0/O, 1/I/L). El código es único entre las clases activas.
- **BR-CLS-03** Al regenerar el código, el anterior deja de funcionar de inmediato.
- **BR-CLS-04** El código se valida sin distinguir mayúsculas de minúsculas, después de quitar espacios y guiones.
- **BR-CLS-05** Unirse a una clase de la que ya se es miembro responde `409 ALREADY_CLASS_MEMBER`.
- **BR-CLS-06** Un docente no puede unirse como alumno a su propia clase.
- **BR-CLS-07** El archivado es manual y deja la clase en solo lectura (`permissions.md` §Clases archivadas). Una clase archivada no acepta uniones por código.
- **BR-CLS-08** Quitar a un alumno, o que el alumno salga, lo saca de su equipo. Sus entregas y mensajes se conservan.
- **BR-CLS-09** Eliminar una clase requiere escribir su nombre para confirmar. Es un soft delete.

## 4. Equipos

- **BR-TEAM-01** El docente crea equipos con nombre único dentro de la clase y un tamaño máximo (de 2 a 10).
- **BR-TEAM-02** Un alumno pertenece a **un solo equipo por clase**.
- **BR-TEAM-03** Un equipo no supera su tamaño máximo (`409 TEAM_FULL`).
- **BR-TEAM-04** El armado automático reparte al azar a los alumnos sin equipo entre los equipos con lugar, de forma equilibrada, y respeta los que ya están asignados.
- **BR-TEAM-05** Al crearse, cada equipo tiene automáticamente un canal de chat y una pizarra.
- **BR-TEAM-06** Un equipo con entregas no se borra físicamente: se marca como borrado y sus entregas se conservan.

## 5. Chat

- **BR-CHAT-01** Cada clase tiene un canal general. Cada equipo tiene un canal propio.
- **BR-CHAT-02** Los mensajes tienen entre 1 y 4000 caracteres y hasta 5 adjuntos.
- **BR-CHAT-03** El autor puede **editar** su mensaje en cualquier momento. Se muestra "editado".
- **BR-CHAT-04** El autor puede **borrar** su mensaje. Queda como "Mensaje eliminado" (soft delete).
- **BR-CHAT-05** El docente puede borrar cualquier mensaje del canal general de su clase.
- **BR-CHAT-06** Reacciones: un conjunto fijo de 6 emojis (👍 ❤️ 😂 😮 😢 🎉). Cada usuario puede dar una reacción de cada tipo por mensaje.
- **BR-CHAT-07** Menciones con `@`: solo a miembros del canal. Cada mencionado recibe una notificación.
- **BR-CHAT-08** Un mensaje enviado dos veces con el mismo `clientMessageId` se guarda una sola vez.
- **BR-CHAT-09** El historial se conserva mientras exista la clase. En una clase archivada se puede leer pero no escribir.

### Acceso del docente al chat de un equipo

- **BR-CHAT-10** El docente **no** lee ni escribe en el chat de un equipo por defecto.
- **BR-CHAT-11** El docente puede **pedir acceso**. Los miembros del equipo reciben una notificación.
- **BR-CHAT-12** Cualquier miembro del equipo puede aprobar o rechazar el pedido. Con la primera respuesta se resuelve.
- **BR-CHAT-13** Con el acceso aprobado, el docente lee todo el historial y puede escribir. El chat muestra el aviso "La docente tiene acceso a este chat".
- **BR-CHAT-14** Cualquier miembro puede revocar el acceso en cualquier momento.
- **BR-CHAT-15** No puede haber más de un pedido pendiente por equipo (`409 CHAT_ACCESS_REQUEST_PENDING`).

## 6. Pizarra

- **BR-WB-01** Cada equipo tiene una pizarra, que editan sus miembros en tiempo real.
- **BR-WB-02** El docente de la clase puede ver y editar la pizarra de cualquier equipo **sin pedir acceso**: es trabajo académico, a diferencia del chat, que es conversación privada. *(Supuesto: confirmar con el equipo.)*
- **BR-WB-03** La pizarra se guarda sola. Se puede exportar a PNG.
- **BR-WB-04** No se insertan imágenes en el MVP.

## 7. Tareas

- **BR-ASG-01** Una tarea tiene: título, consigna, fecha y hora límite, modalidad (`individual` o `team`), tipo de calificación (`points` o `pass_fail`) y, si es `points`, un puntaje máximo de 1 a 100.
- **BR-ASG-02** El docente define en **cada tarea** si se aceptan entregas tardías (`allow_late_submissions`). Si se aceptan, puede definir hasta cuándo (`late_until`, opcional).
- **BR-ASG-03** Una tarea puede guardarse como borrador (no visible para los alumnos) o publicarse. Al publicarse se notifica a los alumnos.
- **BR-ASG-04** La modalidad y el tipo de calificación no se pueden cambiar si ya existe alguna entrega enviada.
- **BR-ASG-05** Cambiar la fecha límite notifica a los alumnos y recalcula qué entregas cuentan como tardías.
- **BR-ASG-06** Las tareas de modalidad `team` requieren que la clase tenga equipos.
- **BR-ASG-07** Un alumno sin equipo no puede entregar una tarea de equipo; ve el aviso "No tienes equipo asignado".

## 8. Entregas

- **BR-SUB-01** Una entrega contiene archivos (hasta 10) y/o enlaces (hasta 5, por ejemplo videos). Una entrega vacía no se puede enviar (`SUBMISSION_EMPTY`).
- **BR-SUB-02** Estados: `draft` → `submitted` → `graded`. `submitted` puede volver a `draft` (anular la entrega) mientras el plazo esté abierto.
- **BR-SUB-03** Plazo abierto: `now <= due_at`, o `now <= late_until` si se aceptan tardías (sin `late_until`, el plazo queda abierto indefinidamente).
- **BR-SUB-04** Enviar después de `due_at` con tardías permitidas marca la entrega como **tarde** (`is_late = true`).
- **BR-SUB-05** Fuera del plazo no se puede enviar, anular ni modificar (`SUBMISSION_DEADLINE_PASSED` / `SUBMISSION_LOCKED`).
- **BR-SUB-06** Una entrega calificada no se puede anular.
- **BR-SUB-07** En una tarea de equipo hay **una sola entrega por equipo**, y cualquier miembro puede editarla, enviarla o anularla. Queda registrado quién la envió.
- **BR-SUB-08** Si un alumno cambia de equipo, la entrega queda en el equipo original. El alumno ve la del equipo nuevo.

## 9. Calificación

- **BR-GRD-01** Solo el docente de la clase califica.
- **BR-GRD-02** `points`: una nota de 0 al puntaje máximo, con hasta 2 decimales (`GRADE_OUT_OF_RANGE`). `pass_fail`: `pass` o `fail`.
- **BR-GRD-03** Se puede calificar una entrega enviada o una tarea **sin entrega**, cuando el plazo ya cerró (por ejemplo, un 0 por no entregar).
- **BR-GRD-04** En una tarea de equipo, la nota del equipo aplica a todos sus miembros. El docente puede **ajustarla por alumno** (`submission_member_grades`). Si hay un ajuste, prevalece sobre la nota del equipo para ese alumno.
- **BR-GRD-05** La calificación incluye un comentario opcional (hasta 2000 caracteres).
- **BR-GRD-06** Calificar o cambiar la nota notifica al alumno o al equipo.
- **BR-GRD-07** El docente puede cambiar la nota en cualquier momento. Se guarda quién calificó y cuándo.

## 10. Archivos

Ver `files.md`: límites, tipos, contextos y escaneo.

- **BR-FILE-01** Un archivo se puede descargar solo después de pasar el escaneo antivirus.
- **BR-FILE-02** La cuota es de 2 GB por clase (`STORAGE_QUOTA_EXCEEDED`).
- **BR-FILE-03** Los videos no se suben: se comparten como enlace.

## 11. Notificaciones

| Evento | Destinatarios |
|--------|---------------|
| Tarea publicada | Alumnos de la clase |
| Fecha límite cambiada | Alumnos de la clase |
| Recordatorio 24 h antes del vencimiento | Alumnos sin entrega enviada |
| Entrega calificada o nota cambiada | El alumno, o los miembros del equipo |
| Agregado a un equipo | El alumno |
| Mención en el chat | El mencionado |
| Pedido de acceso al chat | Los miembros del equipo |
| Acceso aprobado o rechazado | El docente |
| Archivo rechazado por el antivirus | Quien lo subió |

- **BR-NOT-01** Las notificaciones se muestran dentro de la plataforma. No hay correos en el MVP, salvo los de autenticación.
- **BR-NOT-02** Las notificaciones leídas se borran a los 90 días.
