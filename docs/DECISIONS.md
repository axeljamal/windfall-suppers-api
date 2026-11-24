# Decisions

Why each choice was made, and what it beat.

## Minimal APIs over MVC controllers
Five endpoints don't need controller classes, attributes and model binding conventions. Minimal APIs keep each route next to its handler and are Microsoft's default for new APIs. If the API grew to dozens of endpoints with shared filters, controllers would be worth reconsidering. Route groups (`MapGroup`) already cover the shared-prefix case.

## Hand-written command and query classes over MediatR
MediatR became a commercial licence in 2025, and with two handler classes it would add indirection without a clear benefit. Plain classes registered in DI give the same read/write split (CQS), are easy to step through in a debugger, and could move to MediatR or Wolverine later if pipelines (logging, validation behaviours) were needed.

## Business rules on the entity over a service layer
`Supper.Book()` owns the rules, so any caller gets them and the unit tests need no database. The alternative, an "anaemic" entity with rules in a service, makes it easy for a second code path to skip a check.

## SQLite over SQL Server for the demo
SQLite runs with no server and keeps the repo clone-and-run. Everything goes through EF Core, so switching to SQL Server means changing `UseSqlite` to `UseSqlServer` and regenerating migrations. One thing to change at that point: SQL Server has a native `rowversion` that could replace the manual `Version` counter.

## Optimistic concurrency (Version token) over locking
Overbooking is a race: two requests both read "1 seat left". A pessimistic lock would hold a database lock per booking, and SQLite only locks the whole database. An optimistic token costs nothing until a real clash, then returns 409 so the client can retry. `ConcurrencyTests` proves it.

## Migrations at start-up (for now)
Simple for a single instance. With several instances, migrations should run once as a deploy step (`dotnet ef migrations bundle`) so instances don't race. CI fails if the model and migrations drift (`has-pending-model-changes`).

## Exceptions for rule breaks over a Result type
A `DomainException` with an error kind (Invalid, NotFound, Conflict) maps to HTTP in one handler, which keeps endpoints to one line. A `Result<T>` type avoids exceptions for expected outcomes and is worth it on hot paths. At this size the exception route is clearer.

## Real HTTP tests over mocked handlers
`WebApplicationFactory` starts the actual app with an in-memory SQLite database, so tests cover routing, JSON, the exception mapping, the EF mapping and the migrations. That's how the `UPDATE` vs `INSERT` bug was caught. A mocked DbContext would have hidden it.

## Header user id over real auth
The point is the booking core, and fake JWTs would add noise. In production the user id would come from a validated token (for Windfall, Supabase Auth JWTs via `AddAuthentication().AddJwtBearer()`).
