using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class GeneInstanceConfiguration : IEntityTypeConfiguration<GeneInstance>
{
    public void Configure(EntityTypeBuilder<GeneInstance> builder)
    {
        builder.ToTable("GeneInstances");

        builder.HasKey(gi => gi.Id);
        builder.Property(gi => gi.Id)
            .HasConversion(
                id => id.Value,
                value => new GeneInstanceId(value));

        builder.Property(c => c.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(gi => gi.UserId).IsRequired();

        // Foreign key for GeneBlueprint
        builder.Property(gi => gi.GeneBlueprintId)
            .HasConversion(
                id => id.Value,
                value => new GeneBlueprintId(value));

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        builder.Property(gi => gi.NucleotideSequences)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(gi => gi.ExpressionConfiguration)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<ExpressionConfiguration>(v));

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

        builder.Property(gi => gi.RegulatoryNetwork)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<List<GeneInstanceId>>(v))
            .Metadata.SetValueComparer(dependenciesComparer);

        builder.Property(gi => gi.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v))
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