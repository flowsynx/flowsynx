using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FlowSynx.Domain.Workflow;

namespace FlowSynx.Persistence.Core.Sqlite.Configurations
{
    public class WorkflowQueueEntityConfiguration : IEntityTypeConfiguration<WorkflowQueueEntity>
    {
        public void Configure(EntityTypeBuilder<WorkflowQueueEntity> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(t => t.Id)
                   .IsRequired();

            builder.Property(t => t.UserId)
                   .IsRequired();

            builder.Property(t => t.WorkflowId)
                   .IsRequired();

            builder.Property(t => t.ExecutionId)
                   .IsRequired();

            var statusConverter = new ValueConverter<WorkflowQueueStatus, string>(
                v => v.ToString(),
                v => (WorkflowQueueStatus)Enum.Parse(typeof(WorkflowQueueStatus), v, true)
            );

            builder.Property(t => t.Status)
                   .HasColumnType("TEXT")
                   .IsRequired()
                   .HasConversion(statusConverter);
        }
    }
}