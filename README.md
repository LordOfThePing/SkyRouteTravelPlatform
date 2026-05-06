# SkyRoute — Flight Search & Booking

Senior full-stack technical assessment. Angular 17 + .NET 8 implementation of SkyRoute's Flight Search & Booking module, aggregating two mocked airline providers (**GlobalAir**, **BudgetWings**) behind an extensible provider interface.

---

## 1. Challenge summary and scope

**In scope** (per challenge brief):

- Flight search across **two mocked backend providers** with realistic results.
- Provider-specific pricing rules:
  - **GlobalAir:** `final = base × 1.15` (15 % fuel surcharge), rounded to 2 decimals.
  - **BudgetWings:** `final = max(base × 0.90, 29.99)` (10 % promo on base; USD 29.99 floor).
- Search form: origin, destination, departure date, passengers (1–9), cabin class (Economy / Business / First Class).
- Hardcoded airports — at least 6 across at least 2 countries.
- Results display with **total price as primary** and per-passenger as secondary.
- Frontend-only sorting (price ↑/↓, duration ↑, departure time).
- Loading and empty states.
- Booking flow with flight summary, price breakdown, passenger form, and a generated booking reference.
- **Dynamic document field**: `Passport Number` for international routes, `National ID` for domestic — both label and validation switch by route.
- Local-only deployment, public git repo, README with setup, architecture, trade-offs.

**Out of scope** (explicitly):

- Authentication / user accounts (documented as next step).
- Real airline integrations.
- Persistence beyond in-memory store.
- Cloud deployment and CI/CD.
- Payment processing.
- Round-trip / multi-city itineraries (one-way only — brief implies single departure date).

---

## 2. Architecture overview

```
┌─────────────────────────┐     HTTPS/JSON      ┌───────────────────────────────────┐
│   Angular 17 SPA        │ ──────────────────▶ │   ASP.NET Core 8 Web API          │
│   (standalone comps,    │                     │   ┌─────────────────────────────┐ │
│    Reactive Forms,      │ ◀────────────────── │   │  Controllers / Minimal APIs │ │
│    RxJS, Material)      │     ProblemDetails  │   ├─────────────────────────────┤ │
└─────────────────────────┘                     │   │  Application services       │ │
        │                                       │   │  • FlightAggregator         │ │
        │                                       │   │  • BookingService           │ │
        │ Routes:                               │   ├─────────────────────────────┤ │
        │ /search   → SearchPage                │   │  Domain                     │ │
        │ /booking  → BookingPage               │   │  • IFlightProvider          │ │
        │ /confirm  → ConfirmationPage          │   │  • Pricing strategies       │ │
        │                                       │   ├─────────────────────────────┤ │
        │                                       │   │  Infrastructure             │ │
        │                                       │   │  • GlobalAirProvider (mock) │ │
        │                                       │   │  • BudgetWingsProvider(mock)│ │
        │                                       │   │  • EfCoreBookingRepository  │ │
        │                                       │   │    └─ SqliteDbContext       │ │
        │                                       │   │       └─ skyroute.db (file) │ │
        │                                       │   └─────────────────────────────┘ │
        │                                       └───────────────────────────────────┘
```

**Data flow — search**

1. User submits form → Angular validates client-side.
2. `FlightSearchService` POSTs to `/api/flights/search`.
3. API validates via FluentValidation; rejects on 400 with ProblemDetails.
4. `FlightAggregator` fans out to all registered `IFlightProvider`s in parallel.
5. Each provider returns flights with **base fares**; its `IPricingStrategy` produces final per-passenger fares.
6. Aggregator computes `total = perPassenger × passengers`, returns a unified DTO list.
7. Angular renders results; subsequent re-sorts are local.

**Data flow — booking**

1. User selects a flight → Angular passes flight model via router state to `/booking`.
2. Booking page derives `isInternational` from `originAirport.country !== destinationAirport.country`.
3. Form renders a `FormArray` of `FormGroup`s — one per passenger from the search — and swaps each row's document label and validator dynamically.
4. POST `/api/bookings` → `BookingService` validates the flight reference and passenger count, persists `Booking` + child `Passenger` rows via EF Core into `skyroute.db`, returns `{ bookingReference: "SR-XXXXXX" }`.
5. Confirmation page displays the reference and the persisted passenger list.

---

## 3. Frameworks and technologies

### Frontend

| Tech                       | Why                                                                                  |
| -------------------------- | ------------------------------------------------------------------------------------ |
| Angular 17 (standalone)    | Mandated stack; standalone components reduce boilerplate for a small app.            |
| TypeScript (strict)        | Type-safe DTO contracts mirroring backend models.                                    |
| Reactive Forms + FormArray | Required for dynamic validators (document field switching) and N passenger forms.    |
| RxJS                       | Idiomatic for HTTP + sort state streams.                                             |
| Tailwind CSS               | Utility-first styling; faster iteration than maintaining a component-library theme.  |
| Headless UI                | Accessible, unstyled primitives (dialogs, listboxes) that pair cleanly with Tailwind.|
| Karma + Jasmine            | Default Angular test stack; minimal configuration.                                   |

### Backend

| Tech                          | Why                                                                              |
| ----------------------------- | -------------------------------------------------------------------------------- |
| .NET 8 LTS                    | Mandated stack; latest LTS.                                                      |
| ASP.NET Core Web API          | Standard for REST surface.                                                       |
| EF Core 8                     | Code-first persistence; clean repository implementation.                         |
| SQLite                        | Zero-config file-based DB; perfect for local-only deliverable; swappable later. |
| FluentValidation              | Cleaner, testable validation rules vs. data annotations.                         |
| Serilog (planned)             | Structured logs with request correlation; preferred over default `ILogger` sink.|
| Built-in Rate Limiter         | First-party, no external dependency.                                             |
| xUnit + FluentAssertions      | Industry standard test stack with readable assertions.                           |
| Swashbuckle / OpenAPI         | Auto-generated API docs for the demo walkthrough.                                |

### Tooling

- **EditorConfig** + Prettier + ESLint + `dotnet format` for consistent style.
- **Git** with conventional commits.

---

## 4. API design overview

Base URL (local): `http://localhost:5080/api`

### `POST /api/flights/search`

Request:

```json
{
  "originCode": "MAD",
  "destinationCode": "JFK",
  "departureDate": "2026-06-15",
  "passengers": 2,
  "cabinClass": "Business"
}
```

Response `200 OK`:

```json
{
  "results": [
    {
      "id": "GA-1471",
      "provider": "GlobalAir",
      "flightNumber": "GA1471",
      "originCode": "MAD",
      "destinationCode": "JFK",
      "departureTime": "2026-06-15T09:30:00Z",
      "arrivalTime": "2026-06-15T17:55:00Z",
      "durationMinutes": 505,
      "cabinClass": "Business",
      "pricePerPassenger": 1265.50,
      "totalPrice": 2531.00,
      "currency": "USD",
      "passengers": 2
    }
  ]
}
```

### `POST /api/bookings`

Request — note the `passengers` **array**; its length must equal the original search's passenger count.

```json
{
  "flightId": "GA-1471",
  "passengers": [
    {
      "fullName": "Ada Lovelace",
      "email": "ada@example.com",
      "documentType": "Passport",
      "documentNumber": "X1234567"
    },
    {
      "fullName": "Alan Turing",
      "email": "alan@example.com",
      "documentType": "Passport",
      "documentNumber": "Y9876543"
    }
  ],
  "totalPrice": 2531.00
}
```

Response `201 Created`:

```json
{
  "bookingReference": "SR-7K2QA9",
  "createdAt": "2026-05-06T12:34:56Z",
  "passengerCount": 2
}
```

### `GET /api/airports`

Returns the hardcoded airport catalogue (code, name, city, country). Used to populate dropdowns and to drive domestic/international detection.

### Error model — RFC 7807 ProblemDetails

```json
{
  "type": "https://skyroute.local/errors/validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "passengers": ["Must be between 1 and 9."],
    "departureDate": ["Cannot be in the past."]
  },
  "traceId": "00-ab12...-01"
}
```

`400` for validation, `404` for unknown flight reference, `429` for rate-limit, `500` for unexpected (no stack trace leaked).

---

## 5. Security in API

Even though the brief does not mandate auth, the API surface is hardened defensively at the boundary:

- **Input validation** — FluentValidation enforces:
  - Airport codes match `^[A-Z]{3}$` and exist in the airport catalogue.
  - `passengers` ∈ `[1, 9]`.
  - `cabinClass` ∈ allowed enum.
  - `departureDate` is today or later.
  - Origin ≠ Destination.
  - `documentNumber` matches passport or national-ID regex by claim.
- **Sanitization** — all string inputs trimmed; length caps applied (`fullName ≤ 100`, `email ≤ 254`, `documentNumber ≤ 32`); enum values whitelisted; no string is interpolated into a query (no DB in this challenge, but contract enforced).
- **Safe error handling** — global exception middleware returns ProblemDetails with `traceId` only; no stack traces, no inner-exception messages exposed.
- **Abuse / rate limiting** — ASP.NET Core fixed-window rate limiter, e.g. `60 req/min/IP` on `/api/flights/search`, `20 req/min/IP` on `/api/bookings`. Returns `429` with `Retry-After`.
- **Sensitive data handling** — passenger PII (name, email, document) is logged only by category, never by value (`logger.LogInformation("Booking created for flight {FlightId}", id)`); document numbers never round-trip back to the client after submission; no PII in error messages or `traceId`.
- **CORS** — locked to the local Angular origin in development; documented to be tightened to the production origin when deployed.
- **HTTPS** — enforced via `UseHttpsRedirection` (HSTS in non-Development).
- **Auth/Authz** — **out of scope for this challenge**. Designed-in seam: all controllers accept `[Authorize]` cleanly when an auth scheme (JWT bearer + an identity provider) is added. Booking endpoint would become user-scoped, and a `userId` would be attached to bookings.

---

## 6. Scalability approach

- **Stateless API** — no server-side session. Bookings persisted via `IBookingRepository` (EF Core + SQLite today; the same `DbContext` works against Postgres or SQL Server with one provider switch). Horizontal scale-out behind a load balancer is the default direction once the DB is moved off SQLite.
- **Provider extensibility** — new providers register against `IFlightProvider`. The aggregator has no compile-time coupling to any provider. Adding a third provider = one class + one DI registration line.
- **Parallel fan-out** — `FlightAggregator` calls all providers concurrently with `Task.WhenAll`, with per-provider timeouts and a `try/catch` boundary so one slow/failing provider does not poison the response (graceful degradation).
- **Caching opportunities** — `IMemoryCache` keyed by `(origin, destination, date, cabin, passengers)` with short TTL (e.g. 60 s) for repeated identical searches; for production, replace with a distributed cache (Redis). Documented but not implemented in challenge scope.
- **Async / background opportunities** — booking confirmations could be emailed via a background queue (Hangfire / Hosted Service / Service Bus). Not implemented; called out as a seam.
- **Observability direction**:
  - **Logs** — Serilog structured logs, request correlation ID propagated through `traceId`.
  - **Metrics** — `System.Diagnostics.Metrics` for `flights.search.duration`, `flights.search.providerFailures`, `bookings.created`.
  - **Tracing** — OpenTelemetry exporter ready (commented config) for distributed tracing across providers.

---

## 7. SOLID application

- **S — Single Responsibility.** Each class has one axis of change:
  - `GlobalAirProvider` only fetches GlobalAir flights.
  - `GlobalAirPricingStrategy` only computes GlobalAir's price.
  - `FlightAggregator` only coordinates fan-out.
  - `BookingService` only orchestrates booking creation.
  - `EfCoreBookingRepository` only handles persistence.
- **O — Open/Closed.** Adding **BudgetWings** (and any future provider) does **not modify** `FlightAggregator`. It implements `IFlightProvider` and is added to DI. Pricing rules live in `IPricingStrategy` implementations — adding a "WeekendDiscount" strategy is additive.
- **L — Liskov Substitution.** All `IFlightProvider` implementations honor the same contract (return `IReadOnlyList<FlightOffer>`, never `null`, throw only on infrastructure failure). The aggregator can substitute any implementation transparently. Pricing strategies and `IBookingRepository` implementations likewise — `EfCoreBookingRepository` could be replaced by an in-memory fake in tests with no behavioral surprises.
- **I — Interface Segregation.** Interfaces are narrow: `IFlightProvider.SearchAsync(...)`, `IPricingStrategy.PriceFor(decimal baseFare)`, `IBookingRepository.SaveAsync(...)`. No god-interface forces clients to depend on methods they don't use.
- **D — Dependency Inversion.** High-level `FlightAggregator` and `BookingService` depend on the abstractions (`IFlightProvider`, `IBookingRepository`), not on concrete `GlobalAirProvider` / `EfCoreBookingRepository`. Wiring happens in `Program.cs` via the built-in DI container, making each component independently testable.

---

## 8. Functional behavior — requirement mapping

| Requirement                                                        | Where it lives                                                                          |
| ------------------------------------------------------------------ | --------------------------------------------------------------------------------------- |
| Search fields: origin, destination, date, passengers (1–9), cabin  | `SearchFormComponent` (Angular Reactive Form) + `SearchRequestValidator` (.NET).        |
| ≥ 6 hardcoded airports, ≥ 2 countries                              | `Airports.json` / `AirportCatalog` static seed (e.g. MAD, BCN, AGP / JFK, LAX, MIA).    |
| GlobalAir pricing (`base × 1.15`)                                  | `GlobalAirPricingStrategy.PriceFor` — unit-tested.                                      |
| BudgetWings pricing (`max(base × 0.90, 29.99)`)                    | `BudgetWingsPricingStrategy.PriceFor` — unit-tested with floor case.                    |
| Total price primary, per-passenger secondary                       | `FlightResultRowComponent` template; CSS hierarchy emphasises `total-price`.            |
| Frontend-only sorting (4 modes)                                    | `SortPipe` / `SortService`; never re-queries API.                                       |
| Loading indicator                                                  | Search service exposes `isSearching$`; `MatProgressSpinner` bound in template.          |
| Empty state                                                        | `EmptyResultsComponent` shown when results array is empty post-search.                  |
| Booking flow: summary, breakdown, passenger forms, confirm         | `BookingPageComponent` + `FormArray` of passenger forms + `POST /api/bookings`.         |
| One form per passenger (N from search)                             | `FormArray` size driven by `passengers` query param; `Passenger N of M` headers.        |
| Booking reference returned                                         | `BookingService` generates `SR-XXXXXX`, persists via EF Core; `ConfirmationPageComponent` displays it. |
| Dynamic document: Passport (intl) vs National ID (domestic)        | `DocumentFieldComponent` reacts to `route.isInternational` → swaps label + validator on every passenger row. |
| Same-country detection                                             | `AirportCatalog.GetCountry(code)` consumed by frontend booking page logic.              |

---

## 9. Setup and run instructions (local)

### Prerequisites

- Node.js ≥ 20 and npm
- .NET 8 SDK
- Git

### Backend

```bash
cd backend
dotnet restore
# Apply EF Core migrations (creates skyroute.db); also runs automatically on startup.
dotnet ef database update --project src/SkyRoute.Infrastructure --startup-project src/SkyRoute.Api
dotnet run --project src/SkyRoute.Api
# API on http://localhost:5080
# Swagger on http://localhost:5080/swagger
# SQLite file at backend/src/SkyRoute.Api/skyroute.db
```

> If you don't have the EF Core CLI installed: `dotnet tool install --global dotnet-ef`.

### Frontend

```bash
cd frontend
npm install
npm start
# App on http://localhost:4200
```

### Demo script (45–60 min walkthrough)

1. Open Swagger, hit `/api/airports` to show the catalogue.
2. Open `http://localhost:4200`, search **MAD → BCN, 2 pax, Economy**. Show results from both providers, point out per-passenger vs total.
3. Sort by price ascending, then by duration. Note: no network request fires.
4. Try **MAD → MAD** to show validation error.
5. Try a route the mock returns no flights for to show empty state.
6. Click an MAD → BCN result → booking page shows **National ID** label + validator.
7. Search **MAD → JFK** instead → booking page shows **Passport Number** label + validator.
8. Submit the form → confirmation screen shows `SR-XXXXXX`.
9. Walk through `IFlightProvider`, `IPricingStrategy`, `FlightAggregator` to demonstrate SOLID + extensibility.

### Tests

```bash
# Backend
cd backend && dotnet test

# Frontend
cd frontend && npm test -- --watch=false
```

---

## 10. Trade-offs, known limitations, and next steps

### Conscious trade-offs

- **SQLite, not Postgres.** File-based, zero-config, ideal for a local deliverable. Same EF Core code targets Postgres / SQL Server with a one-line provider change.
- **No auth.** Bookings are anonymous and globally enumerable in principle. The seam is in place (`[Authorize]` + user-scoping the repository).
- **One-way only.** The brief's single departure date implies one-way; no round-trip / multi-city.
- **Validation duplicated FE/BE.** Necessary for UX (instant feedback) and security (never trust client). Documented, not deduplicated.
- **Mocked providers are deterministic.** Same query returns same flights — desirable for demo reproducibility, but not realistic. A real integration would use circuit breakers, retries, and provider-side caching.
- **No pagination / advanced filters.** Mock returns ~5–8 flights per provider, so unnecessary at this scale.
- **Tailwind utility classes in templates.** Higher template noise than a component library, traded for faster iteration and zero theming overhead.

### Known limitations

- No internationalisation (English only).
- Accessibility pass is best-effort via Material defaults; no formal WCAG audit.
- Rate limiting not yet enabled by default — config sample provided in `appsettings.Development.json`.
- No CI pipeline.

### What I would do next

1. Add JWT auth + per-user booking scoping.
2. Swap SQLite for Postgres in production (`UseNpgsql`); keep SQLite for local dev.
3. Add OpenTelemetry exporter and a pre-built Grafana dashboard config.
4. Add Hangfire queue for confirmation emails.
5. Add Playwright e2e: search → sort → book domestic; search → book international.
6. Containerise via Docker Compose (api + web + Postgres).
7. Replace deterministic mocks with a recorded-fixture replay layer that simulates latency and intermittent provider failures, to demonstrate resilience patterns.
8. Extract a small Tailwind-based component library (button, input, dialog) once duplication justifies it.

### Unfinished work (if any at submission)

This section is updated at the end of the challenge if any in-scope item slipped. As of writing, every Must-have in [TODO.md](TODO.md) is targeted for completion within the budget.
