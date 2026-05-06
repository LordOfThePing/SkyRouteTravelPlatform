# TODO.md — SkyRoute Flight Search & Booking

Sprint-based delivery plan for the 3–4 hour senior full-stack challenge. Each sprint is a vertical slice with clear DoD. Times are budgets, not estimates.

---

## Time-aware prioritization

| Priority    | Items                                                                                      |
| ----------- | ------------------------------------------------------------------------------------------ |
| Must-have   | Search form, dual-provider mock, pricing rules, results table, sorting, booking + dyn doc, booking ref, README |
| Should-have | Loading/empty states polished, FluentValidation, basic unit tests on pricing strategies    |
| Nice-to-have| Rate limiting, structured logging, e2e happy-path, animation polish, accessibility pass    |

If the clock runs out, ship Must-have + README and document the rest under §"If time remains".

---

## Sprint 0 — Foundation & architecture (≈40 min)

- **Objective:** Scaffold runnable solutions with clean architecture in place.
- **In scope:**
  - .NET 8 solution: `SkyRoute.Api`, `SkyRoute.Domain`, `SkyRoute.Infrastructure`, `SkyRoute.Tests`.
  - **EF Core 8 + SQLite**: `SkyRouteDbContext`, `Booking` and `Passenger` entities, initial migration, `db.Database.Migrate()` on startup, file `skyroute.db`.
  - Angular 17 workspace, standalone components, routing, environments file.
  - **Tailwind CSS + Headless UI** wired in: `tailwind.config.js`, PostCSS, base `styles.css` with `@tailwind` directives; install `@headlessui/angular` (or use CDK + Tailwind primitives).
  - CORS allowed for local Angular origin.
  - Provider abstraction skeleton: `IFlightProvider`, `IFlightAggregator`, `IBookingRepository`, DI registration.
  - Hardcoded airports module (≥6 airports, ≥2 countries) shared shape.
- **Out of scope:** real data, validation rules, styling beyond design tokens.
- **Dependencies:** none.
- **Risks:** Tailwind/PostCSS setup quirks in Angular — mitigate by following the official Angular + Tailwind v3 recipe.
- **DoD:** `dotnet run` exposes `/health` and applies migration on startup; `ng serve` renders Tailwind-styled shell page that calls `/health`; `skyroute.db` file is created on first run.

## Sprint 1 — Search flow (≈45 min)

- **Objective:** End-to-end flight search across both mock providers with correct pricing.
- **In scope:**
  - `POST /api/flights/search` contract + DTOs.
  - `GlobalAirProvider` and `BudgetWingsProvider` returning deterministic mocked flights for any input.
  - Pricing strategies:
    - GlobalAir: `round(base * 1.15, 2)`.
    - BudgetWings: `max(round(base * 0.90, 2), 29.99)`.
  - Aggregator merges provider responses; returns total + per-passenger prices.
  - Angular search form (Reactive Forms): origin, destination, departure date, passengers (1–9), cabin class.
  - Service layer in Angular calls API; results model typed.
- **Out of scope:** sorting, booking, error UI polish.
- **Dependencies:** Sprint 0.
- **Risks:** mock determinism (same query → same results) — seed RNG by `(origin, destination, date, providerId)`.
- **DoD:** Submitting form returns ≥1 result per provider with correct math; verified with two manual test cases.

## Sprint 2 — Results, sorting, loading & empty states (≈30 min)

- **Objective:** Production-grade results UX, frontend-only sorting.
- **In scope:**
  - Results list/table: provider, flight #, departure, arrival, duration, cabin, **total price (primary)**, per-passenger (secondary).
  - Sort dropdown: price ↑, price ↓, duration ↑, departure time. Pure client-side sort (`Array.prototype.sort` on a copy).
  - Loading spinner bound to search-in-flight signal.
  - Empty state component with explanatory copy and CTA to refine search.
- **Out of scope:** pagination, filters beyond sort.
- **Dependencies:** Sprint 1.
- **Risks:** sort instability — use stable comparators; tie-break on flight number.
- **DoD:** All four sorts verified; empty state shown when API returns `[]`; spinner visible during request.

## Sprint 3 — Booking flow + multi-passenger + dynamic document rule (≈55 min)

- **Objective:** Functional booking with route-aware document field, one form per passenger, persisted in SQLite.
- **In scope:**
  - Route `/booking/:flightId` (state-passed flight summary to avoid re-fetch).
  - Booking screen sections: flight summary, price breakdown (per-passenger × count = total), **passenger forms array (one per passenger)**.
  - Per-passenger fields: full name, email, document number.
  - Use `FormArray` of `FormGroup` so passenger count from search drives the number of rendered forms; show `Passenger 1 of N` headers.
  - **Dynamic document logic** applied to every passenger row:
    - Determine `isInternational` by comparing country of origin vs destination airport (computed once per booking).
    - Switch label: `Passport Number` vs `National ID`.
    - Switch validator dynamically (passport regex vs national-ID regex) — reset and re-apply per row on country change.
  - `POST /api/bookings` accepts a `passengers: [...]` array; persists `Booking` + child `Passenger` rows via EF Core; returns `{ bookingReference }` (e.g. `SR-XXXXXX`).
  - Confirmation view shows reference code prominently and lists every passenger.
- **Out of scope:** payment, seat selection, per-passenger price differences.
- **Dependencies:** Sprint 1 (flight model), Sprint 0 (airports with country, EF Core).
- **Risks:**
  - Dynamic validators on a `FormArray` leaking stale state — apply `setValidators` + `updateValueAndValidity` on every row when route changes.
  - Persisted booking with FK to passengers — verify cascade delete and migration applied.
- **DoD:** Domestic and international routes both show correct label + validation on every passenger row; confirm persists booking + N passengers in `skyroute.db`; reference visible to user; querying SQLite shows the rows.

## Sprint 4 — API contract, validation, error handling (≈25 min)

- **Objective:** Hardened API surface.
- **In scope:**
  - FluentValidation rules on `SearchRequest` and `BookingRequest`.
  - ProblemDetails (RFC 7807) error responses for 400 / 404 / 500.
  - Global exception handler middleware — never leak stack traces.
  - Angular HTTP interceptor maps ProblemDetails to user-facing toast/inline errors.
  - Basic input sanitization (trim, length caps, whitelisted enums).
- **Out of scope:** auth, full i18n of errors.
- **Dependencies:** Sprints 1 & 3.
- **Risks:** validation duplicated FE/BE — accept that, document it.
- **DoD:** Invalid payloads return structured 400; unexpected errors return safe 500; UI surfaces both.

## Sprint 5 — QA, polish, demo readiness (≈25 min)

- **Objective:** Walk-through ready.
- **In scope:**
  - Manual smoke pass of: search (multiple cabins/pax), all sorts, empty state (impossible route), domestic booking, international booking, error path.
  - README final pass; ensure setup commands are exact and copy-pasteable.
  - Add a couple of unit tests on pricing strategies (the math is the riskiest pure logic).
  - Strip TODOs, console logs, dead code.
- **Out of scope:** E2E automation.
- **Dependencies:** all prior sprints.
- **Risks:** discovering broken happy path late — run smoke at the start of this sprint.
- **DoD:** Both apps start with documented commands; demo script in README §9 works end-to-end.

---

## Test plan

### Backend (xUnit + FluentAssertions)

- `GlobalAirPricingTests`: 100 → 115.00; 100.001 → rounding behavior; zero base.
- `BudgetWingsPricingTests`: 100 → 90.00; 30 → 29.99 (floor); 1000 → 900.00.
- `FlightAggregatorTests`: returns union of providers; failure of one provider does not crash request (resilience hint).
- `SearchValidatorTests`: passengers 0 / 10 rejected; same origin/destination rejected; past date rejected.
- `BookingValidatorTests`: email format; document required; flight ref required; **passengers array length must equal flight passenger count**.
- `BookingRepositoryTests` (EF Core in-memory or SQLite-in-memory): persists booking + passenger children; returns generated reference.

### API (integration, optional if time)

- `POST /api/flights/search` happy path returns 200 with combined results.
- `POST /api/bookings` returns 201 with reference; invalid payload returns 400 ProblemDetails.

### Frontend (Jasmine/Karma — minimal)

- `SortService` (or pipe): each of the 4 sorts on a fixture array.
- `DocumentValidatorDirective` (or component logic): switches validators on country change.
- `SearchFormComponent`: invalid form disables submit.

### Manual smoke (always run before demo)

1. Search Madrid → Barcelona, 2 pax, Economy → expect domestic.
2. Search Madrid → New York, 1 pax, Business → expect international.
3. Sort all four ways, verify ordering.
4. Force empty state (e.g., same origin/destination should be blocked, or a route the mock returns no flights for).
5. Book one of each (domestic + international) with 2+ passengers → confirm reference is shown and all passenger rows render with the correct document label.
6. Submit empty form → see validation errors.
7. Stop API → verify FE shows graceful error.

---

## If time remains

- Add ASP.NET Core rate limiter (fixed window, per IP, e.g. 60/min on `/search`).
- Add Serilog structured logs with request correlation IDs.
- Add `IMemoryCache` for repeated identical searches (TTL 60s).
- Add OpenAPI/Swagger UI with examples.
- Add Angular skeleton loaders instead of plain spinner.
- Add `aria-*` attributes and keyboard navigation pass on results table.
- Add Playwright happy-path e2e.
- Add Docker Compose for one-command run.
