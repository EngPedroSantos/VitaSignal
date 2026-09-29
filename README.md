
# VitaSignal

A .NET platform that ingests simulated real-time telemetry from patient vital-sign monitoring devices, evaluates readings against normal ranges, and is instrumented end-to-end with OpenTelemetry, Prometheus, and Grafana.

Built as a portfolio project to demonstrate architecture, cost-awareness, and business judgment — not just working code.

> **On the data:** all patient and vital-sign data in this system is synthetic. See [Privacy by design](#privacy-by-design) below.

## Context

VitaSignal simulates a real-time patient vital-sign monitoring platform: patients are registered, devices report readings (heart rate, SpO2, body temperature, blood pressure), and each reading is evaluated against a normal reference range. The domain was chosen deliberately: it connects to the author's professional experience with healthcare operator/provider systems, while generating naturally rich telemetry data.

**Stack:** .NET 10 (LTS) · C# · ASP.NET Core (Controllers) · PostgreSQL · Entity Framework Core · Docker Compose · OpenTelemetry · Prometheus · Grafana

## Architecture

Clean Architecture, four layers, dependencies pointing inward only:

```
VitaSignal.Domain          → no dependencies
VitaSignal.Application     → depends on Domain
VitaSignal.Infrastructure  → depends on Application
VitaSignal.Api             → depends on Infrastructure (composition root)
```

- **Domain** — `Patient`, `VitalReading`, `VitalRange`/`VitalRangeCatalog`. Immutable entities, validated at construction, zero framework dependencies.
- **Application** — use cases (`RegisterPatientUseCase`, `RegisterVitalReadingUseCase`) and the repository interfaces they depend on. Simple reads bypass use cases and go straight to a repository (a deliberate CQS choice).
- **Infrastructure** — EF Core `DbContext`, entity configurations, and repository implementations against PostgreSQL.
- **Api** — ASP.NET Core controllers, dependency injection wiring, centralized exception-to-HTTP mapping.

## How to run

Requires Docker Desktop.

```bash
docker compose up --build
```

This starts the API and a PostgreSQL container together, and applies pending EF Core migrations automatically on startup. Once running:

- API + interactive docs: `http://localhost:8080/scalar/v1`
- PostgreSQL: `localhost:5432` (database `vitasignal`, see `docker-compose.yml` for credentials — local development only, never real credentials)

Data persists across `docker compose down` / `up` via a named volume.

### Running tests

```bash
dotnet test
```

## Privacy by design

Even though all data here is synthetic, the system is designed as if it handled real patient data: no real PII is ever seeded or accepted, and every stored field exists for a stated reason.

## Costs and trade-offs

> **Status: not yet completed.** This section will be filled in once the project is deployed to Azure, with a real estimated monthly cost and the trade-offs behind the infrastructure choices (container hosting option, database tier, observability stack overhead). Tracked as a later step in the project roadmap.

What's already decided with a cost rationale in mind:
- **PostgreSQL over SQL Server** — no licensing cost, in any environment.
- **Multi-stage Docker build** — the published image ships only the ASP.NET Core runtime, not the full SDK, keeping image size (and therefore registry storage and deploy time) down.
