using FlowSynx.Domain.TenantContacts;
using FlowSynx.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class TenantContactEntityConfiguration : IEntityTypeConfiguration<TenantContact>
{
    public void Configure(EntityTypeBuilder<TenantContact> builder)
    {
        builder.ToTable("TenantContacts");

        // Primary key
        builder.HasKey(t => t.Id);

        // Value object conversions
        builder.Property(t => t.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        // Properties
        builder.Property(t => t.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.Email)
            .HasMaxLength(256)
            .IsRequired();

        // Relationships
        builder.HasOne(t => t.Tenant)
               .WithMany(w => w.Contacts)
               .HasForeignKey(t => t.TenantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}