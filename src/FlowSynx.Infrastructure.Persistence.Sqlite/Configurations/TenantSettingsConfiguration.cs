using FlowSynx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.Configurations;

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> builder)
    {
        builder.ToTable("TenantSettings");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Key).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Value).IsRequired();
    }
}