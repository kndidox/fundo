using Fundo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fundo.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.LastName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.AddressLine1).IsRequired().HasMaxLength(200);
        builder.Property(c => c.AddressLine2).HasMaxLength(200);
        builder.Property(c => c.City).IsRequired().HasMaxLength(100);
        builder.Property(c => c.State).IsRequired().HasMaxLength(2);
        builder.Property(c => c.ZipCode).IsRequired().HasMaxLength(10);
        builder.Property(c => c.CompanyName).IsRequired().HasMaxLength(200);
        // Stored normalized (digits only, no dashes) — see ApplicationsController.
        builder.Property(c => c.Ssn).IsRequired().HasMaxLength(9);

        // The SSN is the natural key used to detect a returning customer.
        builder.HasIndex(c => c.Ssn).IsUnique();
    }
}
