namespace Fundo.Domain.Rules;

/// <summary>
/// Well-known denial reasons for the rules built into the codebase. A data-driven rule (see
/// <c>Fundo.Domain.Rules.DataDriven</c>) is free to use any other string as its reason — see
/// ARCHITECTURE.md for why <see cref="RuleResult.Reason"/> is a plain string rather than an enum.
/// </summary>
public static class DenialReasons
{
    public const string StateNotAllowed = "StateNotAllowed";
    public const string SsnBlacklisted = "SsnBlacklisted";
}
