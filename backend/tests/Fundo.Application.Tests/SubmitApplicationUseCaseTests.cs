using Fundo.Application.Applications;
using Fundo.Domain.Outbox;
using Fundo.Domain.Rules;
using Fundo.Domain.Rules.DataDriven;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Fundo.Application.Tests;

public class SubmitApplicationUseCaseTests : SqliteTestBase
{
    [Fact]
    public async Task ExecuteAsync_NewCustomer_CreatesCustomerApplicationAndOutboxEvent()
    {
        using var context = CreateContext();
        var useCase = new SubmitApplicationUseCase(CreateRuleEngine(), context, TimeProvider.System);

        var result = await useCase.ExecuteAsync(Submission(ssn: "111223333", amount: 5000m), CancellationToken.None);

        Assert.True(result.IsApproved);

        using var verifyContext = CreateContext();
        Assert.Equal(1, await verifyContext.Customers.CountAsync());
        Assert.Equal(1, await verifyContext.LoanApplications.CountAsync());
        var outboxEvent = Assert.Single(await verifyContext.OutboxEvents.ToListAsync());
        Assert.Equal(OutboxEventType.CustomerApplicationCreated, outboxEvent.EventType);
    }

    [Fact]
    public async Task ExecuteAsync_ReturningCustomer_UpdatesExistingRecordsInsteadOfCreatingNewOnes()
    {
        const string ssn = "222334444";

        using (var context = CreateContext())
        {
            var useCase = new SubmitApplicationUseCase(CreateRuleEngine(), context, TimeProvider.System);
            await useCase.ExecuteAsync(Submission(ssn: ssn, amount: 1000m), CancellationToken.None);
        }

        SubmitApplicationResult secondResult;
        using (var context = CreateContext())
        {
            var useCase = new SubmitApplicationUseCase(CreateRuleEngine(), context, TimeProvider.System);
            secondResult = await useCase.ExecuteAsync(Submission(ssn: ssn, amount: 9000m), CancellationToken.None);
        }

        Assert.True(secondResult.IsApproved);

        using var verifyContext = CreateContext();
        Assert.Equal(1, await verifyContext.Customers.CountAsync());
        var application = await verifyContext.LoanApplications.SingleAsync();
        Assert.Equal(secondResult.ApplicationId, application.Id);
        Assert.Equal(9000m, application.RequestedAmount);

        // SQLite's provider can't translate ORDER BY over DateTimeOffset server-side, so sort client-side here.
        var events = (await verifyContext.OutboxEvents.ToListAsync()).OrderBy(e => e.CreatedAt).ToList();
        Assert.Equal(2, events.Count);
        Assert.Equal(OutboxEventType.CustomerApplicationCreated, events[0].EventType);
        Assert.Equal(OutboxEventType.CustomerApplicationUpdated, events[1].EventType);
    }

    [Fact]
    public async Task ExecuteAsync_DeniedByState_PersistsNothing()
    {
        using var context = CreateContext();
        var useCase = new SubmitApplicationUseCase(CreateRuleEngine(), context, TimeProvider.System);

        var result = await useCase.ExecuteAsync(Submission(state: "NY"), CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.Equal(DenialReasons.StateNotAllowed, result.Reason);
        Assert.Equal(0, await context.Customers.CountAsync());
        Assert.Equal(0, await context.LoanApplications.CountAsync());
        Assert.Equal(0, await context.OutboxEvents.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_DeniedByBlacklist_PersistsNothing()
    {
        using var context = CreateContext();
        var useCase = new SubmitApplicationUseCase(CreateRuleEngine("999887777"), context, TimeProvider.System);

        var result = await useCase.ExecuteAsync(Submission(ssn: "999887777"), CancellationToken.None);

        Assert.False(result.IsApproved);
        Assert.Equal(DenialReasons.SsnBlacklisted, result.Reason);
        Assert.Equal(0, await context.Customers.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSaveFails_NothingIsPersisted()
    {
        using var realContext = CreateContext();
        var failingContext = new ThrowingSaveDbContext(realContext);
        var useCase = new SubmitApplicationUseCase(CreateRuleEngine(), failingContext, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(Submission(ssn: "333445555"), CancellationToken.None));

        using var verifyContext = CreateContext();
        Assert.Equal(0, await verifyContext.Customers.CountAsync());
        Assert.Equal(0, await verifyContext.LoanApplications.CountAsync());
        Assert.Equal(0, await verifyContext.OutboxEvents.CountAsync());
    }

    // Mirrors the two seeded rule_definitions rows (see RuleDefinitionConfiguration) without
    // depending on the real database — the use case tests below only care that *some* rule
    // denies NY/a blacklisted SSN, not where that rule's definition lives.
    private static RuleEngine CreateRuleEngine(params string[] blacklistedSsns)
    {
        IApplicationRule[] rules =
        [
            new DataDrivenRule(RuleDefinition.Create("State", RuleOperator.Equals, "NY", DenialReasons.StateNotAllowed)),
            new DataDrivenRule(RuleDefinition.Create(
                "Ssn", RuleOperator.In, string.Join(",", blacklistedSsns), DenialReasons.SsnBlacklisted))
        ];
        return new RuleEngine(rules);
    }

    private static ApplicationSubmission Submission(string state = "CA", string ssn = "111223333", decimal amount = 5000m) =>
        new("Jane", "Doe", "123 Main St", null, "Springfield", state, "12345", "Acme Inc", ssn, amount);
}
