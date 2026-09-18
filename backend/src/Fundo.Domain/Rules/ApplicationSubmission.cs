namespace Fundo.Domain.Rules;

/// <summary>
/// Snapshot of the submitted form data that rules evaluate against. Carries every
/// field a rule could plausibly need, so adding a rule never requires widening this type.
/// </summary>
public sealed record ApplicationSubmission(
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
