using FlowSynx.Application.Services;
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
        _appContext.SaveChanges();
    }
}