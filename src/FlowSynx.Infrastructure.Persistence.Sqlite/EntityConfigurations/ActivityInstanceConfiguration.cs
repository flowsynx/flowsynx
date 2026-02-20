using FlowSynx.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class ActivityInstanceConfiguration : IEntityTypeConfiguration<Domain.ActivityInstances.ActivityInstance>
{
    public void Configure(EntityTypeBuilder<Domain.ActivityInstances.ActivityInstance> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(gi => gi.Id);

        builder.Property(gi => gi.Id)
            .ValueGeneratedOnAdd();

        builder.Property(c => c.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value));

        // Ensure FK type matches Workflow.Id by converting the value object
        builder.Property(gi => gi.WorkflowId)
            .IsRequired();

        builder.Property(gi => gi.ActivityId)
            .IsRequired()
            .HasMaxLength(200);

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        var configConverter = new ValueConverter<FlowSynx.Domain.ActivityInstances.ActivityConfiguration, string>(
            v => v.ToString(),
            v => (FlowSynx.Domain.ActivityInstances.ActivityConfiguration)Enum.Parse(typeof(FlowSynx.Domain.ActivityInstances.ActivityConfiguration), v, true)
        );

        // Store JSON fields
        builder.Property(gi => gi.Params)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
           .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(gi => gi.Configuration)
            .HasColumnType("TEXT")
            .HasConversion(configConverter);

        builder.Property(gi => gi.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(dictionaryComparer);

        // Relationship with Workflow
        builder.HasOne(we => we.Workflow)
           .WithMany(w => w.Activities)
           .HasForeignKey(we => we.WorkflowId)
           .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(gi => gi.ActivityId);
        builder.HasIndex(gi => gi.WorkflowId);
    }
}