namespace Fundo.Domain.Rules;

public sealed record RuleResult
{
    public bool IsDenied { get; }

    /// <summary>Free-form reason code. One of <see cref="DenialReasons"/> for the rules built
    /// into the codebase, or any other string for a data-driven rule's own <c>DenialReason</c>.</summary>
    public string? Reason { get; }

    private RuleResult(bool isDenied, string? reason)
    {
        IsDenied = isDenied;
        Reason = reason;
    }

    public static RuleResult Approve() => new(false, null);

    public static RuleResult Deny(string reason) => new(true, reason);
}
