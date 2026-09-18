# Fundo — Loan Application Flow

> **Video walkthrough:** _pending — add the public Loom/Jam link here before submitting._

A small loan application flow: a form is submitted, a rule engine in the backend decides
approve/deny, approved applications are persisted transactionally, and a background worker
publishes the result to a mock external service over HTTP.

This repository currently contains the **backend** (.NET API + the mock external service) and
the PostgreSQL schema. The Next.js frontend is a separate, not-yet-started piece of this
repository (see `frontend/`, currently empty) — see `ARCHITECTURE.md` for the trade-off note.

See `ARCHITECTURE.md` for how it's built and why.

---

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) — `dotnet --version` should print `10.x`.
- PostgreSQL running locally with an empty database named `fundo`.
- The [`dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet) global tool (installed below if you don't have it).

## 1. Configure the database connection

The connection string is kept out of source control via .NET user-secrets — run this once,
replacing the password with your local Postgres password:

```bash
dotnet user-secrets init --project backend/src/Fundo.Api
dotnet user-secrets set "ConnectionStrings:Fundo" "Host=localhost;Port=5432;Database=fundo;Username=postgres;Password=YOUR_PASSWORD" --project backend/src/Fundo.Api
```

## 2. Apply the database schema

```bash
dotnet tool install --global dotnet-ef   # skip if already installed
dotnet ef database update --project backend/src/Fundo.Infrastructure --startup-project backend/src/Fundo.Api
```

This creates the `customers`, `loan_applications`, `outbox_events` and `rule_definitions` tables
in `fundo`, and seeds the two required deny rules (state `NY`, SSN blacklist) as rows in
`rule_definitions` — see `ARCHITECTURE.md` for how the rule engine reads them.

## 3. Run everything

Open two terminals (both projects need to be running for the background event to actually
reach the external service):

```bash
# Terminal 1 — the mock external service, listens on http://localhost:5080
dotnet run --project external-service/Fundo.ExternalMock/Fundo.ExternalMock.csproj

# Terminal 2 — the API, listens on http://localhost:5200
dotnet run --project backend/src/Fundo.Api/Fundo.Api.csproj
```

Swagger/OpenAPI UI is available at `http://localhost:5200/openapi/v1.json` in Development.

Submit an application:

```bash
curl -X POST http://localhost:5200/api/applications \
  -H "Content-Type: application/json" \
  -d '{
        "firstName":"Alice","lastName":"Smith",
        "addressLine1":"1 First Ave","city":"Austin","state":"TX","zipCode":"73301",
        "companyName":"Acme","ssn":"555-00-1111","requestedAmount":5000
      }'
```

Within ~5 seconds (the outbox dispatcher's poll interval) you can confirm the external service
received it:

```bash
curl http://localhost:5080/api/loan-applications/555001111
```

## 4. Run the tests

```bash
dotnet test backend/Fundo.slnx
```

24 tests across three projects:
- `Fundo.Domain.Tests` — the rule engine and the data-driven rule mechanism in isolation.
- `Fundo.Application.Tests` — the submit-application use case (new customer, returning customer,
  denial paths, and rollback-on-failure), against a real relational SQLite database (EF Core's
  InMemory provider doesn't support real transactions, so it isn't used anywhere in this repo).
- `Fundo.Api.Tests` — the `POST /api/applications` endpoint end-to-end via `WebApplicationFactory`.

---

## Test data

| Scenario | What to submit |
|---|---|
| **Approved** | Any state other than `NY`, any SSN not listed below. |
| **Denied — state** | `state = "NY"`. |
| **Denied — blacklist** | `ssn = "123-45-6789"` or `ssn = "987-65-4321"` (seeded as a `rule_definitions` row by the `SeedBuiltInRules` migration — see `ARCHITECTURE.md`). |
| **Returning customer** | Submit the same `ssn` twice with a different `requestedAmount`. The second response returns the same `customerId`/`applicationId` as the first — one row of each, updated, not a second one. |

SSNs may be submitted with or without dashes (`123-45-6789` or `123456789`) — they're normalized
to digits-only before reaching the rule engine and the database.
