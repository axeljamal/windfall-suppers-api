# Windfall Suppers API

A small ASP.NET Core Web API in C# for [Windfall](https://windfall.marea-dvipa.uk), my app for small hosted suppers between neighbours. Hosts offer a supper with up to eight seats; guests book and cancel seats. The production app runs on Supabase. This is the same booking core rebuilt in .NET to learn the stack properly.

**Stack:** .NET 10, ASP.NET Core minimal APIs, Entity Framework Core 10 (SQLite, with migrations), xUnit, `WebApplicationFactory` integration tests, OpenAPI, Docker, GitHub Actions.

## Run it

```bash
dotnet run --project src/WindfallSuppers.Api --urls http://localhost:5199
dotnet test
```

The database is created and migrated on start-up. Try the endpoints with `requests.http`, or read the OpenAPI document at `/openapi/v1.json`.

| Method | Route | What it does |
|---|---|---|
| GET | `/suppers?area=hove` | Upcoming suppers, soonest first, with seats left |
| GET | `/suppers/{id}` | One supper |
| POST | `/suppers` | Host offers a supper (201 + `Location`) |
| POST | `/suppers/{id}/bookings` | Guest books seats |
| DELETE | `/suppers/{id}/bookings/mine` | Guest cancels |
| GET | `/health` | Health check |

Writes need an `X-User-Id` header. This stands in for real authentication, which would read the user from a validated token.

## How it's built

```
src/WindfallSuppers.Api
  Domain/        Supper and Booking entities: the business rules live here
  Data/          EF Core DbContext, mappings and migrations
  Features/      Commands (writes) and queries (reads), plus the HTTP endpoints
  ProblemDetailsExceptionHandler.cs   one place that maps rule breaks to 400/404/409
tests/WindfallSuppers.Tests
  SupperRulesTests.cs   unit tests for the domain rules
  SupperApiTests.cs     HTTP tests against the real app with in-memory SQLite
  ConcurrencyTests.cs   two guests racing for the last seat
```

- **Rules in the domain.** `Supper.Book()` enforces: hosts can't book their own supper, one booking per guest, no overbooking, no booking after it starts. Endpoints and handlers hold no rules.
- **Command/query separation (CQS).** `SupperCommands` loads the entity, applies a rule and saves. `SupperQueries` never changes state; it uses `AsNoTracking` and projects straight to the response shape.
- **Concurrency.** `Supper.Version` is an EF Core concurrency token. If two guests read "1 seat left" at the same time, only the first save succeeds and the second gets a 409. A unique index on `(SupperId, GuestId)` backs up the one-booking-per-guest rule in the database.
- **Testable time.** Handlers take .NET's `TimeProvider`, so tests fix the clock.
- **Errors.** Every rule break comes back as RFC 9457 problem details with a clear message.

Reasons for each choice, and the alternatives I turned down, are in [docs/DECISIONS.md](docs/DECISIONS.md).

## Built with AI, checked by me

I built this with Claude Code as a pair, writing the spec first and reviewing every change. The tests caught one real bug on the way. Booking ids were made in C#, so EF Core assumed a new booking with an id already existed and sent `UPDATE` instead of `INSERT`. The fix (`ValueGeneratedNever`) and the reason are commented in `SuppersDbContext.cs`.
