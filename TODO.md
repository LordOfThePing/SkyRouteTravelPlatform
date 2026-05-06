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

## Manual smoke & interview checklist (run first)

### Pre-interview quick checks (5-10 min)

1. Run `make test-all` and confirm backend + frontend suites pass.
2. Run backend (`dotnet run --project backend/src/SkyRoute.Api --launch-profile http`) and verify `GET /health` = healthy.
3. Run frontend (`cd frontend && npm start`) and verify API badge is online.
4. Open Swagger (`/swagger`) and keep it ready for live payload/error demos.

### Manual smoke (core happy path)

1. Search MAD → BCN, 2 pax, Economy → results from both providers; confirm domestic route.
2. Search MAD → JFK, 1 pax, Business → results from both providers; confirm international route.
3. Sort all four ways (price asc/desc, duration asc, departure asc) and verify ordering changes client-side.
4. Book MAD → BCN, 2 pax → **National ID** label on both rows; submit → reference displayed.
5. Book MAD → JFK, 1 pax → **Passport Number** label; submit → reference displayed.

### Edge cases to verify before interview

1. Search same origin/destination → blocked client-side before API call.
2. Passengers boundary: 0 and 10 rejected; 1 and 9 accepted.
3. Departure date in the past blocked by validation.
4. Empty/invalid booking fields show inline errors (name/email/document).
5. Domestic document invalid format rejected; international invalid format rejected.
6. API down (`status = 0`) shows graceful frontend message (not blank screen/crash).
7. Force API 400 (invalid payload via Swagger) and confirm mapped ProblemDetails message appears in UI.
8. Search returning no flights shows empty-state card with refinement guidance.
9. Tie-case sorting sanity: equal sort key falls back consistently (flight number/original order).

### During interview (pick 3-4 live edge cases)

1. Show same-origin search validation.
2. Show domestic vs international document label/validator switch.
3. Stop API briefly to demonstrate graceful frontend error handling.
4. Use Swagger invalid payload to show backend validation + ProblemDetails contract.
5. Show deterministic behavior: same search inputs return stable mock results.

## Automated test plan (implemented)

### Backend (xUnit + FluentAssertions)

- `GlobalAirPricingStrategyTests`: surcharge math, rounding, zero base.
- `BudgetWingsPricingStrategyTests`: discount math, floor behavior, high fare case.
- `FlightAggregatorTests`: provider result union; one-provider failure isolation.
- `SearchRequestValidatorTests`: passenger bounds, same route rejection, past date rejection.
- `BookingRequestValidatorTests`: invalid email, missing document number, missing flight id.
- `BookingRepositoryTests` (EF Core in-memory): persists booking with passenger children; retrieves by reference.

### Frontend (Jasmine/Karma)

- `SearchPageComponent`: invalid form disables submit; all four sort modes; booking navigation state pass-through.
- `BookingPageComponent`: domestic/international document label switch; domestic submit sends `NationalId`.
- `AppComponent`: shell renders correctly.

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

## ✅ Sprint 4 — API hardening + Angular error handling (DONE)

**Completed:** 2026-05-06

- Angular global HTTP interceptor implemented to map backend `ProblemDetails` into user-facing error messages.
- Network-level API failures (`status = 0`) now surface a clear frontend message about backend availability.
- Search and booking flows now consume mapped API errors consistently instead of generic fallback-only messages.
- Backend validation and safe error contracts verified through automated tests:
  - search and booking validator edge-case suites passing in backend test project,
  - frontend test suite passing after interceptor integration.

---

## ✅ QA baseline — Automated tests (DONE)

**Completed:** 2026-05-06

- Backend test suite implemented and extended (`17/17` passing): pricing strategies, aggregator behavior, search validator edge cases, booking validator edge cases, repository persistence/retrieval behavior.
- Frontend test suite updated/implemented (`11/11` passing): app shell baseline, search sorting/navigation checks (all 4 sorts), booking document-rule checks.
- Added top-level test orchestration command: `make test-all`.
- Verified `make test-all` runs both suites successfully with current code.

---

## ✅ Sprint 5 — QA, tests, demo readiness (DONE)

**Completed:** 2026-05-06

- Automated QA completed with full command: `make test-all`.
- Final README pass completed for consistency with current implementation and copy-pasteable setup/test commands.
- Verified release builds succeed: backend `dotnet build -c Release` and frontend `npm run build`.
- Checked for stray debug markers (`console.log`, `TODO`, `FIXME`) in codebase and cleaned/no hits found.
