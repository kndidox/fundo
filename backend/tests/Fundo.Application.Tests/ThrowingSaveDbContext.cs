using Fundo.Application.Persistence;
using Fundo.Domain.Entities;
using Fundo.Domain.Outbox;
using Fundo.Domain.Rules.DataDriven;
using Fundo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Application.Tests;

/// <summary>Delegates everything to a real context except SaveChangesAsync, which always
/// fails — used to prove that a save failure leaves nothing persisted.</summary>
public sealed class ThrowingSaveDbContext(FundoDbContext inner) : IFundoDbContext
{
    public DbSet<Customer> Customers => inner.Customers;
    public DbSet<LoanApplication> LoanApplications => inner.LoanApplications;
    public DbSet<OutboxEvent> OutboxEvents => inner.OutboxEvents;
    public DbSet<RuleDefinition> RuleDefinitions => inner.RuleDefinitions;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Simulated persistence failure");
}
