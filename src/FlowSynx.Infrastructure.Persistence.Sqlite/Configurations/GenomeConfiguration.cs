using FlowSynx.Application.Serializations;
using FlowSynx.Domain.Aggregates;
using FlowSynx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.Configurations;

public class GenomeConfiguration : IEntityTypeConfiguration<Genome>
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public GenomeConfiguration(ISerializer jsonSerializer, IDeserializer jsonDeserializer)
    {
        _serializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _deserializer = jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer));
    }

    public void Configure(EntityTypeBuilder<Genome> builder)
    {
        builder.ToTable("Genomes");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasConversion(
                id => id.Value,
                value => new GenomeId(value));

        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && _serializer.Serialize(l) == _serializer.Serialize(r)),
            d => d == null ? 0 : _serializer.Serialize(d).GetHashCode(),
            d => d == null ? null : _deserializer.Deserialize<Dictionary<string, object>>(_serializer.Serialize(d)));

        builder.Property(g => g.Metadata)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(g => g.SharedEnvironment)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(dictionaryComparer);

        // Chromosomes relationship
        builder.HasMany(g => g.Chromosomes)
            .WithOne()
            .HasForeignKey("GenomeId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}