using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Entities.Log;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class WorkflowTaskExecutionEntityConfiguration : IEntityTypeConfiguration<WorkflowTaskExecutionEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskExecutionEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Id)
               .IsRequired();

        var levelConverter = new ValueConverter<WorkflowTaskExecutionStatus, string>(
            v => v.ToString(),
            v => (WorkflowTaskExecutionStatus)Enum.Parse(typeof(WorkflowTaskExecutionStatus), v, true)
        );

        builder.Property(t => t.Status)
               .IsRequired()
               .HasConversion(levelConverter);

        builder.Property(te => te.StartTime)
               .IsRequired();

        builder.HasOne(te => te.WorkflowExecution)
               .WithMany(we => we.TaskExecutions)
               .HasForeignKey(te => te.WorkflowExecutionId)
               .OnDelete(DeleteBehavior.Cascade);

    }
}