# Wizer — Tiempo real (SignalR y pizarra)

## Objetivo

Definir cómo funcionan el chat, la presencia, el indicador de escritura, las notificaciones en vivo y la pizarra colaborativa, y cómo se mantienen consistentes si la red falla.

## Principios

1. **Las mutaciones van por REST** (crear, editar y borrar mensajes, reacciones, etc.). Así reutilizan la validación, la autorización, los errores, el rate limiting y las pruebas HTTP.
2. **Los hubs emiten eventos** del servidor al cliente, y reciben solo **señales efímeras** del cliente (escribiendo, cursores, trazos de pizarra).
3. **El servidor es la fuente de verdad.** El cliente aplica actualizaciones optimistas y las reconcilia con el evento del servidor.
4. **Todo evento es idempotente** para el cliente: aplicarlo dos veces no cambia el resultado.
5. **Una sola instancia, sin backplane** (ADR-04). Los grupos y la presencia viven en memoria. Escalar requiere Redis (decisión futura).

## Hubs

| Hub | Ruta | Responsabilidad |
|-----|------|-----------------|
| `ChatHub` | `/hubs/chat` | Eventos de mensajes, reacciones, escribiendo y presencia |
| `WhiteboardHub` | `/hubs/whiteboard` | Sincronización de escena, cursores y participantes |
| `NotificationsHub` | `/hubs/notifications` | Notificaciones nuevas y contador de no leídas |

- Viven en el módulo dueño: `Modules/Chat/Hubs/ChatHub.cs`, etc.
- Hubs fuertemente tipados: `Hub<IChatClient>`.
- Todos llevan `[Authorize]`. La autenticación usa la cookie `wz_access`, que llega en el handshake porque el origen es el mismo.
- Un hub **no contiene lógica de negocio**: valida la pertenencia llamando al Service y delega.
- Para emitir desde un Service se usa un notifier (`IChatNotifier`) implementado con `IHubContext<ChatHub, IChatClient>`. Los Services no conocen SignalR.

## Grupos

| Grupo | Miembros |
|-------|----------|
| `class:{classId}` | Miembros de la clase (chat general) |
| `team:{teamId}` | Miembros del equipo y el docente con acceso concedido |
| `board:{teamId}` | Participantes conectados a la pizarra |
| `user:{userId}` | Todas las conexiones de un usuario (notificaciones) |

- El cliente llama a `JoinClass(classId)` / `JoinTeam(teamId)`. El hub **verifica la pertenencia** antes de agregar la conexión al grupo y, si no corresponde, lanza un `HubException` con código `FORBIDDEN`.
- `user:{userId}` se asigna automáticamente al conectar.
- Si se quita a alguien de una clase o de un equipo, el servidor lo saca de los grupos activos.

## Chat

### Enviar un mensaje

```text
Cliente: POST /api/v1/chat/channels/{channelId}/messages
         { clientMessageId, body, mentions[], attachmentFileIds[] }
Servidor: valida → persiste → 201 { message }
          → emite MessageCreated(message) al grupo del canal
Cliente: reemplaza el mensaje optimista (match por clientMessageId)
```

- `clientMessageId` (UUID generado en el cliente) es la **clave de idempotencia**. Hay un índice único `(sender_id, client_message_id)`: un reintento devuelve el mensaje existente con `200`, no lo duplica.
- Edición: `PATCH /messages/{id}` → evento `MessageUpdated`.
- Borrado: `DELETE /messages/{id}` (soft delete) → evento `MessageDeleted`. Se muestra "Mensaje eliminado".
- Reacciones: `PUT` / `DELETE /messages/{id}/reactions/{emoji}` → evento `ReactionsChanged`.
- Menciones: el backend valida que los mencionados sean miembros del canal y crea una notificación para cada uno.

### Reenvío automático (outbox)

- Un mensaje que no se pudo enviar queda en un **outbox** en memoria (Zustand) con estado `pending` o `failed`.
- Se reintenta solo al reconectar y con backoff (1 s, 2 s, 5 s, 10 s y 30 s como máximo), siempre con el mismo `clientMessageId`.
- Después de 5 fallos que no sean de red (por ejemplo 403 o 422) queda en `failed`, con la opción de reintentar o descartar.
- El outbox no se persiste entre recargas en el MVP.

### Eventos de `IChatClient`

```text
MessageCreated(MessageDto)
MessageUpdated(MessageDto)
MessageDeleted(channelId, messageId)
ReactionsChanged(channelId, messageId, ReactionSummaryDto[])
TypingChanged(channelId, userId, displayName, isTyping)
PresenceChanged(classId, userId, isOnline)
TeacherAccessChanged(teamId, hasAccess)
```

### Escribiendo

- El cliente llama a `Typing(channelId)` como máximo cada 3 s mientras escribe.
- El servidor emite `TypingChanged(isTyping: true)` al resto del grupo.
- El cliente oculta el indicador 5 s después del último evento, o al recibir el mensaje de esa persona.

### Presencia

- `IPresenceTracker` (singleton, en memoria) cuenta las conexiones por usuario y por clase.
- Cuando un usuario pasa de 0 a 1 conexión se emite `PresenceChanged(true)`; de 1 a 0, `PresenceChanged(false)` con un retardo de 10 s para no parpadear al recargar.
- `GET /api/v1/classes/{id}/presence` devuelve los conectados al cargar la pantalla.

### Reconexión y resincronización

```text
withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
onreconnected → JoinClass / JoinTeam otra vez
             → invalidar las queries de mensajes de los canales abiertos
             → vaciar el outbox
```

Los mensajes se cargan con paginación por cursor (`api.md`). Tras reconectar, se pide la página más reciente y se fusiona con la caché por `id`.

## Pizarra (Excalidraw)

### Modelo

- Una pizarra por equipo: tabla `whiteboards` (`team_id` único), con la escena (`scene` jsonb, solo los elementos), `version bigint` y `updated_at`.
- Las imágenes dentro de la pizarra están **deshabilitadas en el MVP**: la herramienta de imagen de Excalidraw se oculta.

### Sincronización

```text
Al abrir:
  GET /api/v1/teams/{teamId}/whiteboard → { scene, version }
  hub.JoinBoard(teamId) → BoardJoined(participants, latestScene si es más nueva)

Mientras se dibuja (cliente, con throttle de 100 ms):
  hub.PushElements(teamId, changedElements[])
Servidor:
  valida la pertenencia → reconcilia en memoria → emite ElementsChanged(changedElements) al resto
  → marca la pizarra como "sucia"

Cursores (con throttle de 50 ms, sin persistir):
  hub.PushPointer(teamId, { x, y, tool }) → PointerMoved(userId, displayName, pointer)
```

- **Reconciliación por elemento**: se usan los campos `version` y `versionNonce` de cada elemento de Excalidraw. Gana la `version` mayor; si hay empate, gana el `versionNonce` menor (el mismo algoritmo que Excalidraw). La implementación vive en `Modules/Whiteboard/Classes/SceneReconciler` y tiene pruebas unitarias.
- Los elementos borrados llegan con `isDeleted: true` y se conservan en la escena (tombstones), para que la reconciliación funcione.
- Límite de 2 MB por escena y 500 elementos por mensaje. Si se superan, se rechaza con `WHITEBOARD_TOO_LARGE`.

### Persistencia

- `WhiteboardSnapshotWorker` (`BackgroundService`) guarda en PostgreSQL las escenas sucias **5 s después del último cambio** y también cuando sale el último participante.
- Cada guardado incrementa `version`.
- Si el proceso se reinicia, se pierden como máximo unos 5 s de trazos. Es un riesgo aceptado.

### Exportar

La exportación a PNG se hace en el cliente con `exportToBlob` de Excalidraw. No pasa por el servidor.

### Acceso del docente

El docente de la clase puede abrir la pizarra de cualquier equipo **sin pedir acceso** (es trabajo académico; ver `business-rules.md` §Pizarra). Sus cursores se ven con la etiqueta "Docente".

## Notificaciones en vivo

- `NotificationsHub` emite `NotificationCreated(NotificationDto)` y `UnreadCountChanged(count)` al grupo `user:{userId}`.
- La lista se carga por REST con paginación por cursor. El hub solo avisa.

## Rate limiting en los hubs

| Método | Límite por conexión |
|--------|---------------------|
| `Typing` | 1 cada 3 s (el exceso se ignora) |
| `PushPointer` | 20 por segundo |
| `PushElements` | 15 por segundo |
| `JoinClass`, `JoinTeam`, `JoinBoard` | 30 por minuto |

Si se supera, el exceso se descarta sin cortar la conexión, y se registra en el log como `Warning`.

## Logging

- Conexión y desconexión: `Information` con `UserId`, `ConnectionId` y `Hub`.
- Unión a grupos rechazada: `Warning`.
- **Nunca** registrar el contenido de los mensajes ni la escena de la pizarra.

## Pruebas

- `SceneReconciler`, `PresenceTracker` y la lógica de idempotencia: unitarias.
- Hubs: integración con `WebApplicationFactory` + `HubConnection` contra `TestServer`, autenticados con cookie. Casos: unirse con y sin permiso, recibir el evento después de un POST y reconectar.
- E2E (Playwright): dos contextos de navegador, un mensaje enviado por uno aparece en el otro.
