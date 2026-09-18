# Architecture

## Project structure

```
backend/
  src/
    Fundo.Domain/          Entities, the rule engine, and its rules. No framework dependencies.
    Fundo.Application/     Use case (SubmitApplicationUseCase), DTOs, and the IFundoDbContext port.
    Fundo.Infrastructure/  EF Core (Postgres), the outbox dispatcher, and the external HTTP
                            client. Implements the Application ports.
    Fundo.Api/              ASP.NET Core: one thin controller, Program.cs composition root.
  tests/
    Fundo.Domain.Tests/        Rule engine + individual rules, no I/O.
    Fundo.Application.Tests/   The use case against a real (SQLite) relational database.
    Fundo.Api.Tests/           POST /api/applications end-to-end via WebApplicationFactory.
external-service/
  Fundo.ExternalMock/      Minimal API mock of the external system the outbox delivers to.
frontend/
  src/
    app/                   The form ("/"), and the approved/denied result pages.
    components/            ApplicationForm (client component: state, validation, submit) and
                            the two result-page bodies (each reads its own query params).
    lib/                   api.ts (typed fetch client), us-states.ts, denial-messages.ts.
```

Dependencies point inward: `Api → Application/Infrastructure`, `Infrastructure → Application →
Domain`. `Domain` depends on nothing outside the BCL — the rule engine, `Customer` and
`LoanApplication` are plain C#, so business rules aren't tied to EF Core or ASP.NET Core.

No generic repository layer sits in front of EF Core: `IFundoDbContext` (defined in
`Application`, implemented by `FundoDbContext` in `Infrastructure`) exposes the three `DbSet<T>`s
directly. `DbContext` already *is* the repository + unit-of-work; wrapping it in
`ICustomerRepository`/`ILoanApplicationRepository` interfaces here would be a layer with nothing
to do.

## The rule engine

`Fundo.Domain.Rules`:

- `IApplicationRule` — one method, `Evaluate(ApplicationSubmission) → RuleResult`.
- `RuleEngine` — takes `IEnumerable<IApplicationRule>` via DI, runs each in order, returns the
  first denial or an approval if none match. It knows nothing about any specific rule.

Every deny rule, **including the two required by the spec** (state `NY`, SSN blacklist), is data:
a row in the `rule_definitions` table, seeded via EF Core `HasData` in
`RuleDefinitionConfiguration` (so it ships with the schema migration — nothing to remember to run
separately, and the app can never come up with those two rules silently missing). There is no
`StateNotAllowedRule`/`SsnBlacklistRule` class in the codebase; see "Data-driven rules" below for
how a `rule_definitions` row becomes a working `IApplicationRule`.

`RuleResult.Reason` is a plain `string`, not an enum — `Fundo.Domain.Rules.DenialReasons` holds
constants for the two seeded reasons (`StateNotAllowed`, `SsnBlacklisted`), used by the seed data
and by tests, but a rule's reason can be any string. This is what lets a brand-new rule *and* a
brand-new denial reason both ship as pure data, with zero code changes. The trade-off is no
compile-time check that a reason string is "known" — the frontend needs a sensible fallback for a
reason it's never seen before, rather than being able to assume a fixed, closed set.

### Data-driven rules — adding a rule with zero deploys

For the common case — "deny when one field compares to a fixed value" — adding a rule needs
**no code change at all**: insert a row into `rule_definitions`.

```sql
INSERT INTO rule_definitions ("Id", "Field", "Operator", "Value", "DenialReason", "IsActive", "Priority")
VALUES (gen_random_uuid(), 'RequestedAmount', 'GreaterThan', '50000', 'AmountTooHigh', true, 0);
```

How it works (`Fundo.Domain.Rules.DataDriven`):

- `RuleFieldAccessors` maps a field name (`"RequestedAmount"`, `"State"`, `"Ssn"`, ...) to how
  it's read off an `ApplicationSubmission`. Using a field that's already registered here in a new
  rule needs nothing; making a *new* field available to rules needs one line here.
- `RuleOperator` is a small, fixed set of comparisons (`Equals`, `NotEquals`, `In`, `Contains`,
  `GreaterThan`, `LessThan`) — deliberately not a general expression language.
- `DataDrivenRule` implements `IApplicationRule` by interpreting one `RuleDefinition` row with
  the two pieces above.
- `Fundo.Application/DependencyInjection.cs` registers `IEnumerable<IApplicationRule>` as a
  factory (not a fixed list) that turns every active `rule_definitions` row into a
  `DataDrivenRule`, queried fresh per request scope. No caching, no invalidation logic — at this
  scale, querying is simpler and correct by construction.

**What this does and doesn't cover:** a rule whose logic can't be expressed as "field, operator,
value" (aggregates, cross-referencing another service, ...) still needs a real `IApplicationRule`
class, concatenated onto the same query in `DependencyInjection.cs`
(`.Concat([new MyComplexRule(...)])`) — `RuleEngine` doesn't know or care which kind of rule it's
evaluating, so code-based and data-driven rules coexist in the same list with no special-casing.

## Transactions: how "one unit of work" is actually enforced

`SubmitApplicationUseCase` does everything through **one `DbContext` instance** and calls
`SaveChangesAsync` **exactly once**, after staging every change:

1. Look up `Customer` by SSN. Insert a new one, or call `UpdateDetails(...)` on the existing one.
2. Look up `LoanApplication` by `CustomerId`. Insert a new one, or call
   `UpdateRequestedAmount(...)` on the existing one.
3. Insert an `OutboxEvent` row containing a JSON snapshot of the customer + application, typed
   `CustomerApplicationCreated` or `CustomerApplicationUpdated`.
4. `await _db.SaveChangesAsync(cancellationToken)`.

EF Core wraps every INSERT/UPDATE produced by a single `SaveChangesAsync` call in one database
transaction automatically — there's no explicit `BeginTransaction`/`Commit` here because none is
needed. **If step 4 throws for any reason** (a constraint violation, the database being
unreachable, anything) **none of the three writes land** — no half-saved customer, no orphan
application, no outbox row, and therefore no event is ever picked up for delivery. This is
covered by `SubmitApplicationUseCaseTests.ExecuteAsync_WhenSaveFails_NothingIsPersisted`, which
swaps in a `SaveChangesAsync` that always throws and asserts the row counts stay at zero.

If a rule denies the application, none of the above runs at all — the use case returns before
touching the database.

## The background event → external service (transactional outbox)

The HU requires the DB writes and "publish the event" to be one unit of work, but also requires
the HTTP call itself to happen in the background, outside the request. Those two requirements
are reconciled with the **transactional outbox pattern**:

- The "event" that's transactional is the `OutboxEvent` **row**, not the HTTP call. It's written
  in the exact same `SaveChangesAsync` as the customer/application (see above), so it can never
  exist without the data it describes, and never be silently dropped if the save fails.
- `OutboxDispatcherService` (a `BackgroundService` in `Fundo.Infrastructure`) polls for
  `OutboxEvent` rows with `Status = Pending` every 5 seconds, and calls the external service via
  `IExternalLoanClient` — `POST /api/loan-applications` for a `Created` event, `PUT
  /api/loan-applications/{ssn}` for an `Updated` event.
- On success the row is marked `Sent`. On failure it's retried with exponential backoff
  (2s, 4s, 8s, 16s, 32s) up to 5 attempts, after which it's marked `Failed` and left in the table
  for manual inspection — there's no dead-letter queue or alerting (see Trade-offs).

**What happens if the external HTTP call fails:** nothing to the already-committed database
state — the customer and application rows are unaffected, since they were already committed in
a separate, prior transaction. Only the outbox row's own status/attempt count changes. This is
the whole point of the pattern: the external call failing can never roll back data that's already
correctly saved, and a saved application always eventually gets its event delivered (until the
retry budget is exhausted).

### External service contract

`Fundo.ExternalMock` (a separate Minimal API project, so it runs as its own process like a real
external system would):

- `POST /api/loan-applications` — create. Body: JSON snapshot of the customer + application
  (`CustomerId`, `ApplicationId`, name/address/company fields, `Ssn`, `RequestedAmount`).
- `PUT /api/loan-applications/{ssn}` — update, same body shape.
- `GET /api/loan-applications/{ssn}` / `GET /api/loan-applications` — read back what the mock has
  stored (in memory), for manual verification during the demo.

No retries are implemented *inside the mock* — it always returns `200`. Retry behavior lives in
the dispatcher, which is the actual owner of delivery guarantees; a "real" external service would
be expected to be idempotent on `(ssn)`, which is why update uses `PUT` keyed by SSN rather than
a generated external ID the outbox would have to track.

## The frontend

Next.js (App Router, TypeScript, Tailwind). Three routes: `/` (the form), `/approved` and
`/denied` (result pages) — matching the brief's language ("denied users are redirected to a
denied page") literally rather than showing the result inline.

- **`ApplicationForm` is the only stateful piece** — plain `useState`, no form library. Nine
  fields in one form doesn't need React Hook Form or similar; it would be a dependency with
  nothing to do.
- **Client-side validation is UX only** — required fields, SSN/ZIP format, amount > 0. It never
  duplicates a deny *rule*: state is a `<select>` of the 50 states + DC (so the 2-letter format
  the backend expects is structurally guaranteed), but "NY is denied" is never checked in the
  browser. That decision lives in exactly one place — the backend's rule engine — on purpose.
- **Result passed via query params** (`router.push("/approved?applicationId=...")`) rather than
  a context/store — it's two or three values crossing one navigation, not shared app state.
- **`lib/denial-messages.ts` maps `reason` to copy, with a generic fallback for anything it
  doesn't recognize** — a direct consequence of the backend's `reason` being free-form text, not
  a closed enum (see "The rule engine" above): a new backend rule can ship a denial reason the
  frontend has never seen, and it still renders something sensible.
- **Network/server errors surface as a banner on the form itself**, not a redirect — the user's
  input isn't lost, and they can retry without retyping everything.

## Trade-offs (what's deliberately left out)

- **No repository interfaces per entity** — `IFundoDbContext` exposing `DbSet<T>` is the only
  persistence abstraction. A repository per entity would just forward to EF Core with no
  behavior of its own.
- **No message broker (RabbitMQ/etc.)** for the background event — an in-process
  `BackgroundService` polling an outbox table is enough for a single-instance app and avoids
  standing up infrastructure that isn't needed for the pattern to work correctly.
- **No dead-letter queue or alerting** for outbox events that exhaust their retries — they're
  marked `Failed` and stay queryable in the table. A real production system would want an
  operator alert here; out of scope for this exercise.
- **No admin UI for `rule_definitions`** — rules are managed by SQL/migration today. A real
  product would want a screen for this; out of scope here.
- **No authentication** — explicitly out of scope per the brief.
- **No Docker** — everything runs with `dotnet run`/`npm run dev` against a local Postgres
  instance; the brief allows either, and this is simpler for a two-day exercise with no other
  infrastructure to coordinate.
- **No frontend automated tests** — the brief's testing guidance ("cover what matters: the rule
  engine, the returning-customer path, and the endpoint") is backend-focused; the frontend is a
  thin, mostly-presentational layer over an already-tested API. `npm run build` type-checks and
  lints it on every run.
