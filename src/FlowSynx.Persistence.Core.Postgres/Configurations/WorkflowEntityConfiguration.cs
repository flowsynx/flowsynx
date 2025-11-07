using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using FlowSynx.Domain.Workflow;
using FlowSynx.Application.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlowSynx.Persistence.Core.Postgres.Configurations;

public class WorkflowEntityConfiguration : IEntityTypeConfiguration<WorkflowEntity>
{
    private readonly IEncryptionService _encryptionService;

    public WorkflowEntityConfiguration(IEncryptionService encryptionService)
    {
        ArgumentNullException.ThrowIfNull(encryptionService);
        _encryptionService = encryptionService;
    }

    public void Configure(EntityTypeBuilder<WorkflowEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(t => t.Id).IsRequired();

        builder.Property(t => t.UserId)
               .IsRequired();

        builder.Property(t => t.Name)
               .HasColumnType("citext")
               .IsRequired()
               .HasMaxLength(128);

        var stringConverter = new ValueConverter<string, string>(
            v => _encryptionService.Encrypt(v),
            v => _encryptionService.Decrypt(v)
        );

        var stringComparer = new ValueComparer<string>(
            (s1, s2) => string.Equals(s1, s2, StringComparison.Ordinal),
            s => s == null ? 0 : s.GetHashCode(),
            s => s
        );

        builder.Property(t => t.Definition)
               .IsRequired()
               .HasColumnType("text")
               .HasConversion(stringConverter, stringComparer);

        builder.Property(t => t.SchemaUrl)
               .HasColumnType("text");
    }
}
