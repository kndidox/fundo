namespace Fundo.Domain.Rules;

/// <summary>
/// A single deny rule. To add a new rule: implement this interface and register it in DI —
/// no existing rule or the engine itself needs to change.
/// </summary>
public interface IApplicationRule
{
    RuleResult Evaluate(ApplicationSubmission submission);
}
