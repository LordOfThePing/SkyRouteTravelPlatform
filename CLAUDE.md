# CLAUDE.md — SkyRoute Travel Platform

This file gives any Claude Code session full context for the SkyRoute Developer Challenge. Read it before making changes.

---

## 1. Project identity

- **Name:** SkyRoute Travel Platform
- **Type:** Senior-level full-stack technical assessment (3–4 hour challenge)
- **Module under construction:** Flight Search & Booking
- **Stack (mandated):** Angular (frontend) + .NET (backend)
- **AI tools:** allowed and used

## 2. Business context

SkyRoute is a flight aggregator. For this challenge it integrates **two mocked providers** in the backend:

| Provider     | Per-passenger pricing rule                                                                    |
| ------------ | --------------------------------------------------------------------------------------------- |
| GlobalAir    | `final = base * 1.15` (15% fuel surcharge), rounded to 2 decimals                             |
| BudgetWings  | `final = max(base * 0.90, 29.99)` (10% promo discount on base only, USD 29.99 floor)          |

The architecture **must support adding more providers** without modifying core search code (Open/Closed).

## 3. Functional pillars (source of truth)

1. **Flight Search** — origin, destination, departure date, passengers (1–9), cabin class (Economy / Business / First Class). Airports hardcoded, ≥6 across ≥2 countries.
2. **Results display** — provider, flight number, departure, arrival, duration, cabin, price. **Total price is primary**, per-passenger is secondary.
3. **Sorting (frontend only)** — price asc, price desc, duration asc, departure time. No backend round-trip.
4. **States** — loading indicator while searching; clear empty state when no matches.
5. **Booking flow** — flight summary, price breakdown, passenger form (full name, email, document number), confirm action returns a booking reference code.
6. **Dynamic document rule:**
   - International (different countries) → label `Passport Number` + passport validation.
   - Domestic (same country) → label `National ID` + national-ID validation.
   - Both label and validator switch dynamically based on the selected route.

## 4. Technical decisions / defaults

These are the working assumptions used in `README.md` and `TODO.md`. Override here if the user changes them.

- **Angular:** v17+, standalone components, Reactive Forms, RxJS, **Tailwind CSS + Headless UI** (`@ngneat/helipopper`-style or direct Headless UI primitives) for accessible, unstyled components.
- **.NET:** .NET 8 LTS, ASP.NET Core Minimal APIs (or controllers — see decision log), FluentValidation, Serilog.
- **Persistence:** **EF Core 8 + SQLite** (file-based, `skyroute.db`). Bookings + passengers persisted; survives restarts. Repository abstraction so the provider can be swapped for Postgres later.
- **Auth:** explicitly **out of scope** for the challenge. Documented as a next step.
- **Rate limiting:** ASP.NET Core built-in rate limiter, fixed-window per IP.
- **Testing:** xUnit + FluentAssertions on backend; Jasmine/Karma on frontend (Angular default).
- **Repo layout:**
  ```
  /backend    .NET solution (API + Domain + Infrastructure + Tests)
  /frontend   Angular workspace
  /docs       optional supplementary docs
  ```

## 5. Non-negotiable design principles

- **SOLID** — explained explicitly in README §7. Provider integration is the canonical example (Strategy pattern + DI).
- **Statelessness** — API holds no per-request state; bookings persisted via repository.
- **Provider extensibility** — adding a provider = implement `IFlightProvider` + register in DI. No edits to aggregator/search service.
- **Defensive at boundaries only** — validate user input at API edge, trust internal calls.

## 6. Scope discipline

This is a 3–4 hour challenge. Do **not**:

- Build auth/authz beyond stating it as out-of-scope.
- Add a real database unless the user asks.
- Wire CI/CD or cloud deployment.
- Build admin screens, user accounts, or payment integration.
- Over-abstract: no premature interfaces beyond what SOLID/extensibility require.

## 7. Style / output rules for Claude

- Edits to existing files preferred over new files.
- Code comments only when WHY is non-obvious.
- README and TODO are the demo artifacts — keep them tight, professional, concrete. No filler.
- File references in chat use `[file](path)` markdown links.
- When asked to implement, follow `TODO.md` sprint order unless told otherwise.

## 8. Open clarifications (assumed unless user corrects)

| Topic                  | Assumed default                                                  |
| ---------------------- | ---------------------------------------------------------------- |
| Angular version        | 17+ standalone components                                        |
| .NET version           | .NET 8 LTS                                                       |
| Database               | EF Core 8 + SQLite (`skyroute.db`), code-first migrations        |
| Auth                   | Out of scope, documented                                         |
| Currency               | USD only                                                         |
| Round-trip flights     | One-way only (challenge spec implies single departure date)      |
| Number of mock flights | ~5–8 per search per provider, deterministic seed by route+date   |
| Passenger forms        | One full form per passenger (matches `passengers` count)         |
| UI library             | Tailwind CSS + Headless UI (no Material)                         |

If any of these is wrong, update this file and re-run.
