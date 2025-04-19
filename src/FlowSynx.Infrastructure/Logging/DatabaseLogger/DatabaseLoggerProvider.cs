using FlowSynx.Domain.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Logging.DatabaseLogger;

[ProviderAlias("Database")]
public class DatabaseLoggerProvider : ILoggerProvider
{
    private readonly DatabaseLoggerOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILoggerService _loggerService;

    public DatabaseLoggerProvider(DatabaseLoggerOptions options, IHttpContextAccessor httpContextAccessor,
        ILoggerService loggerService)
    {
        _httpContextAccessor = httpContextAccessor;
        _loggerService = loggerService;
        _options = options;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DatabaseLogger(categoryName, _options, _httpContextAccessor, _loggerService);
    }

    public void Dispose()
    {

    }
}