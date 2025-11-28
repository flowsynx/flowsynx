using FlowSynx.Application.Configuration.System.Logger;
using FlowSynx.Infrastructure.Logging.ConsoleLogger;
using FlowSynx.Infrastructure.Logging.DatabaseLogger;
using FlowSynx.Infrastructure.Logging.FileLogger;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging;

public class CompositeLoggingProviderFactory : ILoggingProviderFactory
{
    private readonly IReadOnlyDictionary<string, ILogProviderBuilder> _builders;

    public CompositeLoggingProviderFactory(IServiceProvider serviceProvider)
    {
        _builders = new Dictionary<string, ILogProviderBuilder>(StringComparer.OrdinalIgnoreCase)
        {
            { "console",  new SerilogConsoleProviderBuilder() },
            { "database", new SerilogDatabaseProviderBuilder(serviceProvider) },
            { "file",     new SerilogFileProviderBuilder() }
        };
    }

    public ILoggerProvider? Create(
        string name,
        LoggerProviderConfiguration config)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        return _builders.TryGetValue(name, out var builder)
            ? builder.Build(name, config)
            : null;
    }
}