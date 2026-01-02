using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class ChromosomeConfiguration : IEntityTypeConfiguration<Chromosome>
{
    public void Configure(EntityTypeBuilder<Chromosome> builder)
    {
        builder.ToTable("Chromosomes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new ChromosomeId(value));

        builder.Property(c => c.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);

        builder.Property(c => c.CellularEnvironment)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<CellularEnvironment>(v));

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        builder.Property(c => c.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(dictionaryComparer);

        // Store execution results separately (not in same table)
        builder.Ignore(c => c.ExecutionResults);

        // Genes are stored in separate table
        builder.HasMany(c => c.Genes)
            .WithOne()
            .HasForeignKey("ChromosomeId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}