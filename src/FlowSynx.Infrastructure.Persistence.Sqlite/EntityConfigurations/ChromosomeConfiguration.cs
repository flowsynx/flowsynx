using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genomes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class ChromosomeConfiguration : IEntityTypeConfiguration<Chromosome>
{
    public void Configure(EntityTypeBuilder<Chromosome> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Namespace)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("default");

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        // Store JSON fields
        builder.Property(c => c.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(c => c.Spec)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<ChromosomeSpec>(v, jsonOptions))
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(c => c.Labels)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(c => c.Annotations)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(dictionaryComparer);

        // Relationship with Genome
        builder.HasOne<Genome>()
            .WithMany(g => g.Chromosomes)
            .HasForeignKey(c => c.GenomeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.Namespace, c.Name })
            .IsUnique();

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Namespace);
        builder.HasIndex(c => c.GenomeId);
        builder.HasIndex(c => c.CreatedOn);

    }
}