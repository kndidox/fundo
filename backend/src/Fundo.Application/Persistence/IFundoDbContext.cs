using Fundo.Domain.Entities;
using Fundo.Domain.Outbox;
using Fundo.Domain.Rules.DataDriven;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Application.Persistence;

/// <summary>
/// Port for the persistence unit of work. A single <see cref="SaveChangesAsync"/> call
/// commits every tracked change (Customer + LoanApplication + OutboxEvent) as one
/// database transaction — EF Core wraps all statements in one SaveChanges call atomically,
/// so there is no need for an explicit BeginTransaction/Commit dance here.
/// </summary>
public interface IFundoDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<LoanApplication> LoanApplications { get; }
    DbSet<OutboxEvent> OutboxEvents { get; }
    DbSet<RuleDefinition> RuleDefinitions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
