namespace Fundo.Domain.Rules.DataDriven;

/// <summary>
/// A deny rule stored as data (table <c>rule_definitions</c>) instead of code: "if &lt;Field&gt;
/// &lt;Operator&gt; &lt;Value&gt;, deny with &lt;DenialReason&gt;". Adding one of these needs no
/// deploy — see <see cref="DataDrivenRule"/> for how it's evaluated and
/// <see cref="RuleFieldAccessors"/> for which fields are available to reference.
/// </summary>
public class RuleDefinition
{
    public Guid Id { get; private set; }
    public string Field { get; private set; } = default!;
    public RuleOperator Operator { get; private set; }
    public string Value { get; private set; } = default!;
    public string DenialReason { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }

    private RuleDefinition() { }

    public static RuleDefinition Create(
        string field,
        RuleOperator @operator,
        string value,
        string denialReason,
        int priority = 0)
    {
        return new RuleDefinition
        {
            Id = Guid.NewGuid(),
            Field = field,
            Operator = @operator,
            Value = value,
            DenialReason = denialReason,
            IsActive = true,
            Priority = priority
        };
    }
}
