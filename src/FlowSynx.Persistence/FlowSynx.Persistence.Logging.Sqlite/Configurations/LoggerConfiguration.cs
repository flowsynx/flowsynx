using FlowSynx.Domain.Log;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlowSynx.Persistence.Logging.Sqlite.Configurations;

public class LoggerConfiguration : IEntityTypeConfiguration<LogEntity>
{
    public void Configure(EntityTypeBuilder<LogEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Id)
               .IsRequired();

        builder.Property(t => t.Message)
               .IsRequired();

        builder.Property(t => t.TimeStamp)
               .IsRequired();

        builder.Property(t => t.Level)
               .IsRequired();
    }
}