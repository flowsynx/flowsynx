using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FlowSynx.Application.Services;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FlowSynx.Domain.Plugin;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class PluginEntityConfiguration : IEntityTypeConfiguration<PluginEntity>
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;

    public PluginEntityConfiguration(IJsonSerializer jsonSerializer, IJsonDeserializer jsonDeserializer)
    {
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
    }

    public void Configure(EntityTypeBuilder<PluginEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Id)
               .IsRequired();

        builder.Property(t => t.PluginId)
               .IsRequired();

        builder.Property(t => t.UserId)
               .IsRequired();

        builder.Property(t => t.Name)
               .HasMaxLength(128)
               .IsRequired();

        builder.Property(t => t.Type)
               .HasMaxLength(128)
               .IsRequired();

        builder.Property(t => t.Checksum)
               .HasMaxLength(1024)
               .IsRequired();

        builder.Property(t => t.PluginLocation)
               .HasMaxLength(4096)
               .IsRequired();

        var pluginSpecificationConverter = new ValueConverter<List<PluginSpecification>?, string>(
            v => _jsonSerializer.Serialize(v),
            v => _jsonDeserializer.Deserialize<List<PluginSpecification>?>(v)
        );

        var pluginSpecificationComparer = new ValueComparer<List<PluginSpecification>>(
            (c1, c2) => _jsonSerializer.Serialize(c1) ==
                        _jsonSerializer.Serialize(c2),
            c => _jsonSerializer.Serialize(c).GetHashCode(),
            c => _jsonDeserializer.Deserialize<List<PluginSpecification>>(_jsonSerializer.Serialize(c))
        );

        builder.Property(e => e.Specifications)
               .HasColumnType("jsonb")
               .HasConversion(pluginSpecificationConverter, pluginSpecificationComparer);
    }
}