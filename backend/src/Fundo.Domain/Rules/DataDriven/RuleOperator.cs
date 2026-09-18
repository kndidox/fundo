namespace Fundo.Domain.Rules.DataDriven;

/// <summary>The fixed, small set of comparisons a data-driven rule can express. Deliberately not
/// a full expression language — anything beyond these needs a real <see cref="IApplicationRule"/>.</summary>
public enum RuleOperator
{
    Equals = 1,
    NotEquals = 2,
    In = 3,
    Contains = 4,
    GreaterThan = 5,
    LessThan = 6
}
