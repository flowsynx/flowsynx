using FlowSynx.Domain.WorkflowExecutions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class WorkflowExecutionLogConfiguration : IEntityTypeConfiguration<WorkflowExecutionLog>
{
    public void Configure(EntityTypeBuilder<WorkflowExecutionLog> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(el => el.Id);

        builder.Property(el => el.Id)
            .ValueGeneratedOnAdd();

        builder.Property(el => el.Level)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(el => el.Source)
            .HasMaxLength(200);

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        // Store JSON fields
        builder.Property(el => el.Data)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        // Relationship - explicit to align FK and navigation
        builder.HasOne(el => el.WorkflowExecution)
            .WithMany(er => er.Logs)
            .HasForeignKey(el => el.WorkflowExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(el => el.WorkflowExecutionId);
        builder.HasIndex(el => el.Level);
        builder.HasIndex(el => el.Timestamp);
    }
}