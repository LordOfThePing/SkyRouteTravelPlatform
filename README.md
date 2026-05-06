# SkyRoute — Flight Search & Booking

Senior full-stack technical assessment. Angular 17 + .NET 8 implementation of SkyRoute's Flight Search & Booking module, aggregating two mocked airline providers (**GlobalAir**, **BudgetWings**) behind an extensible provider interface.

## Status

| Area                                              | Status |
| ------------------------------------------------- | :----: |
| Backend solution + EF Core + SQLite + migrations  | ✅ |
| Provider abstraction, GlobalAir, BudgetWings      | ✅ |
| Pricing strategies (15 % surcharge / 10 % + floor) | ✅ |
| `GET /api/flights/search`, `POST /api/bookings`, `GET /api/airports`, `GET /health` | ✅ |
| FluentValidation (incl. passport / national-ID format), ProblemDetails, exception middleware, rate limiter attached, CORS | ✅ |
| Angular search form, results table, 4 client-side sorts, loading + empty states | ✅ |
| Booking flow with `FormArray` (one form per passenger) and dynamic `Passport`/`National ID` field | ✅ |
| Confirmation page (reference + persisted passenger summary)        | ✅ |
| Backend tests (30) and frontend tests (11), all passing             | ✅ |
| `make build` / `make test-all` work end-to-end                      | ✅ |

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
- Persistence beyond local SQLite storage.
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
│    RxJS, Tailwind CSS)  │     ProblemDetails  │   ├─────────────────────────────┤ │
└─────────────────────────┘                     │   │  Application services       │ │
        │                                       │   │  • FlightAggregator         │ │
        │                                       │   │  • Booking API flow         │ │
        │ Routes:                               │   ├─────────────────────────────┤ │
        │ /search   → SearchPage                │   │  Domain                     │ │
        │ /booking  → BookingPage               │   │  • IFlightProvider          │ │
        │ /confirmation → ConfirmationPage      │   │  • Pricing strategies       │ │
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
2. `ApiService` issues `GET /api/flights/search?originCode=...&...` with the form values as query parameters.
3. API validates via FluentValidation; rejects on 400 with ProblemDetails.
4. `FlightAggregator` fans out to all registered `IFlightProvider`s in parallel.
5. Each provider returns flights with **base fares**; its `IPricingStrategy` produces final per-passenger fares.
6. Aggregator computes `total = perPassenger × passengers`, returns a unified DTO list.
7. Angular renders results; subsequent re-sorts are local.

**Data flow — booking**

1. User selects a flight → Angular passes flight model via router state to `/booking`.
2. Booking page derives `isInternational` from `originAirport.country !== destinationAirport.country`.
3. Form renders a `FormArray` of `FormGroup`s — one per passenger from the search — and swaps each row's document label and validator dynamically.
4. POST `/api/bookings` → API validates payload, persists `Booking` + child `Passenger` rows via EF Core into SQLite, returns `{ bookingReference: "SR-XXXXXX" }`.
5. Confirmation page displays the reference and the persisted passenger list.

---

## 3. Frameworks and technologies

### Frontend

| Tech                       | Why                                                                                  |
| -------------------------- | ------------------------------------------------------------------------------------ |
| Angular 17 (standalone)    | Mandated stack; standalone components reduce boilerplate for a small app.            |
| TypeScript                 | Type-safe DTO contracts mirroring backend models.                                    |
| Reactive Forms + FormArray | Required for dynamic validators (document field switching) and N passenger forms.    |
| Signals + computed         | Local reactive state for offers, sort, validation flags.                             |
| RxJS                       | HTTP, finalize-on-error, interceptor mapping ProblemDetails → user-facing message.   |
| Tailwind CSS               | Utility-first styling; faster iteration than maintaining a component-library theme.  |
| Angular CDK                | Available for accessible primitives if dialogs/listboxes are added later.            |
| Karma + Jasmine            | Default Angular test stack; minimal configuration.                                   |

### Backend

| Tech                          | Why                                                                              |
| ----------------------------- | -------------------------------------------------------------------------------- |
| .NET 8 LTS                    | Mandated stack; latest LTS.                                                      |
| ASP.NET Core Web API          | Standard for REST surface.                                                       |
| EF Core 8 + SQLite            | Code-first persistence; file-based DB for local-only deliverable; swappable later via provider change. |
| FluentValidation              | Cleaner, testable validation rules vs. data annotations.                         |
| Built-in Rate Limiter         | First-party, no external dependency.                                             |
| Default `ILogger` (Serilog planned) | Structured logs are a planned next step; using Microsoft default for now.   |
| xUnit + FluentAssertions      | Readable assertion syntax over plain xUnit.                                      |
| Swashbuckle / OpenAPI         | Auto-generated API docs for the demo walkthrough.                                |

### Tooling

- **Makefile** wrapping `dotnet`, `npm`, and `dotnet ef` commands (`make build`, `make test-all`, `make migrate`, etc.).
- **Git** for source control and reviewable sprint checkpoints. See [TODO.md](TODO.md) for the sprint timeline.

---

## 4. API design overview

Base URL (local): `http://localhost:5080/api`

### `GET /api/flights/search`

Search is a side-effect-free, idempotent read, so it uses **GET with query parameters**. This is REST-correct, browser-friendly (the URL can be bookmarked or pasted), and lets HTTP-level caching front the endpoint when needed. POST would only become preferable if the criteria grew into nested arrays (multi-leg, multiple cabin preferences, flexible-date matrices) — explicitly out of scope here.

Rate-limited at **60 requests / minute / IP** via `[EnableRateLimiting("search")]`.

Example:

```
GET /api/flights/search
    ?originCode=MAD
    &destinationCode=JFK
    &departureDate=2026-06-15
    &passengers=2
    &cabinClass=Business
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

Booking creates server-side state (a persisted record), so it uses POST. Rate-limited at **20 requests / minute / IP** via `[EnableRateLimiting("booking")]`. Request — note the `passengers` **array**; one entry per passenger collected on the booking page.

```json
{
  "flightId": "GA-1471",
  "originCode": "MAD",
  "destinationCode": "JFK",
  "cabinClass": "Business",
  "totalPrice": 2531.00,
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
  ]
}
```

`documentType` is one of `Passport` (international route) or `NationalId` (domestic route). The frontend chooses the value based on origin/destination country comparison; the API accepts both, validates the enum, and persists the booking + child passengers in a single transaction.

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

- **Input validation (implemented)** — FluentValidation enforces:
  - Airport codes match `^[A-Z]{3}$` and exist in the `AirportCatalog`.
  - `passengers` ∈ `[1, 9]`.
  - `cabinClass` ∈ {`Economy`, `Business`, `First Class`}.
  - `departureDate` ≥ today (UTC).
  - Origin ≠ destination.
  - Booking: `flightId`, `totalPrice > 0`, `passengers` non-empty; per-passenger `fullName`, `email` (RFC-style), `documentType` ∈ {`Passport`, `NationalId`}, `documentNumber`.
  - **Document format**: `documentNumber` must match `^[A-Za-z0-9]{6,12}$` for Passport and `^[A-Za-z0-9]{6,14}$` for NationalId. Mirrors the frontend regex; covered by 13 unit tests.
- **Sanitization (implemented)** — names trimmed, emails lower-cased, document numbers trimmed and upper-cased before persistence; length caps applied (`fullName ≤ 100`, `email ≤ 254`, `documentNumber ≤ 32`); enum values whitelisted; EF Core parameterizes all SQL.
- **Safe error handling (implemented)** — `ExceptionMiddleware` catches all unhandled exceptions and returns ProblemDetails with only `traceId`; no stack traces or inner exception messages exposed. Validation failures return RFC 7807 `ValidationProblemDetails` with field-level messages. The Angular `apiErrorInterceptor` reads `errors`/`detail`/`title` and surfaces them to the user.
- **Abuse / rate limiting (implemented)** — ASP.NET Core fixed-window rate limiter with two policies: `60 req/min` for search (`[EnableRateLimiting("search")]` on `FlightsController.Search`) and `20 req/min` for booking (`[EnableRateLimiting("booking")]` on `BookingsController.Create`). Returns `429 Too Many Requests` when exceeded.
- **CORS (implemented)** — locked to `http://localhost:4200` in development; tighten to the production origin when deployed.
- **HTTPS (implemented)** — `UseHttpsRedirection` is active (HSTS would be added when running outside Development).
- **Sensitive data handling (planned)** — passenger PII (name, email, document) is currently not logged at all; structured logging with PII-safe scopes is a planned step (Serilog, see §10).
- **Document format (FE + BE)** — passport/national-ID regex enforced both in the Angular form (`/^[A-Z0-9]{6,12}$/` passport, `/^[A-Z0-9]{6,14}$/` national ID) and on the server inside `PassengerValidator`. Frontend gives instant feedback; backend rejects bypass attempts.
- **Auth/Authz** — **out of scope for this challenge**. Designed-in seam: controllers accept `[Authorize]` cleanly once an auth scheme is added; bookings would become user-scoped via a `userId` column.

---

## 6. Scalability approach

- **Stateless API (implemented)** — no server-side session. Bookings persisted via `IBookingRepository` (EF Core + SQLite today; same `DbContext` works against Postgres or SQL Server with a one-line provider change). Horizontal scale-out behind a load balancer becomes feasible once the DB is moved off SQLite.
- **Provider extensibility (implemented)** — new providers register against `IFlightProvider`. The aggregator has no compile-time coupling to any provider. Adding a third provider = one class + one `services.AddSingleton<IFlightProvider, ...>()` line in [InfrastructureServiceExtensions.cs](backend/src/SkyRoute.Infrastructure/Extensions/InfrastructureServiceExtensions.cs).
- **Parallel fan-out + isolation (implemented)** — `FlightAggregator` calls all providers concurrently with `Task.WhenAll` and wraps each in a `try/catch` so one failing provider does not poison the response (covered by `FlightAggregatorTests.SearchAsync_WhenOneProviderFails_StillReturnsOtherResults`). Per-provider timeouts are a documented next step.
- **Caching opportunities (planned)** — `IMemoryCache` keyed by `(origin, destination, date, cabin, passengers)` with short TTL (~60 s) for repeated identical searches; replace with Redis for distributed deployments.
- **Async / background opportunities (planned)** — booking confirmations could be emailed via a background queue (Hangfire / Hosted Service / Service Bus).
- **Observability direction (planned)**:
  - **Logs** — Serilog structured logs with request correlation IDs.
  - **Metrics** — `System.Diagnostics.Metrics` for `flights.search.duration`, `flights.search.providerFailures`, `bookings.created`.
  - **Tracing** — OpenTelemetry exporter for distributed tracing across providers.

---

## 7. SOLID application

- **S — Single Responsibility.** Each class has one axis of change:
  - `GlobalAirProvider` only fetches GlobalAir flights.
  - `GlobalAirPricingStrategy` only computes GlobalAir's price.
  - `FlightAggregator` only coordinates fan-out.
  - `BookingsController` handles booking request/response orchestration at API boundary.
  - `EfCoreBookingRepository` only handles persistence.
- **O — Open/Closed.** Adding **BudgetWings** (and any future provider) does **not modify** `FlightAggregator`. It implements `IFlightProvider` and is added to DI. Pricing rules live in `IPricingStrategy` implementations — adding a "WeekendDiscount" strategy is additive.
- **L — Liskov Substitution.** All `IFlightProvider` implementations honor the same contract (return `IReadOnlyList<FlightOffer>`, never `null`, throw only on infrastructure failure). The aggregator can substitute any implementation transparently. Pricing strategies and `IBookingRepository` implementations likewise — `EfCoreBookingRepository` could be replaced by an in-memory fake in tests with no behavioral surprises.
- **I — Interface Segregation.** Interfaces are narrow: `IFlightProvider.SearchAsync(...)`, `IPricingStrategy.PriceFor(decimal baseFare)`, `IBookingRepository.SaveAsync(...)`. No god-interface forces clients to depend on methods they don't use.
- **D — Dependency Inversion.** High-level flight aggregation and persistence flows depend on abstractions (`IFlightProvider`, `IBookingRepository`), not concrete implementations (`GlobalAirProvider`, `EfCoreBookingRepository`). Wiring happens in `Program.cs` via DI, making components independently testable.

---

## 8. Functional behavior — requirement mapping

| Requirement                                                        | Where it lives                                                                          |
| ------------------------------------------------------------------ | --------------------------------------------------------------------------------------- |
| Search fields: origin, destination, date, passengers (1–9), cabin  | `search-page.component.ts` (Reactive Forms) + `SearchRequestValidator` (.NET).        |
| ≥ 6 hardcoded airports, ≥ 2 countries                              | `AirportCatalog` static seed (e.g. MAD, BCN, AGP / JFK, LAX, MIA).    |
| GlobalAir pricing (`base × 1.15`)                                  | `GlobalAirPricingStrategy.PriceFor` — unit-tested.                                      |
| BudgetWings pricing (`max(base × 0.90, 29.99)`)                    | `BudgetWingsPricingStrategy.PriceFor` — unit-tested with floor case.                    |
| Total price primary, per-passenger secondary                       | `search-page.component.ts` results table (total emphasized with stronger typography).            |
| Frontend-only sorting (4 modes)                                    | Client-side computed sort in `search-page.component.ts`; never re-queries API.                                       |
| Loading indicator                                                  | Search page binds loading state to inline spinner card and submit button state.          |
| Empty state                                                        | Search page shows empty-state card when a search returns zero results.                  |
| Booking flow: summary, breakdown, passenger forms, confirm         | `BookingPageComponent` + `FormArray` of passenger forms + `POST /api/bookings`.         |
| One form per passenger (N from search)                             | `FormArray` size driven by selected flight `passengers`; `Passenger N of M` headers.        |
| Booking reference returned                                         | `BookingsController` generates `SR-XXXXXX`, persists via EF Core; `confirmation-page.component.ts` displays it. |
| Dynamic document: Passport (intl) vs National ID (domestic)        | `booking-page.component.ts` derives route type and swaps label + validator on each passenger row. |
| Same-country detection                                             | Frontend compares airport countries loaded from `GET /api/airports`.              |

---

## 9. Setup and run instructions (local)

### Prerequisites

- **Node.js ≥ 20** and npm
- **.NET 8 SDK** (`dotnet --list-sdks` should include `8.0.x`)
- **dotnet-ef** global tool: `dotnet tool install --global dotnet-ef`
- **Git**
- _Optional but recommended_: **GNU Make** (`choco install make` on Windows, preinstalled elsewhere)

### Quick start — with Make

```bash
# In two terminals from the repo root:
make backend-run     # API on http://localhost:5080 (auto-migrates skyroute.db on first run)
make frontend-run    # App on http://localhost:4200, opens browser
```

Other helpful targets: `make build`, `make test-all`, `make migrate`, `make clean`. Run `make help` for the full list.

### Manual — without Make

**Backend**

```bash
cd backend
dotnet restore
# Migration runs automatically on startup, but you can apply it explicitly:
dotnet ef database update --project src/SkyRoute.Infrastructure --startup-project src/SkyRoute.Api
dotnet run --project src/SkyRoute.Api --launch-profile http
# API:     http://localhost:5080
# Swagger: http://localhost:5080/swagger
# SQLite:  backend/data/skyroute.db (auto-created outside the source tree)
```

**Frontend**

```bash
cd frontend
npm install
npm start
# App on http://localhost:4200
```

### Demo script (45–60 min walkthrough)

1. Open Swagger (`http://localhost:5080/swagger`), hit `GET /api/airports` to show the 8-airport catalogue across 4 countries.
2. Open `http://localhost:4200`. Search **MAD → BCN, 2 pax, Economy**. Show results from both providers, highlight **total price** prominence vs per-passenger as secondary.
3. Use the sort dropdown — price ↑/↓, duration ↑, departure ↑. Note: **no network request** fires (open DevTools to prove it).
4. Try **MAD → MAD** to show frontend-blocked error before any API call.
5. Click **Book** on a MAD → BCN result → booking page shows **National ID** label + validator on every passenger row (2 forms).
6. Go back, search **MAD → JFK**, click **Book** → booking page shows **Passport Number** label + validator.
7. Submit a valid passport-format value → confirmation screen shows `SR-XXXXXX` reference + persisted passenger summary.
8. (Optional) In Swagger, send a malformed `POST /api/flights/search` to demonstrate `ValidationProblemDetails`; stop the API to demonstrate the Angular interceptor's "Could not reach API" message.
9. Walk through `IFlightProvider`, `IPricingStrategy`, `FlightAggregator`, `EfCoreBookingRepository` to demonstrate SOLID + extensibility.

### Tests

```bash
# Full suite (recommended)
make test-all          # 17 backend + 11 frontend, all passing

# Backend only
cd backend && dotnet test

# Frontend only
cd frontend && npx ng test --watch=false --browsers=ChromeHeadless
```

---

## 10. Trade-offs, known limitations, and next steps

### Conscious trade-offs

- **`GET` for search, `POST` for booking.** Search is idempotent and side-effect-free, so it uses GET — REST-correct, bookmarkable, browser-cacheable. POST would be the right call only if criteria grew into nested arrays (multi-leg, complex fare rules, flexible-date matrices); for the current 5-field DTO, GET is cleaner. Booking creates server-side state, so it stays POST.
- **SQLite, not Postgres.** File-based, zero-config, ideal for a local deliverable. Same EF Core code targets Postgres / SQL Server with a one-line provider change. Database lives at `backend/data/skyroute.db` (outside the source tree, gitignored, auto-created on first run).
- **No auth.** Bookings are anonymous and globally enumerable by reference. The seam is in place (`[Authorize]` on controllers + user-scoping the repository).
- **One-way only.** The brief's single departure date implies one-way; no round-trip / multi-city.
- **Multi-passenger forms.** The brief says "a passenger details form" (singular); we render one form per passenger because the search captures `passengers ∈ [1,9]` and asking once for the whole group felt incorrect. Adds `FormArray` complexity in exchange.
- **Validation duplicated FE/BE — by design.** Both layers validate document format, email, length caps, and route guards. Necessary for UX (instant feedback) and security (never trust client). Documented, not deduplicated.
- **Mocked providers are deterministic.** Same `(origin, destination, date, cabin, providerId)` returns the same flights — desirable for demo reproducibility, but not realistic. A real integration would use circuit breakers, retries, and provider-side caching.
- **No pagination / advanced filters.** Mock returns ~3–6 flights per provider per search, so unnecessary at this scale.
- **Tailwind utility classes in templates.** Higher template noise than a component library, traded for faster iteration and zero theming overhead.

### Known limitations

- No internationalisation (English only).
- Accessibility pass is best-effort; no formal WCAG audit.
- No CI pipeline.
- Logging uses default `ILogger`; no structured correlation IDs yet.

### What I would do next

1. Add Serilog with request correlation IDs and PII-safe scopes; ship structured logs.
2. Add JWT auth + per-user booking scoping (booking endpoint becomes user-scoped).
3. Swap SQLite for Postgres in production (`UseNpgsql`); keep SQLite for local dev.
4. Add `IMemoryCache` (60 s TTL) keyed by search inputs; replace with Redis when distributed.
5. Add per-provider timeouts (`CancellationTokenSource.CreateLinkedTokenSource` + `TimeSpan`) inside `FlightAggregator`.
6. Add OpenTelemetry traces + metrics (`flights.search.duration`, `bookings.created`).
7. Add Playwright e2e: search → sort → book domestic; search → book international.
8. Containerise via Docker Compose (api + web + Postgres).
9. Replace deterministic mocks with a recorded-fixture replay layer that simulates latency and intermittent provider failures.
10. Tune rate-limit thresholds per environment; add per-user keying once auth lands.

### Unfinished work (at submission)

All Must-have items from [TODO.md](TODO.md) are implemented. Every Sprint 1–3 deliverable ships green; Sprint 4 (API hardening) is largely done with the gaps above documented. No in-scope item is missing.
