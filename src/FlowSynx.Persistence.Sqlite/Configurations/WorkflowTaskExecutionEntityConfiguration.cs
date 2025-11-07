using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Persistence.Sqlite.Configurations
{
    public class WorkflowTaskExecutionEntityConfiguration : IEntityTypeConfiguration<WorkflowTaskExecutionEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowTaskExecutionEntity> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(t => t.Id)
                   .IsRequired();

            builder.Property(t => t.Name)
                   .HasColumnType("TEXT COLLATE NOCASE")
                   .HasMaxLength(128)
                   .IsRequired();

            builder.Property(t => t.WorkflowId)
                   .IsRequired();

            builder.Property(t => t.WorkflowExecutionId)
                   .IsRequired();

            var statusConverter = new ValueConverter<WorkflowTaskExecutionStatus, string>(
                v => v.ToString(),
                v => (WorkflowTaskExecutionStatus)Enum.Parse(typeof(WorkflowTaskExecutionStatus), v, true)
            );

            builder.Property(t => t.Status)
                   .HasColumnType("TEXT")
                   .IsRequired()
                   .HasConversion(statusConverter);

            builder.HasOne(te => te.WorkflowExecution)
                   .WithMany(we => we.TaskExecutions)
                   .HasForeignKey(te => te.WorkflowExecutionId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}