#!/usr/bin/env bash
# Deploys the latest origin/main on the production server, from the clone at ~/wizer as the
# `wizer` user. The CI deploy key is restricted to this script (authorized_keys `command=`).
# If the new version fails its health check, the previous commit is redeployed and the script
# still exits with an error so the CI job fails.
set -euo pipefail

compose=(docker compose -f docker-compose.prod.yml --env-file .env.prod)

# Waits up to 60 s (30 tries, 2 s apart) for the backend to answer through Caddy.
healthy() {
  local port
  port="$(grep -E '^WIZER_HTTP_PORT=' .env.prod | cut -d= -f2 || true)"
  for _ in $(seq 1 30); do
    if curl -fsS -o /dev/null "http://127.0.0.1:${port:-8088}/health/live"; then
      return 0
    fi
    sleep 2
  done
  return 1
}

# Build cache shared with the other apps on this Docker host; keep it bounded.
cleanup() {
  docker image prune -f >/dev/null
  docker builder prune -f --max-used-space 5GB >/dev/null
}

deploy_commit() {
  git checkout --quiet --detach "$1"
  "${compose[@]}" up -d --build --remove-orphans
}

main() {
  cd "$(dirname "$(readlink -f "$0")")/../.."
  local previous target
  previous="$(git rev-parse HEAD)"
  git fetch --quiet origin main
  target="$(git rev-parse origin/main)"
  echo "Deploying ${target:0:7} (previous ${previous:0:7})"

  if deploy_commit "$target" && healthy; then
    cleanup
    echo "Deployed ${target:0:7}"
    return 0
  fi

  echo "Health check failed for ${target:0:7}; rolling back to ${previous:0:7}" >&2
  "${compose[@]}" ps >&2
  # Migrations are not reverted: a rollback across a schema change needs a manual decision.
  if deploy_commit "$previous" && healthy; then
    echo "Rolled back to ${previous:0:7}" >&2
  else
    echo "Rollback to ${previous:0:7} also failed" >&2
  fi
  return 1
}

# Kept on one line: git checkout may rewrite this file while bash is still reading it.
main "$@"; exit $?
