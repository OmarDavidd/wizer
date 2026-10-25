# Wizer — development commands. Run `make` or `make help` to list them.

COMPOSE := docker compose -f docker-compose.dev.yml
BACKEND := apps/backend
FRONTEND := apps/frontend

# Local settings come from .env (optional); defaults match docker-compose.dev.yml.
-include .env
POSTGRES_DB ?= wizer
POSTGRES_USER ?= wizer
POSTGRES_PASSWORD ?= wizer_dev
POSTGRES_PORT ?= 5432
# Password of every seeded account (admin@wizer.local, docente1@wizer.local, alumno01@wizer.local…).
SEED_PASSWORD ?= WizerDev2026

.DEFAULT_GOAL := help
.PHONY: help setup infra-up infra-up-av infra-down infra-reset infra-logs infra-ps \
	dev dev-api dev-web api web test test-back test-front lint format api-client migration check

help: ## List available commands
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-14s\033[0m %s\n", $$1, $$2}'

setup: ## First-time setup: .env, backend restore, frontend install
	@test -f .env || ! test -f .env.example || cp .env.example .env
	dotnet restore $(BACKEND)/Wizer.slnx
	dotnet tool restore
	npm ci --prefix $(FRONTEND)

infra-up: ## Start PostgreSQL, Garage (S3 storage) and Mailpit in the background
	$(COMPOSE) up -d --wait postgres garage mailpit

infra-up-av: infra-up ## Same as infra-up plus ClamAV; waits until it is ready (first start can take minutes)
	$(COMPOSE) --profile av up -d --wait clamav

infra-down: ## Stop infrastructure (keeps data)
	$(COMPOSE) --profile av down

infra-reset: ## Stop infrastructure and DELETE its data volumes
	$(COMPOSE) --profile av down -v

infra-logs: ## Follow infrastructure logs
	$(COMPOSE) --profile av logs -f

infra-ps: ## Show infrastructure containers
	$(COMPOSE) --profile av ps

# make -j runs both as foreground children, so Ctrl+C reaches them (a shell's `cmd &` would start them
# ignoring SIGINT). sed ignores Ctrl+C and exits when its writer does, so shutdown logs still get through.
dev: infra-up ## Start the infrastructure, then the API and the frontend together with prefixed logs (Ctrl+C stops both)
	@$(MAKE) --no-print-directory -j2 dev-api dev-web

dev-api:
	@$(MAKE) --no-print-directory api 2>&1 | (trap '' INT; sed -u 's/^/[api] /')

dev-web:
	@$(MAKE) --no-print-directory web 2>&1 | (trap '' INT; sed -u 's/^/[web] /')

api: ## Run the API with hot reload on http://localhost:5080
	ConnectionStrings__Wizer="Host=localhost;Port=$(POSTGRES_PORT);Database=$(POSTGRES_DB);Username=$(POSTGRES_USER);Password=$(POSTGRES_PASSWORD)" \
	Seed__Enabled=true Seed__Password="$(SEED_PASSWORD)" \
		dotnet watch --project $(BACKEND)/src/Wizer.Api run

web: ## Run the frontend on http://localhost:5173
	npm run dev --prefix $(FRONTEND)

test: test-back test-front ## Run all tests

test-back: ## Backend unit + integration tests (integration needs Docker)
	dotnet test $(BACKEND)/Wizer.slnx

test-front: ## Frontend tests
	npm test --prefix $(FRONTEND)

lint: ## Lint and verify formatting (backend + frontend)
	dotnet format $(BACKEND)/Wizer.slnx --verify-no-changes
	npm run lint --prefix $(FRONTEND)
	npm run format:check --prefix $(FRONTEND)
	npm run typecheck --prefix $(FRONTEND)

format: ## Auto-format backend and frontend
	dotnet format $(BACKEND)/Wizer.slnx
	npm run format --prefix $(FRONTEND)

api-client: ## Rebuild the OpenAPI document and regenerate the frontend client (Orval)
	dotnet build $(BACKEND)/src/Wizer.Api
	npm run api:generate --prefix $(FRONTEND)

migration: ## Add an EF Core migration: make migration name=AddSomething
	@test -n "$(name)" || { echo "Usage: make migration name=PascalCaseName"; exit 1; }
	dotnet ef migrations add $(name) --project $(BACKEND)/src/Wizer.Api --output-dir Infrastructure/Persistence/Migrations

check: lint test ## Everything CI runs, locally
