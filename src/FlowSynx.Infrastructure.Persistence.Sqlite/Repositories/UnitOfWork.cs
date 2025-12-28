using FlowSynx.Application.Repositories;
using FlowSynx.Domain.Primitives;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(
        IDbContextFactory<SqliteApplicationContext> appContextFactory,
        ILogger<UnitOfWork> logger)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);

        // Dispatch Domain Events before saving
        var domainEntities = context.ChangeTracker
            .Entries<Entity>()
            .Where(x => x.Entity.DomainEvents?.Any() == true)
            .ToList();

        // Dispatch events (in a real app, this would use a mediator)
        foreach (var entry in domainEntities)
        {
            // Here you would publish domain events through a mediator
            entry.Entity.ClearDomainEvents();
        }

        var result = await context.SaveChangesAsync(cancellationToken);
        return result > 0;
    }
}