
# VitaSignal

A .NET platform that ingests simulated real-time telemetry from patient vital-sign monitoring devices, evaluates every reading against clinical ranges, raises alerts, and is instrumented end-to-end with OpenTelemetry, Prometheus and Grafana.

Built as a portfolio project to demonstrate architecture, cost-awareness, and business judgment — not just working code.

> **On the data:** all patient and vital-sign data in this system is synthetic. See [Privacy by design](#privacy-by-design) below.

## Context

VitaSignal simulates a real-time patient vital-sign monitoring platform: patients are registered, bedside devices report readings (heart rate, SpO2, body temperature, blood pressure), and each reading is evaluated against a normal and a critical reference range. Readings outside those ranges raise clinical alerts that someone has to acknowledge.

The domain was chosen deliberately: it connects to the author's professional experience with healthcare operator/provider systems, while generating naturally rich telemetry data.

**Stack:** .NET 10 (LTS) · C# · ASP.NET Core (Controllers) · PostgreSQL · Entity Framework Core · Docker Compose · OpenTelemetry · Prometheus · Grafana · xUnit · Testcontainers

## Architecture

Clean Architecture, four layers, dependencies pointing inward only:

```
VitaSignal.Domain          → no dependencies
VitaSignal.Application     → depends on Domain
VitaSignal.Infrastructure  → depends on Application
VitaSignal.Api             → depends on Infrastructure (composition root)

VitaSignal.DeviceSimulator → talks to the API over HTTP only
```

- **Domain** — `Patient`, `VitalReading`, `VitalAlert` and the range catalogs. Entities validate themselves at creation and throw `DomainValidationException` when a rule is broken. No framework dependencies.
- **Application** — use cases (register patient, register reading, list readings, list and acknowledge alerts), repository interfaces, `IUnitOfWork`, cursor pagination and the business metrics/spans.
- **Infrastructure** — EF Core `DbContext`, entity configurations, migrations and repository implementations against PostgreSQL.
- **Api** — controllers, response contracts, the exception-to-HTTP mapping, health checks and OpenTelemetry wiring.
- **DeviceSimulator** — a worker that behaves like a fleet of bedside monitors (see [Device simulator](#device-simulator)).

### Design decisions

- **Three ranges per vital sign.** *Plausible* is what a device can physically measure (a SpO2 of 250% is a broken payload, so it is rejected with 400). *Normal* is the clinical reference range; outside it the reading is stored and a **Warning** alert is raised. *Critical* is the range where someone must act now; outside it the alert is **Critical**.
- **Readings and alerts are saved in the same transaction.** Repositories only stage changes; the use case commits once through `IUnitOfWork`, so a reading is never stored without the alert it should have raised.
- **Errors are explicit.** Domain rules throw `DomainValidationException`, missing request data throws `InvalidRequestException`, missing resources throw a `NotFoundException`. A single handler turns them into RFC 7807 problem details (400/404). Anything else is a 500 with a generic message, logged with the full exception, and every error response carries a `traceId`.
- **Timestamps must carry a timezone.** `2026-10-01T10:00:00Z` and `2026-10-01T07:00:00-03:00` are accepted and stored in UTC; a timestamp without offset is rejected, because there is no way to know when that reading happened.
- **Keyset (cursor) pagination** for readings and alerts, ordered newest first. Offset pagination gets slower the deeper you go and skips/duplicates rows when new readings arrive between pages, which happens all the time with telemetry.
- **Business metrics and spans live in the Application layer**, right where the decision is made ("registered", "out of range", "rejected", "alert raised"). It only uses `System.Diagnostics` and `Microsoft.Extensions.Logging.Abstractions`, so no observability vendor leaks into the core.
- **PostgreSQL over SQL Server** — no licensing cost in any environment.
- **Migrations run on startup.** Fine for local development and a single instance. Before running more than one replica in Azure this should move to a migration bundle executed once per deploy.

## How to run

Requires Docker Desktop.

```bash
docker compose up --build
```

To also start the device simulator (continuous traffic for the dashboards):

```bash
docker compose --profile sim up --build
```

| Service | URL | Notes |
|---|---|---|
| API + interactive docs | http://localhost:8080/scalar/v1 | Development environment only |
| Health | http://localhost:8080/health/live, http://localhost:8080/health/ready | `ready` also checks the database |
| Metrics (Prometheus format) | http://localhost:8080/metrics | Scraped every 5s |
| Prometheus | http://localhost:9090 | |
| Grafana | http://localhost:3000 | `admin` / `admin` locally; dashboard *VitaSignal* is provisioned automatically |
| PostgreSQL | localhost:5432 | database `vitasignal` |

Credentials and simulator settings can be overridden with a `.env` file — copy `.env.example`. The defaults are for local development only, never real credentials.

Data persists across `docker compose down` / `up` via named volumes (`docker compose down -v` wipes everything).

To run the API from Visual Studio / `dotnet run` instead, start only the database with `docker compose up db`. The [VitaSignal.Api.http](VitaSignal.Api/VitaSignal.Api.http) file has ready-to-send requests for every endpoint.

### API overview

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/patients` | Registers a synthetic patient and returns its pseudonymous code |
| `GET` | `/api/patients/{id}` | Gets a patient |
| `GET` | `/api/patients/{id}/readings?type=&limit=&cursor=` | Readings, newest first (default 100, max 500 per page) |
| `GET` | `/api/patients/{id}/alerts?severity=&pendingOnly=&limit=&cursor=` | Clinical alerts, newest first |
| `POST` | `/api/vitalreadings` | Registers a reading; returns `201` with the alert it raised, if any |
| `GET` | `/api/vitalreadings/{id}` | Gets a reading |
| `POST` | `/api/alerts/{id}/acknowledge` | Acknowledges an alert (idempotent) |

### Device simulator

`VitaSignal.DeviceSimulator` registers a set of patients and sends one reading per vital sign every few seconds. Each patient has its own baseline and values drift smoothly instead of jumping randomly. From time to time a patient goes through an episode — tachycardia, fever, desaturation or hypertension — that builds up and fades away over several readings, which is what makes the out-of-range and alert panels move.

It only knows the public HTTP contract (no reference to Domain or Infrastructure), uses a standard resilience pipeline (retry, circuit breaker, timeout) and propagates the trace context, so a device tick and the API request it caused belong to the same trace.

| Setting | Default | |
|---|---|---|
| `SIMULATOR_PATIENTS` | 10 | |
| `SIMULATOR_INTERVAL_SECONDS` | 5 | Interval between readings of the same device |
| `SIMULATOR_ANOMALY_RATE` | 0.1 | Roughly the share of time patients spend in an episode |

### Running tests

```bash
dotnet test
```

- `VitaSignal.Domain.Tests` and `VitaSignal.Application.Tests` are plain unit tests.
- `VitaSignal.Api.IntegrationTests` boots the real API with `WebApplicationFactory` against a throwaway PostgreSQL started by Testcontainers, so **Docker must be running**. They cover the HTTP contract end to end: validation errors, pagination, alerts, the foreign keys and the metrics endpoint.

The build treats warnings as errors and enforces the code style in `.editorconfig`; `dotnet format --verify-no-changes` should pass before opening a PR.

## Observability

Every request produces a trace, metrics and structured logs, all correlated by `TraceId`.

**Business metrics** (exposed at `/metrics`):

| Metric | Type | Labels | Answers |
|---|---|---|---|
| `vitasignal_vital_readings_registered_total` | counter | `vital_sign_type` | How much telemetry are we ingesting? |
| `vitasignal_vital_readings_out_of_range_total` | counter | `vital_sign_type` | How many readings were outside the normal range? |
| `vitasignal_vital_readings_rejected_total` | counter | `reason`, `vital_sign_type` | Why are devices being refused (`patient_not_found`, `implausible_value`, `validation_failed`)? |
| `vitasignal_alerts_raised_total` | counter | `severity`, `vital_sign_type` | How many Warning / Critical alerts are we generating? |
| `vitasignal_vital_readings_registration_duration_seconds` | histogram | `vital_sign_type` | How long does it take to validate and store a reading? |

Labels never contain patient or device identifiers, which keeps cardinality (and Prometheus memory) bounded.

**Traces** cover the HTTP request, the use case (`RegisterVitalReading`, `AcknowledgeAlert`, …) and the SQL sent by Npgsql. Scrape and health check requests are not traced. Locally they can be printed with `Telemetry__ConsoleExporter=true`; setting `OTEL_EXPORTER_OTLP_ENDPOINT` sends traces, metrics and logs to any OTLP backend (Azure Monitor, Jaeger, Tempo…).

**Logs** are structured (source-generated `LoggerMessage`), and JSON outside the Development environment.

**Grafana** provisions the *VitaSignal* dashboard (API health on top, clinical telemetry below) and four alert rules, all as code under `grafana/`:

| Alert | Fires when |
|---|---|
| API scrape target down | Prometheus cannot reach `/metrics` for 1 minute |
| High 5xx error rate | More than 5% of requests fail with a server error for 5 minutes |
| Slow reading ingestion | p95 of `POST api/VitalReadings` above 500 ms for 5 minutes |
| Reading ingestion stopped | No reading accepted for 5 minutes |

No contact point is configured locally — alerts show up under *Alerting → Alert rules*. In a real deployment they would be routed to e-mail or Teams.

## Privacy by design

Even though all data here is synthetic, the system is designed as if it handled real patient data:

- **No names.** A patient is identified by a pseudonymous code generated by the platform (`PAC-3F9A1C02B7E4`). A telemetry platform does not need to know who the person is to evaluate a heart rate; the real identity would stay in the hospital's own registry and be linked by that code.
- **Nothing identifying leaves the API.** Logs, spans and metric labels carry only IDs and codes.
- **Synthetic by construction.** `Patient.IsSyntheticData` is always `true` and is returned by the API; the simulator is the only data source and only knows the public contract.
- **Every stored field has a reason.** `Unit` is stored with each reading even though it can be derived from the type today, so historical readings remain correct if a unit ever changes.
- **Retention.** Raw readings are meant to be kept for 90 days; that purge job is not implemented in this version.

Handling real data would additionally require consent management, encryption at rest with managed keys, access auditing and the retention job above — intentionally out of scope.

## Costs and trade-offs

> **Status: not yet completed.** This section will be filled in once the project is deployed to Azure, with a real estimated monthly cost and the trade-offs behind the infrastructure choices (container hosting option, database tier, observability stack overhead). Tracked as a later step in the project roadmap.

What's already decided with a cost rationale in mind:
- **PostgreSQL over SQL Server** — no licensing cost, in any environment.
- **Index on `(PatientId, RecordedAtUtc, Id)` + cursor pagination** — reading history grows linearly with every device; without them each query scans the whole table and the database tier has to grow much earlier.
- **Console exporters are off by default.** Printing every span and metric in production turns into log ingestion that is billed per GB and adds no value.
- **Bounded database retries** (3 attempts, up to 5s) — a database outage surfaces as a fast 500 and a firing alert instead of requests piling up for minutes.
- **Multi-stage Docker build, non-root runtime image and a tight `.dockerignore`** — only the ASP.NET Core runtime ships, keeping image size (registry storage and deploy time) down and the build cache effective.
- **Prometheus pull locally, OTLP ready for the cloud** — no paid backend is needed to develop, and switching to a managed one is a configuration change.
