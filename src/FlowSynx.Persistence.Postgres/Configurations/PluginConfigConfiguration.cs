using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Entities.PluignConfig;
using FlowSynx.Core.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class PluginConfigConfiguration : IEntityTypeConfiguration<PluginConfiguration>
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;

    public PluginConfigConfiguration(IJsonSerializer jsonSerializer, IJsonDeserializer jsonDeserializer)
    {
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
    }

    public void Configure(EntityTypeBuilder<PluginConfiguration> builder)
    {
        builder.HasKey(x => new { x.UserId, x.Name });
        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Type).HasMaxLength(128).IsRequired();

        var dictionaryconverter = new ValueConverter<PluginConfigurationSpecifications?, string>(
            v => _jsonSerializer.Serialize(v),
            v => _jsonDeserializer.Deserialize<PluginConfigurationSpecifications?>(v)
        );

        builder.Property(e => e.Specifications).HasColumnType("jsonb").HasConversion(dictionaryconverter);
    }
}