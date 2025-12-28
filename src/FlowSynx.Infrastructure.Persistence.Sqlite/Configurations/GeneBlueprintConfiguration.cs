using FlowSynx.Application.Serializations;
using FlowSynx.Domain.Aggregates;
using FlowSynx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.Configurations;

public class GeneBlueprintConfiguration : IEntityTypeConfiguration<GeneBlueprint>
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public GeneBlueprintConfiguration(ISerializer jsonSerializer, IDeserializer jsonDeserializer)
    {
        _serializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _deserializer = jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer));
    }

    public void Configure(EntityTypeBuilder<GeneBlueprint> builder)
    {
        builder.ToTable("GeneBlueprints");

        builder.HasKey(gb => gb.Id);
        builder.Property(gb => gb.Id)
            .HasConversion(
                id => id.Value,
                value => new GeneBlueprintId(value));

        builder.Property(gb => gb.Version).IsRequired().HasMaxLength(50);
        builder.Property(gb => gb.GeneticBlueprint).HasMaxLength(255);
        builder.Property(gb => gb.Name).IsRequired().HasMaxLength(200);
        builder.Property(gb => gb.Description).HasMaxLength(1000);

        // ValueComparer for GeneticParameters to ensure proper change tracking
        var parameterListComparer = new ValueComparer<List<ParameterDefinition>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && l.SequenceEqual(r)),
            l =>
                l == null
                    ? 0
                    : l.Aggregate(0, (acc, item) => HashCode.Combine(acc, item != null ? item.GetHashCode() : 0)),
            l =>
                new List<ParameterDefinition>(l ?? System.Linq.Enumerable.Empty<ParameterDefinition>()));

        // Store complex objects as JSON
        builder.Property(gb => gb.GeneticParameters)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<List<ParameterDefinition>>(v))
            .Metadata.SetValueComparer(parameterListComparer);

        builder.Property(gb => gb.ExpressionProfile)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<ExpressionProfile>(v));

        builder.Property(gb => gb.CompatibilityMatrix)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<CompatibilityMatrix>(v));

        builder.Property(gb => gb.ImmuneResponse)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<ImmuneResponse>(v));

        builder.Property(gb => gb.ExecutableComponent)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<ExecutableComponent>(v));

        var metadataComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && _serializer.Serialize(l) == _serializer.Serialize(r)),
            d => d == null ? 0 : _serializer.Serialize(d).GetHashCode(),
            d => d == null
                ? new Dictionary<string, object>()
                : _deserializer.Deserialize<Dictionary<string, object>>(_serializer.Serialize(d)) ?? new Dictionary<string, object>()
        );

        builder.Property(gb => gb.Metadata)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(metadataComparer);

        // Indexes
        builder.HasIndex(gb => new { gb.Id, gb.Version }).IsUnique();
        builder.HasIndex(gb => gb.Name);
        builder.HasIndex(gb => gb.GeneticBlueprint);
    }
}