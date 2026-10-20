# Wizer — Documento de requisitos

> Plataforma web para clases semipresenciales: varias sedes (campus) conectadas a una misma clase, con comunicación, trabajo en equipo y entregas en un solo lugar.
>
> Este documento describe **qué** debe hacer el sistema. No define tecnología, colores ni diseño visual.

---

## 1. Problema

En la educación semipresencial, docentes y alumnos usan herramientas desconectadas entre sí: una plataforma oficial para notas, WhatsApp o Telegram para comunicarse y Drive u OneDrive para compartir archivos. Esto provoca:

- **Gestión docente ineficiente**: demasiado tiempo en tareas administrativas (listas, recolección de entregas desde distintas fuentes).
- **Colaboración estudiantil difícil**: los equipos coordinan por varios canales, pierden versiones y se desorganizan.
- **Comunicación fragmentada**: los mensajes importantes se pierden y no hay historial asociado a cada tarea.
- **Brecha digital operativa**: tener que dominar varias plataformas es una barrera de entrada.

**Objetivo:** un espacio unificado donde la clase, sus equipos, sus conversaciones, sus tareas y sus archivos convivan.

---

## 2. Actores y roles

| Rol | Descripción | Puede |
|-----|-------------|-------|
| **Docente** | Cuenta creada por un administrador | Crear clases, generar código de acceso, armar equipos, publicar tareas, calificar entregas, moderar el chat general, ver las pizarras de los equipos, pedir acceso al chat de un equipo |
| **Alumno** | Se registra solo (correo o Google) | Unirse por código, chatear, trabajar en la pizarra de su equipo, ver tareas, entregar archivos |
| **Administrador** | Gestiona la plataforma | Crear cuentas de docentes y administradores, desactivar usuarios, gestionar campus |

> Hay dos niveles de rol: el **de plataforma** (qué puede crear la cuenta) y el **de clase** (docente o alumno dentro de cada clase). Un docente puede unirse como alumno a la clase de otro docente.

---

## 3. Alcance

### Dentro del alcance

- Gestión de usuarios, autenticación y roles.
- Gestión de clases con unión por código único.
- Equipos de trabajo dentro de cada clase.
- Chat en tiempo real: general de la clase y por equipo.
- Pizarra colaborativa en tiempo real por equipo.
- Tareas con fecha límite, material adjunto y entregas de alumnos.
- Calificación por puntaje o aprobado/desaprobado, con comentario.
- Repositorio de archivos por clase.
- Notificaciones dentro de la plataforma.
- Interfaz web responsive (móvil y escritorio).

### Fuera del alcance

- Videoconferencia integrada (se usan herramientas externas como Zoom o Meet).
- Libreta de calificaciones avanzada, promedios ponderados o boletines.
- Cuestionarios o exámenes automatizados.
- Integración con sistemas académicos institucionales (SIGA u otros).
- Pagos o e-commerce.
- Aplicación móvil nativa.

---

## 4. Requisitos funcionales

Prioridad: **M** = imprescindible para el MVP · **S** = debería estar en el MVP · **C** = deseable, posterior.

### 4.1 Cuentas y autenticación (AUTH)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| AUTH-01 | Registro público de **alumnos** con nombre completo, correo, contraseña y campus. El registro nunca permite elegir otro rol. | M |
| AUTH-02 | Inicio de sesión con correo y contraseña. | M |
| AUTH-03 | Recuperación de contraseña por correo. | M |
| AUTH-04 | Cierre de sesión. | M |
| AUTH-05 | Validar formato de correo y que no esté registrado; mensaje claro si ya existe. | M |
| AUTH-06 | Opción "recordarme" para mantener la sesión. | S |
| AUTH-07 | Verificación de correo obligatoria antes del primer inicio de sesión. | M |
| AUTH-08 | Inicio de sesión con Google (crea una cuenta de alumno si no existe). | M |
| AUTH-09 | El administrador invita a docentes y administradores por correo; la persona invitada establece su contraseña con un enlace de un solo uso. | M |
| AUTH-10 | Bloqueo temporal después de 5 intentos de login fallidos en 5 minutos. | M |
| AUTH-11 | Inicio de sesión con Microsoft. | C |

### 4.2 Perfil (PERF)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| PERF-01 | Ver perfil propio: foto, nombre, correo y datos académicos según rol (alumno: carrera, semestre, matrícula; docente: título profesional). | M |
| PERF-02 | Editar nombre y foto. El nombre no puede quedar vacío; se confirma al guardar. | M |
| PERF-03 | Correo, matrícula y título profesional son de solo lectura para el usuario. | M |
| PERF-04 | Indicar el campus al que pertenece el usuario. | S |

### 4.3 Clases (CLS)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| CLS-01 | Una cuenta docente crea una clase con nombre, descripción, horario y campus participantes. | M |
| CLS-02 | Al crear la clase se genera un **código alfanumérico único de 6 caracteres**. | M |
| CLS-03 | El docente puede copiar y regenerar el código (el anterior deja de funcionar). | S |
| CLS-04 | El alumno se une a una clase ingresando el código. | M |
| CLS-05 | Listado "Mis clases" con las clases del usuario (activas y archivadas). | M |
| CLS-06 | Vista de clase con secciones: Inicio, Equipos, Tareas, Archivos, Pizarra. | M |
| CLS-07 | Lista de miembros con su rol y campus. | M |
| CLS-08 | El docente puede editar, archivar (manualmente), desarchivar o eliminar la clase. Una clase archivada queda en solo lectura. | M |
| CLS-09 | El docente puede quitar alumnos; el alumno puede abandonar la clase. | M |
| CLS-10 | Una clase puede tener más de un docente (co-docente). Fuera del MVP. | C |

### 4.4 Equipos (EQP)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| EQP-01 | El docente crea equipos dentro de una clase con nombre y tamaño máximo. | M |
| EQP-02 | El docente asigna alumnos a equipos manualmente. | M |
| EQP-03 | Armado automático al azar respetando el tamaño máximo. | S |
| EQP-04 | Vista de alumnos sin equipo. | M |
| EQP-05 | Mover un alumno entre equipos o quitarlo de uno. | M |
| EQP-06 | Cada equipo tiene automáticamente su chat y su pizarra. | M |
| EQP-07 | El alumno ve su equipo y sus compañeros en cada clase. | M |
| EQP-08 | Feed de actividad del equipo (mensajes recientes, cambios en la pizarra, entregas). | S |

### 4.5 Chat (CHAT)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| CHAT-01 | Chat general por clase, en tiempo real. | M |
| CHAT-02 | Chat por equipo, visible solo para sus integrantes. El docente accede solo si un miembro aprueba su pedido de acceso, y cualquier miembro puede revocarlo. | M |
| CHAT-03 | Historial persistente y paginado. | M |
| CHAT-04 | Cada mensaje muestra autor, campus y hora. | M |
| CHAT-05 | Indicador "escribiendo…". | S |
| CHAT-06 | Indicador de usuarios conectados. | S |
| CHAT-07 | Adjuntar archivos o imágenes a un mensaje. | S |
| CHAT-08 | El docente puede eliminar mensajes del chat general de su clase. | S |
| CHAT-09 | Contador de mensajes no leídos por clase y por equipo. | S |
| CHAT-10 | Hilo de consultas asociado a cada tarea. | S |
| CHAT-11 | El autor puede editar y borrar sus mensajes ("editado" / "Mensaje eliminado"). | M |
| CHAT-12 | Reacciones con un conjunto fijo de emojis. | S |
| CHAT-13 | Menciones con @ que notifican al mencionado. | S |
| CHAT-14 | Si se cae la conexión, los mensajes pendientes se reenvían solos al reconectar, sin duplicarse. | M |

### 4.6 Pizarra colaborativa (PIZ)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| PIZ-01 | Una pizarra por equipo, editable en tiempo real por sus integrantes. | M |
| PIZ-02 | Herramientas: lápiz libre, rectángulo, elipse, flecha, texto y borrador. | M |
| PIZ-03 | Selección de color y grosor de trazo. | M |
| PIZ-04 | Deshacer y rehacer. | M |
| PIZ-05 | El contenido se guarda y persiste entre sesiones. | M |
| PIZ-06 | Exportar la pizarra como imagen (PNG). | M |
| PIZ-07 | Ver los cursores de los compañeros con su nombre. | S |
| PIZ-08 | Notas adhesivas. | S |
| PIZ-09 | Zoom y desplazamiento del lienzo. | S |
| PIZ-10 | El docente puede entrar a la pizarra de cualquier equipo. | S |
| PIZ-11 | Pizarra general de la clase (además de las de equipo). | C |

### 4.7 Tareas y entregas (TAR)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| TAR-01 | El docente crea una tarea con título, consigna, fecha y hora límite, modalidad (individual o por equipo) y tipo de calificación (puntaje o aprobado/desaprobado). | M |
| TAR-02 | El docente adjunta material de apoyo a la tarea. | M |
| TAR-03 | El docente edita o elimina una tarea. | M |
| TAR-04 | Listado de tareas de la clase; **cada tarea tiene su propia sección** con el detalle de sus entregas. | M |
| TAR-05 | El alumno ve sus tareas pendientes de todas sus clases, ordenadas por urgencia. | M |
| TAR-06 | El alumno sube uno o más archivos y confirma la entrega. | M |
| TAR-07 | El alumno puede anular y corregir su entrega mientras no venza el plazo. | M |
| TAR-08 | En una tarea por equipo, una sola entrega cuenta para todo el equipo. | M |
| TAR-09 | El docente define **en cada tarea** si acepta entregas tardías (y opcionalmente hasta cuándo). Las tardías quedan marcadas como **tarde**. | M |
| TAR-10 | Resumen por tarea: entregadas / total, por calificar, sin entregar y tarde. | M |
| TAR-11 | Filtros por estado y búsqueda de alumno o equipo (pensado para clases numerosas). | M |
| TAR-12 | El docente califica cada entrega con una nota (o aprobado/desaprobado) y un comentario. En las tareas por equipo puede ajustar la nota de cada alumno. | M |
| TAR-13 | El alumno ve su nota y la devolución. | M |
| TAR-14 | Descargar todas las entregas de una tarea en un solo archivo comprimido. | S |
| TAR-15 | Comentario privado entre el alumno y el docente sobre una entrega. | S |

### 4.8 Archivos (ARC)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| ARC-01 | Repositorio de archivos por clase. | M |
| ARC-02 | Subir, listar y descargar archivos. | M |
| ARC-03 | Formatos admitidos: PDF, DOC/DOCX, XLS/XLSX, PPT/PPTX, TXT, imágenes y ZIP. Los videos se comparten como enlace. | M |
| ARC-04 | Límite de 25 MB por archivo y 2 GB por clase. | M |
| ARC-05 | Ver quién subió cada archivo y cuándo. | M |
| ARC-06 | Solo el autor o el docente puede eliminar un archivo. | M |
| ARC-07 | Organización por carpetas. | C |
| ARC-08 | Vista previa de PDF e imágenes. | M |
| ARC-09 | Escaneo antivirus de todo archivo subido; no se puede descargar hasta pasar el escaneo. | M |

### 4.9 Notificaciones (NOT)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| NOT-01 | Aviso al alumno cuando se publica una nueva tarea. | S |
| NOT-02 | Recordatorio de tarea próxima a vencer (24 h antes). | S |
| NOT-03 | Aviso al alumno cuando su entrega fue calificada. | S |
| NOT-04 | Aviso al alumno cuando lo agregan a un equipo. | S |
| NOT-05 | Centro de notificaciones dentro de la plataforma. | S |
| NOT-06 | Notificaciones por correo electrónico (configurables). | C |

### 4.10 Administración (ADM)

| ID | Requisito | Prioridad |
|----|-----------|-----------|
| ADM-01 | Listar, buscar, activar y desactivar usuarios. | M |
| ADM-02 | Invitar docentes y administradores (ver AUTH-09). | M |
| ADM-03 | Gestionar los campus de la institución. | S |
| ADM-04 | Ver y archivar cualquier clase. | C |

---

## 5. Flujos principales

### 5.1 Docente

1. Recibe la invitación del administrador, establece su contraseña e inicia sesión.
2. **Mis clases**: ve las entregas de la semana de todas sus clases y crea una clase nueva.
3. Comparte el **código** con los alumnos.
4. **Equipos**: reparte a los alumnos (a mano o al azar).
5. **Tareas**: publica una tarea con consigna, material y fecha límite.
6. Sigue el avance en la sección de la tarea (entregadas, pendientes, tarde) y **califica**.
7. Participa en el chat de la clase y revisa las pizarras de los equipos.

### 5.2 Alumno

1. Se registra con correo (y lo verifica) o entra con Google.
2. **Inicio**: se une a una clase con el código.
3. Ve sus **pendientes ordenados por urgencia** y la **actividad de su equipo**.
4. Entra a la clase: chat general, su equipo, tareas y archivos.
5. Trabaja con su equipo en la **pizarra** y el **chat del equipo**.
6. Abre una tarea, sube sus archivos y **entrega**. Puede corregir mientras el plazo esté abierto.
7. Recibe la **nota** y la devolución.

---

## 6. Reglas de negocio

Las reglas detalladas, con su ID (`BR-…`), están en `.ia/business-rules.md`. Las principales:

| ID | Regla |
|----|-------|
| RN-01 | El código de clase tiene 6 caracteres alfanuméricos en mayúsculas, sin caracteres ambiguos (0/O, 1/I/L), y es único entre las clases activas. |
| RN-02 | Solo las cuentas docentes crean clases, y cada clase tiene un único docente. |
| RN-03 | Un alumno pertenece a **un solo equipo por clase**. |
| RN-04 | Un equipo no supera su tamaño máximo. |
| RN-05 | Un alumno solo accede a las clases a las que se unió, y solo al chat y la pizarra de su equipo. |
| RN-06 | El docente ve todo en su clase, salvo el chat de los equipos, que requiere un acceso aprobado por un miembro. |
| RN-07 | Una entrega se puede editar y anular mientras el plazo esté abierto. El plazo incluye el período de tardías si la tarea lo permite. |
| RN-08 | Una entrega enviada después de la fecha límite (con tardías permitidas) queda marcada como "tarde". |
| RN-09 | La nota va de 0 al puntaje máximo, o es aprobado/desaprobado, según la tarea. En las tareas por equipo se puede ajustar por alumno. |
| RN-10 | Al salir de una clase, el alumno sale de su equipo; sus entregas y mensajes se conservan. |
| RN-11 | Las clases se archivan a mano; una clase archivada queda en solo lectura. Eliminarla requiere confirmación. |
| RN-12 | La identidad del usuario se obtiene **siempre de la sesión autenticada**, nunca de datos enviados por el cliente. |

---

## 7. Requisitos no funcionales

| Aspecto | Requisito |
|---------|-----------|
| **Rendimiento** | Carga de páginas en menos de 3 s; mensajes de chat y trazos de la pizarra con latencia menor a 500 ms. |
| **Escalabilidad** | Hasta 500 usuarios concurrentes en el MVP. |
| **Disponibilidad** | 99,5 % mensual. |
| **Responsive** | Funcional en móvil (desde 360 px de ancho) y escritorio. |
| **Seguridad** | Contraseñas con hash; sesiones con token y expiración; todos los endpoints protegidos salvo registro, login y recuperación; autorización por rol y por pertenencia a la clase o equipo. |
| **Archivos** | Validación de tipo y tamaño en el servidor; descargas solo para miembros de la clase. |
| **Privacidad** | Los datos personales solo son visibles para los miembros de la clase. |
| **Accesibilidad** | Navegación por teclado, etiquetas en los formularios y contraste suficiente (referencia: WCAG 2.1 AA). |
| **Idioma** | Interfaz en español neutro (tuteo). |
| **Calidad** | Pruebas automatizadas de las reglas de negocio y de los flujos críticos (registro, unirse a una clase, entregar, calificar). |
| **Integridad** | Operaciones de varios pasos (por ejemplo, el registro) atómicas: no deben quedar datos a medias. |

---

## 8. Modelo conceptual

| Entidad | Atributos principales | Relaciones |
|---------|-----------------------|------------|
| **Usuario** | nombre, correo, foto, rol, campus | pertenece a muchas Clases |
| **PerfilAcadémico** | carrera, semestre, matrícula / título profesional | 1 a 1 con Usuario |
| **Campus** | nombre | tiene muchos Usuarios; participa en muchas Clases |
| **Clase** | nombre, descripción, horario, código, estado | 1 Docente; muchos Alumnos, Equipos, Tareas y Archivos |
| **Equipo** | nombre, tamaño máximo | pertenece a una Clase; tiene muchos Alumnos, 1 Chat y 1 Pizarra |
| **Mensaje** | contenido, adjunto, fecha | autor Usuario; pertenece a un chat de Clase o de Equipo |
| **Pizarra** | contenido serializado, última modificación | pertenece a un Equipo (o a una Clase) |
| **Tarea** | título, consigna, fecha límite, puntaje, modalidad | pertenece a una Clase; tiene Material y Entregas |
| **Entrega** | fecha, estado (borrador / entregada / tarde), nota, comentario | de un Alumno o un Equipo para una Tarea; tiene Archivos |
| **Archivo** | nombre, tipo, tamaño, autor, fecha | asociado a una Clase, una Tarea, una Entrega o un Mensaje |
| **Notificación** | tipo, mensaje, leída, fecha | destinatario Usuario |

---

## 9. Decisiones tomadas (2026-10-20)

| # | Pregunta | Decisión |
|---|----------|----------|
| 1 | ¿Un usuario puede ser docente en una clase y alumno en otra? | Sí. El rol de clase vive en la membresía |
| 2 | ¿Quién da de alta a los docentes? | El administrador, por invitación. El registro público solo crea alumnos |
| 3 | ¿Varios docentes por clase? | No en el MVP |
| 4 | ¿Se aceptan entregas tardías? | El docente lo define en cada tarea |
| 5 | ¿Tamaño de archivo y cuota? | 25 MB por archivo y 2 GB por clase; los videos van por enlace |
| 6 | ¿Retención del chat? | Se conserva mientras exista la clase; archivada, en solo lectura |
| 7 | ¿Una o varias instituciones? | Una en el MVP, con el modelo preparado para varias |
| 8 | ¿El docente lee el chat de los equipos? | Solo si un miembro aprueba su pedido de acceso |
| 9 | ¿Tipo de nota? | Configurable por tarea: puntaje o aprobado/desaprobado |
| 10 | ¿Archivado de clases? | Manual |

---

## 10. Entregas del MVP

Las tareas, fechas y responsables se gestionan en Trello.
