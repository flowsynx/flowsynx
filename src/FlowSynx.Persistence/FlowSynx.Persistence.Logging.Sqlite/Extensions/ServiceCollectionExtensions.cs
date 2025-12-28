using FlowSynx.Domain.Repositories;
using FlowSynx.Persistence.Logging.Sqlite.Contexts;
using FlowSynx.Persistence.Logging.Sqlite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Persistence.Logging.Sqlite.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqLiteLoggerLayer(this IServiceCollection services)
    {
        const string connectionString = "Data Source=logs.db";

        services
            .AddSingleton<ILoggerService, LoggerService>()
            .AddDbContextFactory<LoggerContext>(options =>
            {
                options.UseSqlite(connectionString);
            });
        return services;
    }
}