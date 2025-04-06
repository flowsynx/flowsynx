using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Application.Serialization;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class PluginConfigEntityConfiguration : IEntityTypeConfiguration<PluginConfigurationEntity>
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;

    public PluginConfigEntityConfiguration(IJsonSerializer jsonSerializer, IJsonDeserializer jsonDeserializer)
    {
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
    }

    public void Configure(EntityTypeBuilder<PluginConfigurationEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Id)
               .IsRequired();

        builder.Property(t => t.UserId)
               .IsRequired();

        builder.Property(t => t.Name)
               .HasMaxLength(128)
               .IsRequired();

        builder.Property(t => t.Type)
               .HasMaxLength(128)
               .IsRequired();

        var dictionaryConverter = new ValueConverter<PluginConfigurationSpecifications?, string>(
            v => _jsonSerializer.Serialize(v),
            v => _jsonDeserializer.Deserialize<PluginConfigurationSpecifications?>(v)
        );

        var dictionaryComparer = new ValueComparer<PluginConfigurationSpecifications>(
            (c1, c2) => _jsonSerializer.Serialize(c1) ==
                        _jsonSerializer.Serialize(c2),
            c => _jsonSerializer.Serialize(c).GetHashCode(),
            c => _jsonDeserializer.Deserialize<PluginConfigurationSpecifications>(_jsonSerializer.Serialize(c))
        );

        builder.Property(e => e.Specifications)
               .HasColumnType("jsonb")
               .HasConversion(dictionaryConverter, dictionaryComparer);

        builder.HasOne(we => we.Plugin)
               .WithMany(w => w.PluginConfigurations)
               .HasForeignKey(we => we.PluginId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}