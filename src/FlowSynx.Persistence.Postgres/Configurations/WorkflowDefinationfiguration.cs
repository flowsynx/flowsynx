using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Entities.Workflow;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class WorkflowDefinationfiguration : IEntityTypeConfiguration<WorkflowDefination>
{
    public void Configure(EntityTypeBuilder<WorkflowDefination> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(t => t.Id).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(128).IsRequired();
        builder.Property(t => t.Template).IsRequired();
    }
}