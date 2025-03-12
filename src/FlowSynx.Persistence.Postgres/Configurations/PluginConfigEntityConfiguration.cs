using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Entities.PluginConfig;
using FlowSynx.Application.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

        var dictionaryconverter = new ValueConverter<PluginConfigurationSpecifications?, string>(
            v => _jsonSerializer.Serialize(v),
            v => _jsonDeserializer.Deserialize<PluginConfigurationSpecifications?>(v)
        );

        builder.Property(e => e.Specifications)
               .HasColumnType("jsonb")
               .HasConversion(dictionaryconverter);
    }
}