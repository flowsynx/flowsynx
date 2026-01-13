using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

        builder.Property(c => c.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value));

        // Ensure FK type matches Chromosome.Id by converting the value object
        builder.Property(gi => gi.ChromosomeId)
            .IsRequired();

        builder.Property(gi => gi.GeneId)
            .IsRequired()
            .HasMaxLength(200);

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        var configConverter = new ValueConverter<GeneConfig, string>(
            v => v.ToString(),
            v => (GeneConfig)Enum.Parse(typeof(GeneConfig), v, true)
        );

        // Store JSON fields
        builder.Property(gi => gi.Parameters)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
           .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(gi => gi.Config)
            .HasColumnType("TEXT")
            .HasConversion(configConverter);

        builder.Property(gi => gi.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        // Relationship with Chromosome
        builder.HasOne(we => we.Chromosome)
           .WithMany(w => w.Genes)
           .HasForeignKey(we => we.ChromosomeId)
           .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(gi => gi.GeneId);
        builder.HasIndex(gi => gi.ChromosomeId);
    }
}