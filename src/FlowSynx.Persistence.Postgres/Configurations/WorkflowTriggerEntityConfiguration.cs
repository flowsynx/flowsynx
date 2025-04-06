using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FlowSynx.Domain.Entities.Trigger;
using FlowSynx.Application.Services;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlowSynx.Persistence.Postgres.Configurations;

public class WorkflowTriggerEntityConfiguration : IEntityTypeConfiguration<WorkflowTriggerEntity>
{
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;

    public WorkflowTriggerEntityConfiguration(IJsonSerializer jsonSerializer, IJsonDeserializer jsonDeserializer)
    {
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
    }

    public void Configure(EntityTypeBuilder<WorkflowTriggerEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(t => t.Id).IsRequired();
        builder.Property(t => t.WorkflowId).IsRequired();
        builder.Property(t => t.UserId).IsRequired();

        var typeConverter = new ValueConverter<WorkflowTriggerType, string>(
            v => v.ToString(),
            v => (WorkflowTriggerType)Enum.Parse(typeof(WorkflowTriggerType), v, true)
        );

        builder.Property(t => t.Type).IsRequired().HasConversion(typeConverter);

        var statusConverter = new ValueConverter<WorkflowTriggerStatus, string>(
            v => v.ToString(),
            v => (WorkflowTriggerStatus)Enum.Parse(typeof(WorkflowTriggerStatus), v, true)
        );

        builder.Property(t => t.Status).IsRequired().HasConversion(statusConverter);

        var dictionaryConverter = new ValueConverter<Dictionary<string, object>, string>(
            v => _jsonSerializer.Serialize(v),
            v => _jsonDeserializer.Deserialize<Dictionary<string, object>>(v)
        );

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (c1, c2) => _jsonSerializer.Serialize(c1) ==
                        _jsonSerializer.Serialize(c2),
            c => _jsonSerializer.Serialize(c).GetHashCode(),
            c => _jsonDeserializer.Deserialize<Dictionary<string, object>>(_jsonSerializer.Serialize(c))
        );

        builder.Property(e => e.Properties)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasConversion(dictionaryConverter, dictionaryComparer);

        builder.HasOne(we => we.Workflow)
               .WithMany(w => w.Triggers)
               .HasForeignKey(we => we.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}