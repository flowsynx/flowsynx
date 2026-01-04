using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class TenantSecretConfigEntityConfiguration : IEntityTypeConfiguration<TenantSecretConfig>
{
    public void Configure(EntityTypeBuilder<TenantSecretConfig> builder)
    {
        builder.ToTable("TenantSecretConfigs");

        // Primary key
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(t => t.ProviderType)
               .HasColumnType("TEXT")
               .IsRequired()
               .HasConversion(
                    status => status.ToString(),
                    value => (SecretProviderType)Enum.Parse(typeof(SecretProviderType), value, true)
                );

        builder.Property(gb => gb.Configuration)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<ProviderConfiguration>(v))
            .HasColumnType("TEXT");

        builder.Property(c => c.IsEnabled)
            .IsRequired();

        builder.Property(c => c.CacheDurationMinutes)
            .IsRequired();

        // Relationships
        builder.HasOne(t => t.Tenant)
               .WithMany(w => w.SecretConfigs)
               .HasForeignKey(t => t.TenantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}