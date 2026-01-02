using FlowSynx.Application.Core.Serializations;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace FlowSynx.Persistence.Sqlite.EntityConfigurations;

public class GenomeConfiguration : IEntityTypeConfiguration<Genome>
{
    public void Configure(EntityTypeBuilder<Genome> builder)
    {
        builder.ToTable("Genomes");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasConversion(
                id => id.Value,
                value => new GenomeId(value));

        builder.Property(c => c.TenantId)
            .HasConversion(
                id => id.Value,
                value => TenantId.Create(value))
            .IsRequired();

        builder.Property(g => g.UserId).IsRequired();
        builder.Property(g => g.Name).IsRequired().HasMaxLength(200);

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && JsonSerializer.Serialize(l) == JsonSerializer.Serialize(r)),
            d => d == null ? 0 : JsonSerializer.Serialize(d).GetHashCode(),
            d => d == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(d)));

        builder.Property(g => g.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(dictionaryComparer);

        builder.Property(g => g.SharedEnvironment)
            .HasConversion(
                v => JsonSerializer.Serialize(v),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v))
            .Metadata.SetValueComparer(dictionaryComparer);

        // Chromosomes relationship
        builder.HasMany(g => g.Chromosomes)
            .WithOne()
            .HasForeignKey("GenomeId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}