# Wizer — Infraestructura

## Objetivo

Reglas de Docker, entornos, CI y despliegue.

## Principios

1. Todo corre con Docker Compose, en desarrollo y en producción.
2. Sin dependencia de Coolify ni de ninguna plataforma de despliegue.
3. Un solo origen detrás de un reverse proxy (`architecture.md`).
4. La configuración se hace por variables de entorno. Las imágenes son las mismas en todos los entornos.
5. Una sola instancia de la API (ADR-04).

## Entornos

| Entorno | Dónde | Archivo |
|---------|-------|---------|
| Desarrollo | Máquina de cada dev | `docker-compose.dev.yml` |
| CI | GitHub Actions | `docker-compose.ci.yml` |
| Producción | Home Lab | `docker-compose.prod.yml` |

No hay staging en el MVP.

## Servicios

| Servicio | Imagen | Notas |
|----------|--------|-------|
| `proxy` | Caddy | Enruta `/`, `/api`, `/hubs` (WebSocket) y `/storage`. En producción emite TLS |
| `frontend` | Dev: Node + Vite · Prod: nginx con el build | Fallback de la SPA a `index.html` |
| `backend` | `mcr.microsoft.com/dotnet/aspnet:10.0` | Usuario no root. Health check `/health/ready` |
| `migrator` | EF migration bundle | One-shot antes del backend (producción y CI) |
| `postgres` | `postgres:17` | Volumen persistente |
| `garage` | `dxflrs/garage` | Almacenamiento S3. Volúmenes persistentes. Bucket privado `wizer` |
| `clamav` | `clamav/clamav` | Unos 1,5 GB de RAM. Volumen para las firmas |
| `mailpit` | `axllent/mailpit` | Solo en desarrollo y CI. UI en `:8025` |
| `alloy` | `grafana/alloy` | Producción. Envía a Loki |

### Desarrollo

En desarrollo, **la API y el frontend corren en el host** con recarga en caliente. Docker Compose solo levanta la infraestructura:

| Servicio | Cuándo | Puerto (solo `127.0.0.1`) |
|----------|--------|---------------------------|
| `postgres` | siempre | `5432` |
| `mailpit` | siempre | SMTP `1025`, UI http://localhost:8025 |
| `clamav` | perfil `av` (`make infra-up-av`, espera al health check `clamdcheck.sh`), hace falta desde el módulo Files | `3310` |
| `garage` | siempre | API S3 `3900`, API de administración `3903` (solo `GET /health`) |

Vite hace de proxy de `/api`, `/hubs` (con `ws: true`) y `/storage` hacia la API (`http://localhost:5080`) y el almacenamiento, así se mantiene un solo origen igual que en producción (`apps/frontend/vite.config.ts`). En desarrollo no hace falta Caddy.

### Almacenamiento

Se usa **Garage** (`dxflrs/garage:v2.4.1`, AGPLv3), un almacenamiento compatible con S3 pensado para self-hosting. Reemplazó a MinIO el 2026-10-24: la edición comunitaria de MinIO entró en modo mantenimiento en diciembre de 2025 y se archivó en 2026, y sus imágenes oficiales dejaron de publicarse.

- Configuración: `infra/garage/garage.toml`, sin secretos. `docker-compose.dev.yml` pasa `GARAGE_RPC_SECRET` y arranca con `garage server --single-node --default-bucket`, que en cada inicio configura el nodo y crea la clave de acceso y el bucket privado desde `GARAGE_DEFAULT_ACCESS_KEY`, `GARAGE_DEFAULT_SECRET_KEY` y `GARAGE_DEFAULT_BUCKET`. No hace falta un contenedor de inicialización.
- Los valores de desarrollo (`GARAGE_ACCESS_KEY`, `GARAGE_SECRET_KEY`, `GARAGE_RPC_SECRET`) tienen un valor por defecto de baja entropía en el Compose; en producción se generan con `openssl rand -hex 32` (la clave de acceso es `GK` + 24 caracteres hexadecimales).
- La imagen no tiene shell. El CLI se usa con `docker compose -f docker-compose.dev.yml exec garage /garage status` (o `bucket list`, `key list`).
- Lo que Wizer usa está soportado según la documentación de Garage y se **verificó a mano** el 2026-10-24 contra `v2.4.1`: URLs firmadas (SigV4) de `PUT` y `GET`, `response-content-disposition` y `response-content-type` en la descarga, y bucket privado (sin firma o con firma inválida responde 403). Todavía no hay una prueba automática: la agrega la implementación de `IFileStorage`. Garage no implementa políticas de bucket, versionado ni IAM, y Wizer no los necesita.
- Para actualizar: cambiar el tag de la imagen en el Compose (y en `GarageHealthCheckTests`).
- Si quedó un volumen viejo de MinIO: `docker volume rm wizer-dev_minio_data`.
- El código usa la abstracción `IFileStorage` (`files.md`). Cambiar de proveedor S3 no toca los módulos.

### Correo

- **Desarrollo y CI:** Mailpit (SMTP). Los correos no salen a internet y se ven en http://localhost:8025. No necesita API key ni dominio, y las pruebas E2E pueden leer los códigos de verificación.
- **Producción:** Resend, con el dominio del proyecto verificado.
- Las dos implementan `IEmailSender`. Se elige con `EMAIL_PROVIDER=smtp|resend`.

## Comandos (`Makefile`)

Ejecutar `make` (o `make help`) desde la raíz para ver la lista. Los principales:

| Comando | Qué hace |
|---------|----------|
| `make setup` | Primera vez: crea `.env` desde `.env.example`, `dotnet restore` y `npm ci` |
| `make infra-up` | Levanta PostgreSQL, Garage (con el bucket) y Mailpit en segundo plano |
| `make infra-up-av` | Lo mismo más ClamAV |
| `make infra-down` | Apaga la infraestructura (conserva los datos) |
| `make infra-reset` | Apaga y **borra** los volúmenes (base de datos vacía) |
| `make infra-logs` / `make infra-ps` | Logs / estado de los contenedores |
| `make api` | API con `dotnet watch` en http://localhost:5080 |
| `make web` | Frontend en http://localhost:5173 |
| `make test` | Todas las pruebas (`test-back` necesita Docker para Testcontainers) |
| `make lint` / `make format` | Verificar / aplicar formato y lint de back y front |
| `make api-client` | Regenera el OpenAPI y el cliente de Orval. Correrlo después de cambiar un endpoint |
| `make check` | Lo mismo que corre la CI, en local |

Flujo diario:

```bash
make infra-up     # una vez por sesión
make api          # terminal 1
make web          # terminal 2
```

Sin `make` (por ejemplo, en Windows sin WSL), los comandos equivalentes están dentro del `Makefile` y se pueden copiar tal cual.

## Exposición de puertos

- Producción: **solo** `proxy` expone puertos (80 y 443). PostgreSQL, Garage y ClamAV no se publican al host.
- Desarrollo: se pueden publicar PostgreSQL (`5432`), Garage (`3900` y `3903`) y Mailpit (`8025`) en `127.0.0.1`.

## Redes

- `internal`: backend, postgres, garage, clamav y migrator.
- `edge`: proxy, frontend y backend.
- Garage se alcanza desde el navegador solo vía `proxy` → `/storage`.

## Variables de entorno

`.env.example` versionado, con todas las variables y valores ficticios. `.env` ignorado.

```text
ASPNETCORE_ENVIRONMENT
APP_PUBLIC_URL                 https://wizer.example
DB_HOST / DB_NAME / DB_USER / DB_PASSWORD
JWT_SIGNING_KEY
JWT_ACCESS_MINUTES=15
JWT_REFRESH_DAYS=7
JWT_REFRESH_REMEMBER_DAYS=30
GOOGLE_CLIENT_ID
GARAGE_S3_ENDPOINT / GARAGE_ADMIN_ENDPOINT
GARAGE_ACCESS_KEY / GARAGE_SECRET_KEY / GARAGE_RPC_SECRET / GARAGE_BUCKET=wizer
STORAGE_PUBLIC_BASE_URL        https://wizer.example/storage
CLAMAV_HOST / CLAMAV_PORT=3310
EMAIL_PROVIDER                 resend | smtp
RESEND_API_KEY
EMAIL_FROM                     "Wizer <no-reply@wizer.example>"
SMTP_HOST / SMTP_PORT          (mailpit en dev y ci)
SEED_ENABLED
SEED_ADMIN_EMAIL / SEED_ADMIN_PASSWORD   (primer administrador en prod)
VITE_GOOGLE_CLIENT_ID
```

## Dockerfiles

- Multi-stage: build con SDK y runtime con la imagen `aspnet` (backend) o `nginx:alpine` (frontend).
- Usuario no root, `HEALTHCHECK` y etiquetas OCI.
- `.dockerignore` excluye `bin/`, `obj/`, `node_modules/`, `.env` y `legacy/`.

## Persistencia

Volúmenes con nombre: `pg_data`, `garage_meta`, `garage_data`, `clamav_db`.

## Backups

**Propuesta operativa** (ajustar al Home Lab):

| Qué | Cómo | Frecuencia | Retención |
|-----|------|------------|-----------|
| PostgreSQL | `pg_dump -Fc` desde un contenedor `backup` con cron | Diario, 03:00 | 7 diarios + 4 semanales |
| Garage | `rclone sync` del bucket (S3) a un disco de backup | Diario, 03:30 | Espejo + snapshot semanal (4) |
| Prueba de restauración | Restaurar el dump en un contenedor temporal | Mensual | — |

Scripts en `infra/backup/`. Los backups no se guardan en el mismo disco que los volúmenes.

## CI/CD (GitHub Actions)

### `ci.yml` — en cada PR y push a `develop` y `main` (implementado)

| Job | Pasos |
|-----|-------|
| `backend` | `dotnet restore --locked-mode` → `dotnet format --verify-no-changes` → build Release (warnings = errores) → unit tests con cobertura → integration tests (Testcontainers) |
| `frontend` | `npm ci` → oxlint → prettier check → typecheck → `vitest --coverage` → build |
| `api-contract` | Build del backend (regenera `openapi/wizer-api.json`) → `orval` → falla si el OpenAPI o `src/api/generated` tienen cambios **o archivos nuevos** sin commitear (`git status --porcelain`) |
| `secrets` | trufflehog (fijado a una versión, `v3.99.3`; Dependabot lo actualiza) sobre el historial |

Los nombres de los jobs (`backend`, `frontend`, `api-contract`, `secrets`) son los que se marcan como *required status checks* en la ruleset.

### `e2e.yml` — en cada PR a `main` y manual

Levanta `docker-compose.ci.yml` → espera los health checks → Playwright → sube el reporte y los traces si falla.

### `release.yml` — al mergear a `main`

Construye las imágenes `backend`, `frontend` y `migrator` → las publica en `ghcr.io/omardavidd/wizer-*` con el tag del commit y `latest`.

### Protección de ramas

- `main` y `develop` protegidas: requieren `ci` en verde y 1 aprobación. Sin push directo.
- `main` además requiere `e2e` en verde.

### Dependabot

Semanal para NuGet, npm y GitHub Actions.

## Despliegue (MVP)

Manual en el Home Lab:

```bash
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d    # migrator corre antes del backend
```

Automatizarlo (runner self-hosted o webhook) queda para después del MVP.

## Dominio

Todavía no hay dominio. Hasta tenerlo:

- El Home Lab se accede por la red local o por un túnel.
- **Resend necesita un dominio verificado** para enviar correos a cualquier destinatario (`docs/03-riesgos.md`).

## Prohibiciones

Los agentes NO deben:

- introducir Kubernetes, Coolify ni servicios gestionados sin autorización;
- hardcodear IPs, dominios o credenciales del Home Lab;
- publicar puertos de base de datos, Garage o ClamAV en producción;
- modificar `docker-compose.prod.yml`, los workflows de CI o la protección de ramas sin confirmación.
