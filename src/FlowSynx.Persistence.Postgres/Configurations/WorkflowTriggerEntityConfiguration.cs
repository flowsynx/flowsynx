using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FlowSynx.Domain.Entities.Trigger;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class WorkflowTriggerEntityConfiguration : IEntityTypeConfiguration<WorkflowTriggerEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowTriggerEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(t => t.Id).IsRequired();
        builder.Property(t => t.WorkflowId).IsRequired();
        builder.Property(t => t.UserId).IsRequired();

        var levelConverter = new ValueConverter<WorkflowTriggerType, string>(
            v => v.ToString(),
            v => (WorkflowTriggerType)Enum.Parse(typeof(WorkflowTriggerType), v, true)
        );

        builder.Property(t => t.Type).IsRequired().HasConversion(levelConverter);

        builder.HasOne(we => we.Workflow)
               .WithMany(w => w.Triggers)
               .HasForeignKey(we => we.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}