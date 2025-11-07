using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Persistence.Core.Postgres.Configurations;

public class WorkflowTaskExecutionEntityConfiguration : IEntityTypeConfiguration<WorkflowTaskExecutionEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskExecutionEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Id)
               .IsRequired();

        builder.Property(t => t.Name)
               .IsRequired()
               .HasColumnType("citext")
               .HasMaxLength(128);

        builder.Property(t => t.WorkflowId)
               .IsRequired();

        builder.Property(t => t.WorkflowExecutionId)
               .IsRequired();

        var levelConverter = new ValueConverter<WorkflowTaskExecutionStatus, string>(
            v => v.ToString(),
            v => (WorkflowTaskExecutionStatus)Enum.Parse(typeof(WorkflowTaskExecutionStatus), v, true)
        );

        builder.Property(t => t.Status)
               .IsRequired()
               .HasConversion(levelConverter);

        builder.HasOne(te => te.WorkflowExecution)
               .WithMany(we => we.TaskExecutions)
               .HasForeignKey(te => te.WorkflowExecutionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}