# Wizer — Quality gates

## Objetivo

Los controles automáticos que todo cambio debe pasar antes de mergearse. Son deterministas: los ejecuta una máquina, no dependen del criterio del agente. La revisión humana o del agente evalúa diseño y criterio; los gates evalúan lo que se puede verificar con un comando.

Una regla que ningún comando verifica es una sugerencia. Si una regla de `.ia/` se puede automatizar, su gate vive acá.

## Punto de entrada único

```text
make check
```

Corre, en orden: `dotnet format --verify-no-changes`, `npm run lint` (oxlint), `npm run format:check` (Prettier), `npm run typecheck`, `dotnet test` y `npm test`. La CI (`ci.yml`) corre los mismos gates y además los que solo viven en la CI (ver la tabla).

El agente corre este comando antes de dar por terminada una tarea y reporta la salida real.

## Niveles

El proyecto adopta un nivel y lo sube cuando lo necesita. Cada nivel incluye los anteriores.

| Nivel | Gates | Cuándo adoptarlo |
|-------|-------|------------------|
| 1. Base | Formato, lint, typecheck, pruebas | Siempre |
| 2. Seguridad | Secretos, dependencias vulnerables (SCA), análisis estático de seguridad (SAST) | Código en producción, datos de usuarios o dependencias de terceros |
| 3. Plataforma de calidad | Cobertura del código nuevo, duplicación, complejidad, historial (SonarQube o similar) | Equipo, proyecto de larga vida o requisito del cliente |
| 4. Runtime | Análisis dinámico (DAST) contra la app corriendo, por ejemplo OWASP ZAP | App web expuesta a internet |

Nivel adoptado: **1 completo y 2 parcial** (secretos y Dependabot; falta SAST). Wizer guarda datos de alumnos y archivos, así que el nivel 2 completo es el próximo paso.

## Gates

| Gate | Herramienta | Comando | Dónde corre | ¿Bloquea? |
|------|-------------|---------|-------------|-----------|
| Formato backend | dotnet format | `dotnet format --verify-no-changes` | `make check`, CI `backend` | Sí |
| Formato frontend | Prettier | `npm run format:check` | `make check`, CI `frontend` | Sí |
| Lint frontend | oxlint | `npm run lint` | `make check`, CI `frontend` | Sí |
| Typecheck frontend | tsc | `npm run typecheck` | `make check`, CI `frontend` | Sí |
| Build backend | dotnet build | `dotnet build -c Release` (warnings = errores) | CI `backend` | Sí |
| Pruebas backend | xUnit + Testcontainers | `dotnet test` | `make check`, CI `backend` | Sí |
| Pruebas frontend | Vitest | `npm test` (CI: `npm run coverage`) | `make check`, CI `frontend` | Sí |
| Contrato de la API | Orval | Regenera OpenAPI y cliente; falla si hay cambios sin commitear | CI `api-contract` | Sí |
| Secretos | TruffleHog `v3.99.3` | Sobre el historial | CI `secrets` | Sí |
| Dependencias | Dependabot | PRs semanales (NuGet, npm, Actions, imágenes) | GitHub | No |

## Umbrales

- Build Release: cero warnings (`Directory.Build.props`).
- Secretos: cero hallazgos.
- Cobertura: se recolecta (backend y frontend) pero no hay umbral mínimo. <!-- PENDIENTE: ¿se fija una cobertura mínima del código nuevo? -->

## Excepciones

Un hallazgo falso positivo o aceptado se suprime solo:

- en el archivo de configuración de la herramienta o con la anotación propia de la herramienta, nunca desactivando el gate entero;
- con un comentario que explica el motivo;
- con aprobación del usuario.

<!-- PENDIENTE: dónde viven las supresiones de cada herramienta (oxlint, TruffleHog). -->

## Reglas para el agente

- Correr `make check` antes de terminar una tarea; si falla, arreglar la causa, no el gate.
- **Pedir confirmación** antes de desactivar un gate, bajar un umbral, agregar una supresión, saltear hooks (`--no-verify`) o modificar la configuración de la CI.
- No marcar una tarea como terminada con un gate bloqueante en rojo.
- Si un gate falla por algo ajeno al cambio (ya fallaba en la rama base), reportarlo como tal, con la evidencia, en lugar de ocultarlo.
- Un hallazgo de seguridad (secreto, CVE, SAST) se reporta siempre, aunque no bloquee.
