using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// In-memory store keyed by SSN — good enough for a mock that only needs to prove it
// received a create vs. an update for the same customer.
var store = new ConcurrentDictionary<string, LoanApplicationRecord>();

app.MapGet("/", () => Results.Ok(new { service = "Fundo External Mock", recordCount = store.Count }));

app.MapPost("/api/loan-applications", (LoanApplicationPayload payload) =>
{
    var record = new LoanApplicationRecord(payload, ReceivedAt: DateTimeOffset.UtcNow, Operation: "Created");
    store[payload.Ssn] = record;
    app.Logger.LogInformation("CREATE received for SSN {Ssn} (customerId={CustomerId})", payload.Ssn, payload.CustomerId);
    return Results.Ok(record);
});

app.MapPut("/api/loan-applications/{ssn}", (string ssn, LoanApplicationPayload payload) =>
{
    var record = new LoanApplicationRecord(payload, ReceivedAt: DateTimeOffset.UtcNow, Operation: "Updated");
    store[ssn] = record;
    app.Logger.LogInformation("UPDATE received for SSN {Ssn} (customerId={CustomerId})", ssn, payload.CustomerId);
    return Results.Ok(record);
});

app.MapGet("/api/loan-applications/{ssn}", (string ssn) =>
    store.TryGetValue(ssn, out var record) ? Results.Ok(record) : Results.NotFound());

app.MapGet("/api/loan-applications", () => Results.Ok(store.Values));

app.Run();

public partial class Program;

public record LoanApplicationPayload(
    Guid CustomerId,
    Guid ApplicationId,
    string FirstName,
    string LastName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string ZipCode,
    string CompanyName,
    string Ssn,
    decimal RequestedAmount);

public record LoanApplicationRecord(LoanApplicationPayload Payload, DateTimeOffset ReceivedAt, string Operation);
