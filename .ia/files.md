# Wizer — Archivos (Garage y ClamAV)

## Objetivo

Definir cómo se suben, escanean, guardan, sirven y borran los archivos de clases, tareas, entregas y adjuntos del chat.

## Principios

1. **Binarios en Garage (S3), metadatos en PostgreSQL** (tabla `files`).
2. **URLs firmadas** para subir y descargar. La API no transporta los bytes.
3. **Ningún archivo se descarga antes del escaneo antivirus.**
4. **Autorización en cada descarga**: se pide una URL nueva y de vida corta.
5. Los módulos usan `IFileStorage` e `IAntivirusScanner`, nunca un SDK de S3 directamente.

## Contextos

Cada archivo pertenece a un contexto, y ese contexto define quién puede verlo:

| `context_type` | `context_id` | Quién sube | Quién descarga |
|----------------|--------------|------------|----------------|
| `class_material` | `class_id` | Miembros de la clase | Miembros de la clase |
| `assignment_material` | `assignment_id` | Docente | Miembros de la clase |
| `submission` | `submission_id` | Alumno o equipo dueño | Dueño(s) y docente |
| `chat_attachment` | `channel_id` | Miembros del canal | Miembros del canal |
| `avatar` | `user_id` | El usuario | Cualquier usuario autenticado |

## Límites

| Regla | Valor |
|-------|-------|
| Tamaño máximo por archivo | 25 MB |
| Cuota por clase | 2 GB (suma de todos los contextos de la clase) |
| Avatar | 2 MB, solo imágenes |
| Archivos por entrega | 10 |

Los valores viven en configuración (`Files:MaxFileSizeBytes`, etc.), no hardcodeados.

## Tipos permitidos

| Categoría | Extensiones | MIME |
|-----------|-------------|------|
| Documentos | `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`, `.txt` | según extensión |
| Imágenes | `.png`, `.jpg`, `.jpeg`, `.webp`, `.gif` | `image/*` |
| Comprimidos | `.zip` | `application/zip` |

- **Videos: no se suben.** Se comparten como enlace (las entregas y los mensajes admiten URLs).
- La validación revisa la extensión, el MIME declarado **y los bytes mágicos** del objeto subido. Si no coinciden, el archivo se rechaza con `UPLOAD_INVALID_TYPE`.
- Los SVG se rechazan, porque pueden contener scripts.

## Flujo de subida

```text
1. POST /api/v1/files/uploads
   { contextType, contextId, fileName, contentType, sizeBytes }
   → valida el permiso sobre el contexto, el tipo, el tamaño y la cuota
   → crea la fila files (status = uploading, object_key generado)
   → 201 { fileId, uploadUrl, expiresAt }   (PUT firmado, 10 min)

2. PUT {uploadUrl}  (el navegador, directo a Garage vía /storage)

3. POST /api/v1/files/{fileId}/complete
   → verifica que el objeto exista, su tamaño real y los bytes mágicos
   → status = pending_scan
   → encola el escaneo
   → 202 { file }

4. AntivirusScanWorker
   → stream del objeto → clamd (INSTREAM)
   → limpio:     status = available
   → infectado:  status = rejected, borra el objeto, registra Warning
   → error:      reintenta 3 veces con backoff; después status = scan_failed
   → emite el evento FileScanCompleted → notificación o tiempo real para quien subió
```

- `object_key` = `{institutionId}/{contextType}/{contextId}/{fileId}`. **Nunca** se usa el nombre original del archivo en la clave.
- Los archivos en `uploading` por más de 1 h se limpian con un trabajo periódico.
- Al iniciar, el worker reencola los archivos que quedaron en `pending_scan`.

## Flujo de descarga

```text
GET /api/v1/files/{fileId}/download-url?disposition=inline|attachment
→ verifica status = available y el permiso sobre el contexto
→ 200 { url, expiresAt }   (GET firmado, 5 min,
                            con Content-Disposition y el nombre original)
```

- Las URLs nunca se guardan ni se comparten como permanentes.
- `inline` solo se permite para imágenes y PDF (vista previa).

## Vista previa

- Imágenes: `<img>` con la URL firmada `inline`.
- PDF: `react-pdf`, con la URL firmada `inline`.
- Los demás tipos: solo descarga.

## Borrado

- Soft delete en `files` (`deleted_at`). El objeto en Garage se borra con un trabajo periódico 30 días después.
- Pueden borrar: quien subió el archivo y el docente de la clase. Los archivos de una entrega ya enviada no se pueden borrar (hay que anular la entrega antes, mientras el plazo lo permita).

## Garage

- Un bucket privado (`wizer`): Garage no tiene acceso público salvo que se active el modo web, que no se usa.
- Se expone al navegador **solo** a través del reverse proxy en `/storage`, para que las URLs firmadas sean del mismo origen.
- La firma SigV4 cubre el `Host` y el path **tal como llegan a Garage**. `IFileStorage` firma el path `/{bucket}/{key}` con el `Host` que deja el proxy, y después inserta `/storage` delante del path de la URL que devuelve. En desarrollo Vite quita `/storage` y cambia el `Host` a `localhost:3900` (`changeOrigin`), así que se firma con el host de `Garage:S3Endpoint`. En producción, Caddy tiene que hacer lo mismo o conservar el `Host` público y firmar con ese. Una prueba de integración del flujo completo lo confirma.
- Configuración del backend: `Garage:S3Endpoint` y `Garage:AdminEndpoint` (`/health/ready` usa `GET /health` de la API de administración).
- Las credenciales están en variables de entorno. Nunca en código ni en logs.

## ClamAV

- Contenedor `clamav/clamav`. Necesita unos 1,5 GB de RAM y tarda unos minutos en arrancar mientras descarga las firmas.
- `IAntivirusScanner` usa el protocolo clamd (`INSTREAM`) por TCP.
- `/health/ready` informa si ClamAV no está disponible. Mientras tanto, los archivos se quedan en `pending_scan` y no se pierden.

## Pruebas

- Unitarias: validación de tipo, tamaño, cuota y permisos por contexto.
- Integración (Testcontainers con Garage, la misma imagen y `infra/garage/garage.toml` que usa `GarageHealthCheckTests`): el flujo completo de subida y descarga. ClamAV se reemplaza por un doble en las pruebas de integración y se valida con un contenedor real solo en el job de CI `e2e`, usando el archivo de prueba EICAR.
