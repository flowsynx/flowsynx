using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FlowSynx.Domain.Plugin;
using FlowSynx.Application.Serialization;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class PluginEntityConfiguration : IEntityTypeConfiguration<PluginEntity>
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;

    public PluginEntityConfiguration(IJsonSerializer jsonSerializer, IJsonDeserializer jsonDeserializer)
    {
        ArgumentNullException.ThrowIfNull(jsonSerializer);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
    }

    public void Configure(EntityTypeBuilder<PluginEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Id)
               .IsRequired();

        builder.Property(t => t.UserId)
               .IsRequired();

        builder.Property(t => t.Type)
               .HasColumnType("citext")
               .HasMaxLength(1024)
               .IsRequired();

        builder.Property(t => t.Description)
               .HasMaxLength(4096);

        builder.Property(t => t.Version)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(t => t.PluginLocation)
               .HasMaxLength(4096)
               .IsRequired();

        builder.Property(t => t.RepositoryUrl)
               .HasMaxLength(4096);

        builder.Property(t => t.ProjectUrl)
               .HasMaxLength(4096);

        builder.Property(t => t.Copyright)
               .HasMaxLength(2048);

        builder.Property(t => t.Icon)
               .HasMaxLength(4096);

        builder.Property(t => t.License)
               .HasMaxLength(1024);

        builder.Property(t => t.License)
               .HasMaxLength(1024);

        builder.Property(t => t.LicenseUrl)
               .HasMaxLength(4096);

        builder.Property(t => t.Checksum)
               .HasMaxLength(1024)
               .IsRequired();

        builder.Property(t => t.PluginLocation)
               .HasMaxLength(4096)
               .IsRequired();

        // JSON serialization for plugin specifications
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

        // JSON serialization for plugin operations
        var pluginOperationConverter = new ValueConverter<List<PluginOperation>?, string>(
            v => _jsonSerializer.Serialize(v),
            v => _jsonDeserializer.Deserialize<List<PluginOperation>?>(v)
        );

        var pluginOperationComparer = new ValueComparer<List<PluginOperation>>(
            (c1, c2) => _jsonSerializer.Serialize(c1) ==
                        _jsonSerializer.Serialize(c2),
            c => _jsonSerializer.Serialize(c).GetHashCode(),
            c => _jsonDeserializer.Deserialize<List<PluginOperation>>(_jsonSerializer.Serialize(c))
        );

        builder.Property(e => e.Operations)
               .HasColumnType("jsonb")
               .HasConversion(pluginOperationConverter, pluginOperationComparer);
    }
}