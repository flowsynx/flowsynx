using FlowSynx.Application.Serializations;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Sqlite.Configurations;

public class ChromosomeConfiguration : IEntityTypeConfiguration<Chromosome>
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public ChromosomeConfiguration(ISerializer jsonSerializer, IDeserializer jsonDeserializer)
    {
        _serializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
        _deserializer = jsonDeserializer ?? throw new ArgumentNullException(nameof(jsonDeserializer));
    }

    public void Configure(EntityTypeBuilder<Chromosome> builder)
    {
        builder.ToTable("Chromosomes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => new ChromosomeId(value));

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);

        builder.Property(c => c.CellularEnvironment)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<CellularEnvironment>(v));

        var dictionaryComparer = new ValueComparer<Dictionary<string, object>>(
            (l, r) =>
                ReferenceEquals(l, r) ||
                (l != null && r != null && _serializer.Serialize(l) == _serializer.Serialize(r)),
            d => d == null ? 0 : _serializer.Serialize(d).GetHashCode(),
            d => d == null ? null : _deserializer.Deserialize<Dictionary<string, object>>(_serializer.Serialize(d)));

        builder.Property(c => c.Metadata)
            .HasConversion(
                v => _serializer.Serialize(v),
                v => _deserializer.Deserialize<Dictionary<string, object>>(v))
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