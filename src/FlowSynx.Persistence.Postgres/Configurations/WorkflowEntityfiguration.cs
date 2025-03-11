using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class WorkflowEntityfiguration : IEntityTypeConfiguration<WorkflowEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(t => t.Id).IsRequired();

        builder.Property(t => t.UserId)
               .IsRequired();

        builder.Property(t => t.Name)
               .IsRequired()
               .HasMaxLength(128);

        builder.Property(t => t.Definition).IsRequired();
    }
}