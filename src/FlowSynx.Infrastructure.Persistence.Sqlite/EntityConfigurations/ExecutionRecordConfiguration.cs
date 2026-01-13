using FlowSynx.Domain.Genomes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class ExecutionRecordConfiguration : IEntityTypeConfiguration<ExecutionRecord>
{
    public void Configure(EntityTypeBuilder<ExecutionRecord> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(er => er.Id);

        builder.Property(er => er.Id)
            .ValueGeneratedOnAdd();

        builder.Property(er => er.ExecutionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(er => er.TargetType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(er => er.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(er => er.Namespace)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("default");

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        // Store JSON fields
        builder.Property(er => er.Request)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(er => er.Response)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(er => er.Context)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(er => er.Parameters)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(er => er.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.HasIndex(er => er.ExecutionId)
            .IsUnique();

        builder.HasIndex(er => er.TargetType);
        builder.HasIndex(er => er.TargetId);
        builder.HasIndex(er => er.Status);
        builder.HasIndex(er => er.Namespace);
        builder.HasIndex(er => er.StartedAt);
        builder.HasIndex(er => er.CompletedAt);
    }
}