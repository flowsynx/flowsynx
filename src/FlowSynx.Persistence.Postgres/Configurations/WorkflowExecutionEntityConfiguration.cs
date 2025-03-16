using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Entities.Log;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class WorkflowExecutionEntityConfiguration : IEntityTypeConfiguration<WorkflowExecutionEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowExecutionEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(t => t.Id).IsRequired();
        builder.Property(t => t.WorkflowId).IsRequired();
        builder.Property(t => t.UserId).IsRequired();

        var levelConverter = new ValueConverter<WorkflowExecutionStatus, string>(
            v => v.ToString(),
            v => (WorkflowExecutionStatus)Enum.Parse(typeof(WorkflowExecutionStatus), v, true)
        );

        builder.Property(t => t.Status).IsRequired().HasConversion(levelConverter);

        builder.Property(we => we.ExecutionStart)
               .IsRequired();

        builder.HasOne(we => we.Workflow)
               .WithMany(w => w.Executions)
               .HasForeignKey(we => we.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}