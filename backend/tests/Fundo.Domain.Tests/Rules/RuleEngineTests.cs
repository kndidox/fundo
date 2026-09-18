using Fundo.Domain.Rules;
using Xunit;

namespace Fundo.Domain.Tests.Rules;

public class RuleEngineTests
{
    private sealed class AlwaysApproveRule : IApplicationRule
    {
        public RuleResult Evaluate(ApplicationSubmission submission) => RuleResult.Approve();
    }

    private sealed class AlwaysDenyRule(string reason) : IApplicationRule
    {
        public RuleResult Evaluate(ApplicationSubmission submission) => RuleResult.Deny(reason);
    }

    [Fact]
    public void Evaluate_Approves_WhenNoRuleDenies()
    {
        var engine = new RuleEngine([new AlwaysApproveRule(), new AlwaysApproveRule()]);

        var result = engine.Evaluate(TestSubmissions.Valid());

        Assert.False(result.IsDenied);
    }

    [Fact]
    public void Evaluate_ReturnsFirstDenial_WhenMultipleRulesWouldDeny()
    {
        var engine = new RuleEngine(
        [
            new AlwaysApproveRule(),
            new AlwaysDenyRule(DenialReasons.StateNotAllowed),
            new AlwaysDenyRule(DenialReasons.SsnBlacklisted)
        ]);

        var result = engine.Evaluate(TestSubmissions.Valid());

        Assert.True(result.IsDenied);
        Assert.Equal(DenialReasons.StateNotAllowed, result.Reason);
    }

    [Fact]
    public void Evaluate_DoesNotRequireChangingTheEngine_WhenANewRuleIsAdded()
    {
        // Proves the "add a rule = no changes to existing code" requirement: a brand-new
        // rule type is simply added to the list handed to RuleEngine's constructor.
        var engine = new RuleEngine([new AlwaysApproveRule(), new AlwaysDenyRule(DenialReasons.SsnBlacklisted)]);

        var result = engine.Evaluate(TestSubmissions.Valid());

        Assert.True(result.IsDenied);
        Assert.Equal(DenialReasons.SsnBlacklisted, result.Reason);
    }

    [Fact]
    public void Evaluate_AcceptsAnyStringReason_ForDataDrivenRules()
    {
        // A data-driven rule's DenialReason is free text (see DataDrivenRule) — the engine
        // doesn't care that it isn't one of the well-known DenialReasons constants.
        var engine = new RuleEngine([new AlwaysDenyRule("LowCompanyRevenue")]);

        var result = engine.Evaluate(TestSubmissions.Valid());

        Assert.True(result.IsDenied);
        Assert.Equal("LowCompanyRevenue", result.Reason);
    }
}
