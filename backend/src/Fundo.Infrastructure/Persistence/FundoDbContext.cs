using Fundo.Application.Persistence;
using Fundo.Domain.Entities;
using Fundo.Domain.Outbox;
using Fundo.Domain.Rules.DataDriven;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Infrastructure.Persistence;

public class FundoDbContext : DbContext, IFundoDbContext
{
    public FundoDbContext(DbContextOptions<FundoDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoanApplication> LoanApplications => Set<LoanApplication>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<RuleDefinition> RuleDefinitions => Set<RuleDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FundoDbContext).Assembly);
    }
}
