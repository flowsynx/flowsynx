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

public class GeneInstanceConfiguration : IEntityTypeConfiguration<Domain.GeneInstances.GeneInstance>
{
    public void Configure(EntityTypeBuilder<Domain.GeneInstances.GeneInstance> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(gi => gi.Id);

        builder.Property(gi => gi.Id)
            .ValueGeneratedOnAdd();

        builder.Property(gi => gi.GeneId)
            .IsRequired()
            .HasMaxLength(200);

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        // Store JSON fields
        builder.Property(gi => gi.Parameters)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
           .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(gi => gi.Config)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<GeneConfig>(v, jsonOptions))
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(gi => gi.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        // Relationship with Chromosome
        builder.HasOne<Chromosome>()
            .WithMany(c => c.Genes)
            .HasForeignKey(gi => gi.ChromosomeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(gi => gi.GeneId);
        builder.HasIndex(gi => gi.ChromosomeId);
    }
}