using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class GenomeConfiguration : IEntityTypeConfiguration<Genome>
{
    public void Configure(EntityTypeBuilder<Genome> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Namespace)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("default");

        builder.Property(g => g.Description)
            .HasMaxLength(1000);

        var objectDictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        var stringDictionaryComparer = new ValueComparer<Dictionary<string, string>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(d)));

        var specConverter = new ValueConverter<GenomeSpecification, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<GenomeSpecification>(v, jsonOptions)
        );

        // Store JSON fields
        builder.Property(g => g.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(objectDictionaryComparer);

        builder.Property(g => g.Specification)
            .HasColumnType("TEXT")
            .HasConversion(specConverter);

        builder.Property(g => g.Labels)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(stringDictionaryComparer);

        builder.Property(g => g.Annotations)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(stringDictionaryComparer);

        builder.Property(g => g.SharedEnvironment)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(objectDictionaryComparer);

        builder.HasIndex(g => new { g.Namespace, g.Name })
            .IsUnique();

        builder.HasIndex(g => g.Name);
        builder.HasIndex(g => g.Namespace);
        builder.HasIndex(g => g.Owner);
        builder.HasIndex(g => g.CreatedOn);
    }
}