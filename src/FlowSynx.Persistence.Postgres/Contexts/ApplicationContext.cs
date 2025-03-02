using Microsoft.EntityFrameworkCore;
using FlowSynx.Core.Services;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Entities.PluignConfig;
using FlowSynx.Persistence.Postgres.Configurations;

namespace FlowSynx.Persistence.Postgres.Contexts;

public class ApplicationContext : AuditableContext
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ISystemClock _systemClock;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;

    public ApplicationContext(DbContextOptions<ApplicationContext> contextOptions,
        ICurrentUserService currentUserService, ISystemClock systemClock,
        IJsonSerializer jsonSerializer, IJsonDeserializer jsonDeserializer)
        : base(contextOptions)
    {
        _currentUserService = currentUserService;
        _systemClock = systemClock;
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
    }

    public DbSet<PluginConfiguration> PluginConfiguration { get; set; }
    public DbSet<WorkflowDefination> Workflows { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        ApplyAuditing();

        if ( string.IsNullOrEmpty(_currentUserService.UserId))
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

        return await base.SaveChangesAsync(_currentUserService.UserId, cancellationToken);
    }

    private void ApplyAuditing()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var auditable = (IAuditableEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                auditable.CreatedOn = _systemClock.NowUtc;
                auditable.CreatedBy = _currentUserService.UserId;
            }

            if (entry.State == EntityState.Modified)
            {
                auditable.LastModifiedOn = _systemClock.NowUtc;
                auditable.LastModifiedBy = _currentUserService.UserId;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new PluginConfigConfiguration(_jsonSerializer, _jsonDeserializer));
        builder.ApplyConfiguration(new WorkflowDefinationfiguration());
        builder.HasDefaultSchema("FlowSynx");
    }
}