using Fundo.Domain.Rules;
using Fundo.Domain.Rules.DataDriven;
using Xunit;

namespace Fundo.Domain.Tests.Rules.DataDriven;

public class DataDrivenRuleTests
{
    [Fact]
    public void Evaluate_Equals_Denies_WhenFieldMatchesValue()
    {
        var rule = new DataDrivenRule(RuleDefinition.Create("State", RuleOperator.Equals, "NY", "StateNotAllowed"));

        var result = rule.Evaluate(TestSubmissions.Valid(state: "NY"));

        Assert.True(result.IsDenied);
        Assert.Equal("StateNotAllowed", result.Reason);
    }

    [Fact]
    public void Evaluate_Equals_Approves_WhenFieldDoesNotMatch()
    {
        var rule = new DataDrivenRule(RuleDefinition.Create("State", RuleOperator.Equals, "NY", "StateNotAllowed"));

        var result = rule.Evaluate(TestSubmissions.Valid(state: "CA"));

        Assert.False(result.IsDenied);
    }

    [Fact]
    public void Evaluate_NotEquals_DeniesWhenFieldDiffers()
    {
        var rule = new DataDrivenRule(RuleDefinition.Create("State", RuleOperator.NotEquals, "TX", "OnlyTexas"));

        var result = rule.Evaluate(TestSubmissions.Valid(state: "CA"));

        Assert.True(result.IsDenied);
        Assert.Equal("OnlyTexas", result.Reason);
    }

    [Fact]
    public void Evaluate_In_DeniesWhenFieldIsOneOfTheCommaSeparatedValues()
    {
        var rule = new DataDrivenRule(
            RuleDefinition.Create("Ssn", RuleOperator.In, "111111111, 222222222", "SsnBlacklisted"));

        var result = rule.Evaluate(TestSubmissions.Valid(ssn: "222222222"));

        Assert.True(result.IsDenied);
    }

    [Fact]
    public void Evaluate_Contains_DeniesWhenFieldContainsSubstring()
    {
        var rule = new DataDrivenRule(
            RuleDefinition.Create("CompanyName", RuleOperator.Contains, "shell", "SuspiciousCompany"));

        var result = TestSubmissionWithCompany(rule, "Acme Shell Corp");

        Assert.True(result.IsDenied);
    }

    [Theory]
    [InlineData(50_001, true)]
    [InlineData(50_000, false)]
    [InlineData(10_000, false)]
    public void Evaluate_GreaterThan_ComparesNumerically(decimal amount, bool expectedDenied)
    {
        var rule = new DataDrivenRule(
            RuleDefinition.Create("RequestedAmount", RuleOperator.GreaterThan, "50000", "AmountTooHigh"));

        var result = rule.Evaluate(TestSubmissions.Valid(amount: amount));

        Assert.Equal(expectedDenied, result.IsDenied);
    }

    [Fact]
    public void Evaluate_LessThan_ComparesNumerically()
    {
        var rule = new DataDrivenRule(
            RuleDefinition.Create("RequestedAmount", RuleOperator.LessThan, "500", "AmountTooLow"));

        var result = rule.Evaluate(TestSubmissions.Valid(amount: 100m));

        Assert.True(result.IsDenied);
        Assert.Equal("AmountTooLow", result.Reason);
    }

    [Fact]
    public void Evaluate_UnknownField_Throws()
    {
        var rule = new DataDrivenRule(
            RuleDefinition.Create("NotARealField", RuleOperator.Equals, "x", "Whatever"));

        Assert.Throws<InvalidOperationException>(() => rule.Evaluate(TestSubmissions.Valid()));
    }

    private static RuleResult TestSubmissionWithCompany(DataDrivenRule rule, string companyName)
    {
        var submission = TestSubmissions.Valid() with { CompanyName = companyName };
        return rule.Evaluate(submission);
    }
}
