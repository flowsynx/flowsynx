using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities.PluginConfig;
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
                IEnumerable<PluginConfigurationEntity> configs = new List<PluginConfigurationEntity>()
                    {
                        new PluginConfigurationEntity { 
                            Id= Guid.NewGuid(),
                            UserId = "dddd",
                            Name = "azblob", 
                            Type= "flowsynx.connectors/Azure.Blob", 
                            Specifications = new()}
                    };
                _appContext.PluginConfiguration.AddRange(configs);
            }
        }).GetAwaiter().GetResult();
    }
}