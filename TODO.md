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

## Sprint 5 — QA, tests, demo readiness (≈25 min)

- **Objective:** Walk-through ready.
- **In scope:**
  - Unit tests: pricing strategies (GlobalAir + BudgetWings math), search validator edge cases.
  - Manual smoke pass (see test plan below).
  - Automated test command: `make test-all` (backend + frontend).
  - README final pass; ensure setup commands are exact and copy-pasteable.
  - Strip console logs, dead code.
- **Out of scope:** E2E automation.
- **Dependencies:** all prior sprints.
- **DoD:** Both apps start with documented commands; demo script in README works end-to-end; `dotnet test` passes.

---

## Test plan

### Backend (xUnit + FluentAssertions)

- `GlobalAirPricingTests`: 100 → 115.00; 100.001 → rounding behavior; zero base.
- `BudgetWingsPricingTests`: 100 → 90.00; 30 → 29.99 (floor); 1000 → 900.00.
- `FlightAggregatorTests`: returns union of providers; failure of one provider does not crash request.
- `SearchValidatorTests`: passengers 0 / 10 rejected; same origin/destination rejected; past date rejected.
- `BookingValidatorTests`: email format; document required; flight ref required.
- `BookingRepositoryTests` (EF Core in-memory): persists booking + passenger children; returns generated reference.

### Frontend (Jasmine/Karma — minimal)

- `SortPipe` or service: each of the 4 sorts on a fixture array.
- Document label/validator logic: switches on `isInternational` flag.
- `SearchFormComponent`: invalid form disables submit.

### Manual smoke (always run before demo)

1. Search MAD → BCN, 2 pax, Economy → results from both providers; confirm domestic.
2. Search MAD → JFK, 1 pax, Business → results; confirm international.
3. Sort all four ways, verify ordering.
4. Search same origin/destination → validation error before API call.
5. Book MAD → BCN, 2 pax → **National ID** label on both passenger rows; submit → reference shown.
6. Book MAD → JFK, 1 pax → **Passport Number** label; submit → reference shown.
7. Submit empty booking form → see validation errors on every required field.
8. Stop API → verify FE shows graceful error (not blank screen).

---

## If time remains

- Add Serilog structured logs with request correlation IDs.
- Add `IMemoryCache` for repeated identical searches (TTL 60s).
- Add Angular skeleton loaders instead of plain spinner.
- Add `aria-*` attributes and keyboard navigation pass on results table.
- Add Playwright happy-path e2e.
- Add Docker Compose for one-command run.

---

---

## ✅ Sprint 0 — Foundation & architecture (DONE)

**Completed:** 2026-05-06

- .NET 8 solution with `SkyRoute.Api`, `SkyRoute.Domain`, `SkyRoute.Infrastructure`, `SkyRoute.Tests`.
- EF Core 8 + SQLite: `SkyRouteDbContext`, `Booking`/`Passenger` entities, `InitialCreate` migration, auto-migrate on startup → `skyroute.db` created.
- `IFlightProvider`, `IPricingStrategy`, `IFlightAggregator`, `IBookingRepository` interfaces.
- `GlobalAirProvider` + `GlobalAirPricingStrategy` (`base × 1.15`, rounded 2dp).
- `BudgetWingsProvider` + `BudgetWingsPricingStrategy` (`max(base × 0.90, 29.99)`).
- `FlightAggregator` — parallel fan-out, per-provider error isolation.
- `EfCoreBookingRepository` — cascade-persist booking + passengers.
- `AirportCatalog` — 8 airports, 4 countries (Spain, USA, UK, France).
- `FluentValidation` validators for search and booking requests.
- `ExceptionMiddleware` — safe 500 / ProblemDetails, no stack trace leak.
- `POST /api/flights/search`, `POST /api/bookings`, `GET /api/airports`, `GET /health`.
- Swagger UI on `/swagger` (Development only).
- Rate limiter: 60/min search, 20/min booking.
- CORS: `http://localhost:4200` allowed.
- Angular 17 standalone workspace, lazy-loaded routes (`/search`, `/booking`, `/confirmation`).
- Tailwind CSS with `brand-*` palette + `.btn-primary`, `.input-field`, `.card` utilities.
- Angular CDK installed; environments wired; `ApiService` typed.
- Nav shell with live API-status badge calls `/health` on init.
- `dotnet build` — 0 errors; `ng build` — clean, routes code-split.
- `GET /health` → `{"status":"healthy"}` on `http://localhost:5080`.

---

## ✅ Sprint 1 — Search flow (DONE)

**Completed:** 2026-05-06

- Angular search form implemented with Reactive Forms: origin, destination, departure date, passengers (1-9), cabin class.
- Search page now loads airports from `GET /api/airports`.
- Form submit wired to `POST /api/flights/search` using typed `SearchRequest` and `SearchResponse`.
- Client-side validation added for required fields, passenger range, and same origin/destination guard.
- Results table rendered with provider, flight number, departure, arrival, duration, cabin, per-passenger price, and total price.
- Frontend build verification completed (`ng build` clean after Sprint 1 changes).

---

## ✅ Sprint 2 — Results, sorting, loading & empty states (DONE)

**Completed:** 2026-05-06

- Client-side sorting added on search results: price ascending, price descending, duration ascending, and departure time ascending.
- Stable sort behavior implemented with tie-breakers on flight number and original index.
- Dedicated loading state card with spinner added while search requests are in flight.
- Empty-state card added for successful searches returning no flights, with guidance to refine criteria.
- Results list now renders from computed sorted data while preserving total price prominence.
- Frontend build verification completed (`ng build` clean after Sprint 2 changes).

---

## ✅ Sprint 3 — Booking flow + multi-passenger + dynamic document rule (DONE)

**Completed:** 2026-05-06

- Search results now include `Book` action, passing selected flight to `/booking` via router state.
- Booking page implemented with flight summary and price breakdown.
- Passenger `FormArray` implemented with one form per passenger based on selected offer passenger count.
- Dynamic document behavior implemented: domestic routes use `National ID`, international routes use `Passport Number`.
- Dynamic validators applied per passenger row when route/country context resolves.
- `POST /api/bookings` wired from frontend; successful booking redirects to `/confirmation`.
- Confirmation page implemented with booking reference, flight details, and full passenger list.
- Frontend build verification completed (`ng build` clean after Sprint 3 changes).

---

## ✅ QA baseline — Automated tests (DONE)

**Completed:** 2026-05-06

- Backend test suite implemented (15 passing): pricing strategies, aggregator behavior, search validator edge cases, booking validator edge cases.
- Frontend test suite updated/implemented (9 passing): app shell baseline, search sorting/navigation checks, booking document-rule checks.
- Added top-level test orchestration command: `make test-all`.
- Verified `make test-all` runs both suites successfully with current code.

---

## ✅ Sprint 4 — API hardening + Angular error handling (DONE)

**Completed:** 2026-05-06

- Angular global HTTP interceptor implemented to map backend `ProblemDetails` into user-facing error messages.
- Network-level API failures (`status = 0`) now surface a clear frontend message about backend availability.
- Search and booking flows now consume mapped API errors consistently instead of generic fallback-only messages.
- Backend validation and safe error contracts verified through automated tests:
  - search and booking validator edge-case suites passing in backend test project,
  - frontend test suite passing after interceptor integration.
