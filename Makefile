# SkyRoute — top-level Makefile
# Requires: dotnet 8 SDK, Node 20+, npm, dotnet-ef (global tool)
# On Windows: GNU Make from Git for Windows or Chocolatey (`choco install make`)

BACKEND_SLN   := backend/SkyRoute.sln
BACKEND_API   := backend/src/SkyRoute.Api
BACKEND_INF   := backend/src/SkyRoute.Infrastructure
FRONTEND_DIR  := frontend
MIGRATION     ?= Migration

.PHONY: all build backend frontend \
        backend-run frontend-run \
        test backend-test \
        migrate migrate-add migrate-remove \
        clean backend-clean frontend-clean \
        help

# ── Default ──────────────────────────────────────────────────────────────────

all: build

help:
	@echo ""
	@echo "  SkyRoute — available targets"
	@echo ""
	@echo "  Build"
	@echo "    make build            Build both backend (Release) and frontend"
	@echo "    make backend          dotnet build (Release)"
	@echo "    make frontend         ng build (production)"
	@echo ""
	@echo "  Run (development)"
	@echo "    make backend-run      dotnet run --launch-profile http"
	@echo "    make frontend-run     ng serve (http://localhost:4200)"
	@echo ""
	@echo "  Test"
	@echo "    make test             Run all backend unit tests"
	@echo ""
	@echo "  Database / migrations"
	@echo "    make migrate          Apply pending EF Core migrations"
	@echo "    make migrate-add      Add new migration  (MIGRATION=Name)"
	@echo "    make migrate-remove   Remove last migration"
	@echo ""
	@echo "  Clean"
	@echo "    make clean            Remove all build artifacts"
	@echo "    make backend-clean    Remove backend bin/ and obj/ only"
	@echo "    make frontend-clean   Remove frontend dist/ and node_modules/"
	@echo ""

# ── Build ─────────────────────────────────────────────────────────────────────

build: backend frontend

backend:
	dotnet build $(BACKEND_SLN) -c Release --no-incremental

frontend:
	cd $(FRONTEND_DIR) && npm ci && npx ng build --configuration production

# ── Run (dev) ─────────────────────────────────────────────────────────────────

backend-run:
	dotnet run --project $(BACKEND_API) --launch-profile http

frontend-run:
	cd $(FRONTEND_DIR) && npx ng serve --open

# ── Test ──────────────────────────────────────────────────────────────────────

test: backend-test

backend-test:
	dotnet test $(BACKEND_SLN) -c Release --no-build --logger "console;verbosity=normal"

# ── Database ──────────────────────────────────────────────────────────────────

migrate:
	dotnet ef database update \
	  --project $(BACKEND_INF) \
	  --startup-project $(BACKEND_API)

migrate-add:
	dotnet ef migrations add $(MIGRATION) \
	  --project $(BACKEND_INF) \
	  --startup-project $(BACKEND_API) \
	  --output-dir Persistence/Migrations

migrate-remove:
	dotnet ef migrations remove \
	  --project $(BACKEND_INF) \
	  --startup-project $(BACKEND_API)

# ── Clean ─────────────────────────────────────────────────────────────────────

clean: backend-clean frontend-clean

backend-clean:
	dotnet clean $(BACKEND_SLN)

frontend-clean:
	cd $(FRONTEND_DIR) && rm -rf dist .angular
