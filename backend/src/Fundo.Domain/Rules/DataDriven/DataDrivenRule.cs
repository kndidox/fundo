using System.Globalization;

namespace Fundo.Domain.Rules.DataDriven;

/// <summary>Interprets one <see cref="RuleDefinition"/> row as an <see cref="IApplicationRule"/>.
/// This is the only code that ever needs to exist for a new "field + operator + value" rule —
/// the rule itself is just a database row.</summary>
public class DataDrivenRule : IApplicationRule
{
    private readonly RuleDefinition _definition;

    public DataDrivenRule(RuleDefinition definition)
    {
        _definition = definition;
    }

    public RuleResult Evaluate(ApplicationSubmission submission)
    {
        if (!RuleFieldAccessors.TryGetValue(_definition.Field, submission, out var fieldValue))
        {
            // An unknown field in a rule_definitions row is a data/config mistake, not something
            // about this particular submission — fail loudly rather than silently never matching.
            throw new InvalidOperationException(
                $"Rule definition '{_definition.Id}' references unknown field '{_definition.Field}'.");
        }

        var isMatch = Matches(fieldValue, _definition.Operator, _definition.Value);

        return isMatch ? RuleResult.Deny(_definition.DenialReason) : RuleResult.Approve();
    }

    private static bool Matches(string fieldValue, RuleOperator @operator, string ruleValue) => @operator switch
    {
        RuleOperator.Equals => string.Equals(fieldValue, ruleValue, StringComparison.OrdinalIgnoreCase),
        RuleOperator.NotEquals => !string.Equals(fieldValue, ruleValue, StringComparison.OrdinalIgnoreCase),
        RuleOperator.Contains => fieldValue.Contains(ruleValue, StringComparison.OrdinalIgnoreCase),
        RuleOperator.In => ruleValue
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Any(candidate => string.Equals(candidate, fieldValue, StringComparison.OrdinalIgnoreCase)),
        RuleOperator.GreaterThan => ParseDecimal(fieldValue) > ParseDecimal(ruleValue),
        RuleOperator.LessThan => ParseDecimal(fieldValue) < ParseDecimal(ruleValue),
        _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, "Unsupported rule operator.")
    };

    private static decimal ParseDecimal(string value) => decimal.Parse(value, CultureInfo.InvariantCulture);
}
