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
| `minio` | `minio/minio` | Volumen persistente. Bucket privado `wizer` |
| `minio-init` | `minio/mc` | Crea el bucket al iniciar |
| `clamav` | `clamav/clamav` | Unos 1,5 GB de RAM. Volumen para las firmas |
| `mailpit` | `axllent/mailpit` | Solo en desarrollo y CI. UI en `:8025` |
| `alloy` | `grafana/alloy` | Producción. Envía a Loki |

En desarrollo, Vite hace de proxy de `/api`, `/hubs` y `/storage` hacia los contenedores para mantener un solo origen (`vite.config.ts` → `server.proxy`, con `ws: true` para los hubs).

## Exposición de puertos

- Producción: **solo** `proxy` expone puertos (80 y 443). PostgreSQL, MinIO y ClamAV no se publican al host.
- Desarrollo: se pueden publicar PostgreSQL (`5432`), la consola de MinIO (`9001`) y Mailpit (`8025`) en `127.0.0.1`.

## Redes

- `internal`: backend, postgres, minio, clamav y migrator.
- `edge`: proxy, frontend y backend.
- MinIO se alcanza desde el navegador solo vía `proxy` → `/storage`.

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
MINIO_ENDPOINT / MINIO_ACCESS_KEY / MINIO_SECRET_KEY / MINIO_BUCKET=wizer
MINIO_PUBLIC_BASE_URL          https://wizer.example/storage
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

Volúmenes con nombre: `pg_data`, `minio_data`, `clamav_db`.

## Backups

**Propuesta operativa** (ajustar al Home Lab):

| Qué | Cómo | Frecuencia | Retención |
|-----|------|------------|-----------|
| PostgreSQL | `pg_dump -Fc` desde un contenedor `backup` con cron | Diario, 03:00 | 7 diarios + 4 semanales |
| MinIO | `mc mirror` del bucket a un disco de backup | Diario, 03:30 | Espejo + snapshot semanal (4) |
| Prueba de restauración | Restaurar el dump en un contenedor temporal | Mensual | — |

Scripts en `infra/backup/`. Los backups no se guardan en el mismo disco que los volúmenes.

## CI/CD (GitHub Actions)

### `ci.yml` — en cada PR a `develop` y `main`

| Job | Pasos |
|-----|-------|
| `backend` | `dotnet restore` → `dotnet format --verify-no-changes` → build → unit tests → integration tests (Testcontainers) → reporte de cobertura |
| `frontend` | `npm ci` → lint → typecheck → `vitest --coverage` → build |
| `api-contract` | Genera el OpenAPI desde el backend → `orval` → falla si `src/api/generated` cambia sin commitear |
| `secrets` | trufflehog sobre el diff |

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
- publicar puertos de base de datos, MinIO o ClamAV en producción;
- modificar `docker-compose.prod.yml`, los workflows de CI o la protección de ramas sin confirmación.
