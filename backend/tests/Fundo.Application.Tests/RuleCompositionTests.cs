using Fundo.Application.Persistence;
using Fundo.Domain.Rules;
using Fundo.Domain.Rules.DataDriven;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fundo.Application.Tests;

/// <summary>
/// Proves the DI composition in Fundo.Application/DependencyInjection.cs actually resolves a
/// working RuleEngine straight from the database: the two seeded rules (EF Core HasData, see
/// RuleDefinitionConfiguration — CreateContext() applies them via EnsureCreated()) plus one
/// more inserted ad hoc in this test, all evaluated correctly with no code beyond DI wiring.
/// </summary>
public class RuleCompositionTests : SqliteTestBase
{
    [Fact]
    public async Task RuleEngine_ResolvedFromDi_EnforcesSeededAndAdHocRules()
    {
        using var context = CreateContext();
        context.RuleDefinitions.Add(
            RuleDefinition.Create("RequestedAmount", RuleOperator.GreaterThan, "50000", "AmountTooHigh"));
        await context.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<IFundoDbContext>(context);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var rules = scope.ServiceProvider.GetRequiredService<IEnumerable<IApplicationRule>>().ToList();
        Assert.Equal(3, rules.Count); // StateNotAllowed + SsnBlacklisted (seeded) + AmountTooHigh (this test)

        var engine = scope.ServiceProvider.GetRequiredService<RuleEngine>();

        Assert.Equal(DenialReasons.StateNotAllowed, engine.Evaluate(Submission(state: "NY")).Reason);
        Assert.Equal(DenialReasons.SsnBlacklisted, engine.Evaluate(Submission(ssn: "123456789")).Reason);
        Assert.Equal("AmountTooHigh", engine.Evaluate(Submission(amount: 60_000m)).Reason);
        Assert.False(engine.Evaluate(Submission()).IsDenied);
    }

    private static ApplicationSubmission Submission(string state = "CA", string ssn = "111223333", decimal amount = 5000m) =>
        new("Jane", "Doe", "123 Main St", null, "Springfield", state, "12345", "Acme Inc", ssn, amount);
}
