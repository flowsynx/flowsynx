using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class GeneBlueprintConfiguration : IEntityTypeConfiguration<GeneBlueprint>
{
    public void Configure(EntityTypeBuilder<GeneBlueprint> builder)
    {
        builder.ToTable("GeneBlueprints");

        builder.HasKey(gb => gb.Id);
        builder.Property(gb => gb.Id)
            .HasConversion(
                id => id.Value,
                value => new GeneBlueprintId(value));

        builder.Property(c => c.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(gb => gb.UserId).IsRequired();
        builder.Property(gb => gb.Generation).IsRequired().HasMaxLength(50);
        builder.Property(gb => gb.GeneticBlueprint).HasMaxLength(255);
        builder.Property(gb => gb.Phenotypic).IsRequired().HasMaxLength(200);
        builder.Property(gb => gb.Annotation).HasMaxLength(1000);

        // ValueComparer for NucleotideSequences to ensure proper change tracking
        var nucleotideSequencesComparer = new ValueComparer<List<NucleotideSequence>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && l.SequenceEqual(r)),
            l =>
                l == null
                    ? 0
                    : l.Aggregate(0, (acc, item) => HashCode.Combine(acc, item != null ? item.GetHashCode() : 0)),
            l =>
                new List<NucleotideSequence>(l ?? System.Linq.Enumerable.Empty<NucleotideSequence>()));

        // Store complex objects as JSON
        builder.Property(gb => gb.NucleotideSequences)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<List<NucleotideSequence>>(v))
            .Metadata.SetValueComparer(nucleotideSequencesComparer);

        builder.Property(gb => gb.ExpressionProfile)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<ExpressionProfile>(v));

        builder.Property(gb => gb.EpistaticInteraction)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<EpistaticInteraction>(v));

        builder.Property(gb => gb.ImmuneSystem)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<ImmuneSystem>(v));

        builder.Property(gb => gb.ExpressedProtein)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<ExpressedProtein>(v));

        var metadataComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)) ?? new Dictionary<string, object>()
        );

        builder.Property(gb => gb.EpigeneticMarks)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(metadataComparer);

        // Indexes
        builder.HasIndex(gb => new { gb.Id, gb.Generation }).IsUnique();
        builder.HasIndex(gb => gb.Phenotypic);
        builder.HasIndex(gb => gb.GeneticBlueprint);
    }
}