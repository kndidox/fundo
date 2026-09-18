namespace Fundo.Application.Outbox;

/// <summary>Shape of the data sent to the external service — the contract for both create and update.</summary>
public sealed record ExternalLoanApplicationPayload(
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
