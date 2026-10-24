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

.DEFAULT_GOAL := help
.PHONY: help setup infra-up infra-up-av infra-down infra-reset infra-logs infra-ps \
	api web test test-back test-front lint format api-client check

help: ## List available commands
	@grep -E '^[a-zA-Z_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-14s\033[0m %s\n", $$1, $$2}'

setup: ## First-time setup: .env, backend restore, frontend install
	@test -f .env || ! test -f .env.example || cp .env.example .env
	dotnet restore $(BACKEND)/Wizer.slnx
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

api: ## Run the API with hot reload on http://localhost:5080
	ConnectionStrings__Wizer="Host=localhost;Port=$(POSTGRES_PORT);Database=$(POSTGRES_DB);Username=$(POSTGRES_USER);Password=$(POSTGRES_PASSWORD)" \
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

check: lint test ## Everything CI runs, locally
