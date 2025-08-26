using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FlowSynx.Domain.Workflow;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FlowSynx.Application.Services;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class WorkflowExecutionEntityConfiguration : IEntityTypeConfiguration<WorkflowExecutionEntity>
{
    private readonly IEncryptionService _encryptionService;

    public WorkflowExecutionEntityConfiguration(IEncryptionService encryptionService)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);
        _encryptionService = encryptionService;
    }

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

        var stringConverter = new ValueConverter<string, string>(
            v => _encryptionService.Encrypt(v),
            v => _encryptionService.Decrypt(v)
        );

        var stringComparer = new ValueComparer<string>(
            (s1, s2) => string.Equals(s1, s2, StringComparison.Ordinal),
            s => s == null ? 0 : s.GetHashCode(),
            s => s
        );

        builder.Property(t => t.WorkflowDefinition)
               .IsRequired()
               .HasColumnType("text")
               .HasConversion(stringConverter, stringComparer);

        builder.HasOne(we => we.Workflow)
               .WithMany(w => w.Executions)
               .HasForeignKey(we => we.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}