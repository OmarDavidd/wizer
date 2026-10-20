# Wizer — Contexto del proyecto

## Propósito

En la educación semipresencial, docentes y alumnos usan herramientas desconectadas: una plataforma oficial para las notas, WhatsApp para comunicarse y Drive para los archivos. Wizer las reemplaza con un solo espacio donde conviven la clase, sus equipos, sus conversaciones, sus tareas y sus archivos, con varias sedes (campus) conectadas a la misma clase.

Wizer nació como proyecto de NovaUniversitas. Hoy es un **proyecto de portfolio** del equipo, así que la calidad de código, las pruebas y la documentación importan tanto como las funcionalidades.

## Equipo

| Persona | Rol |
|---------|-----|
| Omar | Líder técnico, fullstack |
| Luis | Fullstack |
| Rolando | Fullstack |

Cada dev toma **módulos completos de punta a punta** (backend, frontend, pruebas y documentación). El reparto de tareas se gestiona en Trello.

## Hito

MVP terminado y desplegado entre **mediados y fines de diciembre de 2026**.

## Objetivos funcionales principales

1. Registro, login (correo y Google) y verificación de correo.
2. Clases con código de acceso de 6 caracteres.
3. Equipos dentro de cada clase.
4. Chat en tiempo real general y por equipo, con edición, borrado, reacciones y menciones.
5. Pizarra colaborativa por equipo (Excalidraw).
6. Tareas con fecha límite, entregas y calificación.
7. Repositorio de archivos por clase, con antivirus y vista previa.
8. Notificaciones dentro de la plataforma.
9. Administración de usuarios docentes y campus.

El detalle con IDs (`AUTH-01`, `CHAT-03`…) está en `docs/01-requisitos.md`. Las reglas exactas están en `business-rules.md`.

## Actores

| Actor | Descripción |
|-------|-------------|
| Administrador | Gestiona la plataforma: crea cuentas de docentes, gestiona campus y desactiva usuarios |
| Docente | Cuenta creada por un administrador. Crea clases, equipos y tareas, y califica |
| Alumno | Se registra solo (correo o Google). Se une a clases con código |

Un docente también puede unirse como alumno a la clase de otro docente: el rol dentro de la clase vive en la membresía (`permissions.md`).

## Decisiones de alcance

**Dentro del MVP**
- Una sola institución (el modelo ya incluye `institution_id` para pasar a multi-institución más adelante).
- Un solo docente por clase.
- Login con Google para alumnos.
- Calificación numérica o aprobado/desaprobado, configurable por tarea.
- Interfaz solo en español.

**Fuera del MVP**
- Videoconferencia (se usan Zoom o Meet por fuera).
- Videos subidos como archivo (se comparten por enlace).
- Moderación automática de contenido.
- Modo oscuro.
- PWA y notificaciones push.
- Notificaciones por correo (solo los correos de autenticación).
- Varios docentes por clase.
- Varias instituciones activas.
- App móvil nativa.

## Restricciones técnicas

- Un solo contenedor de la API en producción, sin Redis. SignalR funciona en memoria. Escalar a varias instancias requiere agregar un backplane (decisión futura).
- Home Lab con recursos limitados: todo corre en Docker Compose.
- Todavía no hay dominio: los correos de producción con Resend necesitan un dominio verificado (`docs/03-riesgos.md`).

## Principios del proyecto

1. **Identidad desde la sesión.** Nunca confiar en IDs de usuario enviados por el cliente (el proyecto anterior falló en esto).
2. **Autorización en el backend**, por rol y por pertenencia a la clase o al equipo.
3. **Operaciones atómicas.** Un flujo de varios pasos no deja datos a medias.
4. **Tiempo real resistente.** Reconexión automática, reenvío idempotente y resincronización.
5. **Pruebas primero** en reglas de negocio.
6. **Contrato generado.** OpenAPI es la fuente de verdad entre front y back.
7. **Simple antes que distribuido.** Monolito modular, una instancia y sin colas externas en el MVP.
