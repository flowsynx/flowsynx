using FlowSynx.Core.Services;
using FlowSynx.Domain.Entities.PluignConfig;
using FlowSynx.Persistence.Postgres.Contexts;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Persistence.Postgres.Seeder;

public class ApplicationDataSeeder : IApplicationDataSeeder
{
    private readonly ApplicationContext _appContext;
    private readonly ILogger<ApplicationDataSeeder> _logger;

    public ApplicationDataSeeder(ApplicationContext appContext, ILogger<ApplicationDataSeeder> logger)
    {
        _appContext = appContext;
        _logger = logger;
    }

    public void Initialize()
    {
        AddConfig();
        _appContext.SaveChanges();
    }

    private void AddConfig()
    {
        Task.Run(async () =>
        {
            if (!_appContext.PluginConfiguration.Any())
            {
                IEnumerable<PluginConfiguration> configs = new List<PluginConfiguration>()
                    {
                        new PluginConfiguration { Name = "azblob", Type= "flowsynx.connectors/Azure.Blob", UserId = "dddd", Specifications = new()}
                    };
                _appContext.PluginConfiguration.AddRange(configs);
            }
        }).GetAwaiter().GetResult();
    }
}