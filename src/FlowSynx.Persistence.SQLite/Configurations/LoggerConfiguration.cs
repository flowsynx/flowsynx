using FlowSynx.Domain.Entities.Log;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlowSynx.Persistence.SQLite.Configurations;

public class LoggerConfiguration : IEntityTypeConfiguration<LogEntity>
{
    public void Configure(EntityTypeBuilder<LogEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(t => t.Id).IsRequired();
        builder.Property(t => t.Message).IsRequired();
        builder.Property(t => t.TimeStamp).IsRequired();

        var levelConverter = new ValueConverter<LogsLevel, string>(
            v => v.ToString(),
            v => (LogsLevel)Enum.Parse(typeof(LogsLevel), v, true)
        );

        builder.Property(t => t.Level).IsRequired().HasConversion(levelConverter);
    }
}