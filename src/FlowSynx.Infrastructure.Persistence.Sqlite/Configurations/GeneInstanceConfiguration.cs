using FlowSynx.Application.Serializations;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.Configurations;

public class GeneInstanceConfiguration : IEntityTypeConfiguration<GeneInstance>
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public GeneInstanceConfiguration(ISerializer jsonSerializer, IDeserializer jsonDeserializer)
    {
        _serializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _deserializer = jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer));
    }

    public void Configure(EntityTypeBuilder<GeneInstance> builder)
    {
        builder.ToTable("GeneInstances");

        builder.HasKey(gi => gi.Id);
        builder.Property(gi => gi.Id)
            .HasConversion(
                id => id.Value,
                value => new GeneInstanceId(value));

        builder.Property(gi => gi.TenantId).IsRequired();
        builder.Property(gi => gi.UserId).IsRequired();

        // Foreign key for GeneBlueprint
        builder.Property(gi => gi.GeneBlueprintId)
            .HasConversion(
                id => id.Value,
                value => new GeneBlueprintId(value));

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && _serializer.Serialize(l) == _serializer.Serialize(r)),
            d => d == null ? 0 : _serializer.Serialize(d).GetHashCode(),
            d => d == null ? null : _deserializer.Deserialize<Dictionary<string, object>>(_serializer.Serialize(d)));

        builder.Property(gi => gi.Parameters)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(gi => gi.ExpressionConfiguration)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<ExpressionConfiguration>(v));

        var dependenciesComparer = new ValueComparer<List<GeneInstanceId>>(
        (l, r) =>
            ReferenceEquals(l, r) ||
            (l != null && r != null && l.SequenceEqual(r)),
        l =>
            l == null
                ? 0
                : l.Aggregate(0, (acc, item) => HashCode.Combine(acc, item != null ? item.GetHashCode() : 0)),
        l =>
            new List<GeneInstanceId>(l ?? System.Linq.Enumerable.Empty<GeneInstanceId>()));

        builder.Property(gi => gi.Dependencies)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<List<GeneInstanceId>>(v))
            .Metadata.SetValueComparer(dependenciesComparer);

        builder.Property(gi => gi.Metadata)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(dictionaryComparer);

        // Foreign key to Chromosome
        builder.Property<ChromosomeId>("ChromosomeId")
            .HasConversion(
                id => id.Value,
                value => new ChromosomeId(value));

        // Indexes
        builder.HasIndex("GeneBlueprintId");
        builder.HasIndex("ChromosomeId");
    }
}