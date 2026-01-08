using FlowSynx.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class TenantEntityConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        // Primary key
        builder.HasKey(t => t.Id);

        // Value object conversions
        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        // Properties
        builder.Property(t => t.Name)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(1024);

        builder.Property(t => t.Status)
               .HasColumnType("TEXT")
               .IsRequired()
               .HasConversion(
                    status => status.ToString(), 
                    value => (TenantStatus)Enum.Parse(typeof(TenantStatus), value, true)
                );

        // Indexes
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => t.Name);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedOn);
    }
}