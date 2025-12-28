using FlowSynx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Persistence.Sqlite.Configurations;

public class LoggerConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable("LogEntries");

        builder.HasKey(x => x.Id);

        builder.Property(t => t.Id)
               .IsRequired();

        builder.Property(t => t.Message)
               .IsRequired();

        builder.Property(t => t.TimeStamp)
               .IsRequired();

        var levelConverter = new ValueConverter<LogLevel, string>(
            v => v.ToString(),
            v => (LogLevel)Enum.Parse(typeof(LogLevel), v, true)
        );

        builder.Property(t => t.Level)
               .IsRequired()
               .HasConversion(levelConverter);
    }
}