using FlowSynx.Domain.TenantContacts;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class TenantSecretEntityConfiguration : IEntityTypeConfiguration<TenantSecret>
{
    public void Configure(EntityTypeBuilder<TenantSecret> builder)
    {
        builder.ToTable("TenantSecrets");

        // Primary key
        builder.HasKey(t => t.Id);

        // Value object conversions
        builder.Property(t => t.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        // Properties
        builder.Property(t => t.Key)
            .HasConversion(
                id => id.Value,
                value => SecretKey.Create(value))
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.Value)
            .HasConversion(
                id => id.Value,
                value => SecretValue.Create(value))
            .IsRequired();

        // Relationships
        builder.HasOne(t => t.Tenant)
               .WithMany(w => w.Secrets)
               .HasForeignKey(t => t.TenantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.TenantId, s.Key }).IsUnique();
    }
}