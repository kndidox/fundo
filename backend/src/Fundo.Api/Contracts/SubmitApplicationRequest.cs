using System.ComponentModel.DataAnnotations;

namespace Fundo.Api.Contracts;

public class SubmitApplicationRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; init; } = default!;

    [Required, StringLength(100)]
    public string LastName { get; init; } = default!;

    [Required, StringLength(200)]
    public string AddressLine1 { get; init; } = default!;

    [StringLength(200)]
    public string? AddressLine2 { get; init; }

    [Required, StringLength(100)]
    public string City { get; init; } = default!;

    [Required, StringLength(2, MinimumLength = 2)]
    public string State { get; init; } = default!;

    [Required, StringLength(10)]
    public string ZipCode { get; init; } = default!;

    [Required, StringLength(200)]
    public string CompanyName { get; init; } = default!;

    [Required, RegularExpression(@"^\d{3}-?\d{2}-?\d{4}$", ErrorMessage = "SSN must be in the form 123-45-6789.")]
    public string Ssn { get; init; } = default!;

    // The double overload avoids RangeAttribute's culture-sensitive string-to-decimal parsing.
    [Range(0.01, 100_000_000)]
    public decimal RequestedAmount { get; init; }
}
