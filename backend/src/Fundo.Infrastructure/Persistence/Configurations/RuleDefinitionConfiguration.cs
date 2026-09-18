using Fundo.Domain.Rules;
using Fundo.Domain.Rules.DataDriven;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Infrastructure.Persistence.Configurations;

public class RuleDefinitionConfiguration : IEntityTypeConfiguration<RuleDefinition>
{
    // Fixed on purpose: EF Core's HasData needs stable keys across migrations.
    private static readonly Guid StateNotAllowedRuleId = new("9f6a6e6e-9b1a-4b6e-8f3a-000000000001");
    private static readonly Guid SsnBlacklistRuleId = new("9f6a6e6e-9b1a-4b6e-8f3a-000000000002");

    public void Configure(EntityTypeBuilder<RuleDefinition> builder)
    {
        builder.ToTable("rule_definitions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Field).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Operator).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Value).IsRequired().HasMaxLength(2000);
        builder.Property(r => r.DenialReason).IsRequired().HasMaxLength(100);

        builder.HasIndex(r => r.IsActive);

        // The two rules required by the spec, seeded as data rather than hardcoded as C#
        // classes — see ARCHITECTURE.md. HasData works against private setters the same way
        // normal materialization does, so RuleDefinition doesn't need a public constructor for this.
        builder.HasData(
            new
            {
                Id = StateNotAllowedRuleId,
                Field = "State",
                Operator = RuleOperator.Equals,
                Value = "NY",
                DenialReason = DenialReasons.StateNotAllowed,
                IsActive = true,
                Priority = 0
            },
            new
            {
                Id = SsnBlacklistRuleId,
                Field = "Ssn",
                Operator = RuleOperator.In,
                Value = "123456789,987654321",
                DenialReason = DenialReasons.SsnBlacklisted,
                IsActive = true,
                Priority = 0
            });
    }
}
