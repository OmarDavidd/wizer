#!/usr/bin/env bash
# Deploys the latest origin/main on the production server, from the clone at ~/wizer as the
# `wizer` user. The CI deploy key is restricted to this script (authorized_keys `command=`).
set -euo pipefail

main() {
  cd "$(dirname "$(readlink -f "$0")")/../.."
  git fetch --quiet origin main
  git checkout --quiet --detach origin/main
  local commit
  commit="$(git rev-parse --short HEAD)"
  echo "Deploying ${commit}"

  local compose=(docker compose -f docker-compose.prod.yml --env-file .env.prod)
  "${compose[@]}" up -d --build --remove-orphans

  local port
  port="$(grep -E '^WIZER_HTTP_PORT=' .env.prod | cut -d= -f2 || true)"
  for _ in $(seq 1 30); do
    if curl -fsS -o /dev/null "http://127.0.0.1:${port:-8088}/health/live"; then
      docker image prune -f >/dev/null
      echo "Deployed ${commit}"
      return 0
    fi
    sleep 2
  done

  echo "Health check failed for ${commit}" >&2
  "${compose[@]}" ps >&2
  return 1
}

# Kept on one line: git checkout may rewrite this file while bash is still reading it.
main "$@"; exit $?
