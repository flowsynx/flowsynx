using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(gb => gb.Id);

        builder.Property(gb => gb.Id)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(gb => gb.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(gb => gb.Namespace)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("default");

        builder.Property(gb => gb.Version)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("1.0.0");

        builder.Property(gb => gb.Description)
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

        var specConverter = new ValueConverter<ActivitySpecification, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<ActivitySpecification>(v, jsonOptions)
        );

        // Store JSON fields
        builder.Property(gb => gb.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(objectDictionaryComparer);

        builder.Property(gb => gb.Specification)
            .HasColumnType("TEXT")
            .HasConversion(specConverter);

        builder.Property(gb => gb.Status)
            .HasMaxLength(50)
            .HasDefaultValue("active");

        builder.Property(gb => gb.Labels)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(stringDictionaryComparer);

        builder.Property(gb => gb.Annotations)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(stringDictionaryComparer);

        builder.HasIndex(gb => new { gb.Namespace, gb.Name, gb.Version })
            .IsUnique();

        builder.HasIndex(gb => gb.Name);
        builder.HasIndex(gb => gb.Namespace);
        //builder.HasIndex(gb => gb.Owner);
        builder.HasIndex(gb => gb.CreatedOn);
        builder.HasIndex(gb => gb.Status);
    }
}