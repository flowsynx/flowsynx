using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class WorkflowConfiguration : IEntityTypeConfiguration<Workflow>
{
    public void Configure(EntityTypeBuilder<Workflow> builder)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedOnAdd();

        builder.Property(t => t.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(c => c.WorkflowApplicationId)
            .IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Namespace)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("default");

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Ignore(c => c.Activities);

        var objectDictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        var stringDictionaryComparer = new ValueComparer<Dictionary<string, string>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(d)));

        var specConverter = new ValueConverter<WorkflowSpecification, string>(
            v => JsonSerializer.Serialize(v, jsonOptions),
            v => JsonSerializer.Deserialize<WorkflowSpecification>(v, jsonOptions)
        );

        // Store JSON fields
        builder.Property(c => c.Metadata)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions) ?? new Dictionary<string, object>())
            .Metadata.SetValueComparer(objectDictionaryComparer);

        builder.Property(c => c.Specification)
            .HasColumnType("TEXT")
            .HasConversion(specConverter);

        builder.Property(c => c.Labels)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(stringDictionaryComparer);

        builder.Property(c => c.Annotations)
            .HasColumnType("TEXT")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, jsonOptions) ?? new Dictionary<string, string>())
            .Metadata.SetValueComparer(stringDictionaryComparer);

        // Relationship with WorkflowApplication
        builder.HasOne(we => we.WorkflowApplication)
           .WithMany(w => w.Workflows)
           .HasForeignKey(we => we.WorkflowApplicationId)
           .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.Namespace, c.Name })
            .IsUnique();

        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Namespace);
        builder.HasIndex(c => c.WorkflowApplicationId);
        builder.HasIndex(c => c.CreatedOn);
    }
}