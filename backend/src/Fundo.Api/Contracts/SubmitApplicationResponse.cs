namespace Fundo.Api.Contracts;

public class SubmitApplicationResponse
{
    /// <summary>"Approved" or "Denied".</summary>
    public required string Status { get; init; }

    /// <summary>Populated only when <see cref="Status"/> is "Denied".</summary>
    public string? Reason { get; init; }

    /// <summary>Populated only when <see cref="Status"/> is "Approved".</summary>
    public Guid? ApplicationId { get; init; }

    public Guid? CustomerId { get; init; }
}
