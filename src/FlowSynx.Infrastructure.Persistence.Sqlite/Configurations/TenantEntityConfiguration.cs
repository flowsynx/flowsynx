using FlowSynx.Application.Serializations;
using FlowSynx.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.Configurations;

public class TenantEntityConfiguration : IEntityTypeConfiguration<Tenant>
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public TenantEntityConfiguration(ISerializer jsonSerializer, IDeserializer jsonDeserializer)
    {
        _serializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _deserializer = jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer));
    }

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

        // Configuration reference
        builder.Property(gb => gb.Configuration)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<TenantConfiguration>(v))
            .HasColumnType("TEXT");

        // Indexes
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => t.Name);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedOn);
    }
}