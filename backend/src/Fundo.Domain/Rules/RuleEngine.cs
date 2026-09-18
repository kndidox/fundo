namespace Fundo.Domain.Rules;

/// <summary>
/// Runs every registered <see cref="IApplicationRule"/> and returns the first denial found,
/// or an approval if none match. Deliberately dumb: it knows nothing about individual rules.
/// </summary>
public class RuleEngine
{
    private readonly IEnumerable<IApplicationRule> _rules;

    public RuleEngine(IEnumerable<IApplicationRule> rules)
    {
        _rules = rules;
    }

    public RuleResult Evaluate(ApplicationSubmission submission)
    {
        foreach (var rule in _rules)
        {
            var result = rule.Evaluate(submission);
            if (result.IsDenied)
            {
                return result;
            }
        }

        return RuleResult.Approve();
    }
}
