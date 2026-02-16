using FlowSynx.Domain.WorkflowExecutions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class WorkflowExecutionArtifactConfiguration : IEntityTypeConfiguration<WorkflowExecutionArtifact>
{
    public void Configure(EntityTypeBuilder<WorkflowExecutionArtifact> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(ea => ea.Id);

        builder.Property(ea => ea.Id)
            .ValueGeneratedOnAdd();

        builder.Property(ea => ea.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ea => ea.Type)
            .IsRequired()
            .HasMaxLength(50);

        // Store JSON fields
        builder.Property(ea => ea.Content)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<object>(v, jsonOptions))
            .HasColumnType("TEXT");

        // Relationship - explicit to prevent shadow FK creation
        builder.HasOne(ea => ea.WorkflowExecution)
            .WithMany(er => er.Artifacts)
            .HasForeignKey(ea => ea.WorkflowExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ea => ea.WorkflowExecutionId);
        builder.HasIndex(ea => ea.Name);
        builder.HasIndex(ea => ea.Type);
    }
}