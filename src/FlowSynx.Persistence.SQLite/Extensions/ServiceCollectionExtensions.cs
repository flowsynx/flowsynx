using FlowSynx.Domain.Log;
using FlowSynx.Persistence.SQLite.Contexts;
using FlowSynx.Persistence.SQLite.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowSynx.Persistence.SQLite.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqLiteLoggerLayer(this IServiceCollection services)
    {
        const string connectionString = "Data Source=logs.db";

        services
            .AddScoped<ILoggerService, LoggerService>()
            .AddDbContextFactory<LoggerContext>(options =>
            {
                options.UseSqlite(connectionString);
            });
        return services;
    }
}