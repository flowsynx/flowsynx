using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using FlowSynx.Domain.Entities.Trigger;
using FlowSynx.Application.Services;

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

        var levelConverter = new ValueConverter<WorkflowTriggerType, string>(
            v => v.ToString(),
            v => (WorkflowTriggerType)Enum.Parse(typeof(WorkflowTriggerType), v, true)
        );

        builder.Property(t => t.Type).IsRequired().HasConversion(levelConverter);

        var dictionaryconverter = new ValueConverter<Dictionary<string, object>, string>(
            v => _jsonSerializer.Serialize(v),
            v => _jsonDeserializer.Deserialize<Dictionary<string, object>>(v)
        );

        builder.Property(e => e.Properties)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasConversion(dictionaryconverter);

        builder.HasOne(we => we.Workflow)
               .WithMany(w => w.Triggers)
               .HasForeignKey(we => we.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}