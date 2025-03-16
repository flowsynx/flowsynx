using Microsoft.EntityFrameworkCore;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Entities.Workflow;
using FlowSynx.Domain.Entities.PluginConfig;
using FlowSynx.Persistence.Postgres.Configurations;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using FlowSynx.Domain.Entities.Trigger;

namespace FlowSynx.Persistence.Postgres.Contexts;

public class ApplicationContext : AuditableContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISystemClock _systemClock;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;

    public ApplicationContext(DbContextOptions<ApplicationContext> contextOptions,
        IHttpContextAccessor httpContextAccessor, ISystemClock systemClock,
        IJsonSerializer jsonSerializer, IJsonDeserializer jsonDeserializer)
        : base(contextOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _systemClock = systemClock;
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
    }

    public DbSet<PluginConfigurationEntity> PluginConfiguration { get; set; }
    public DbSet<WorkflowEntity> Workflows { get; set; }
    public DbSet<WorkflowExecutionEntity> WorkflowExecutions { get; set; }
    public DbSet<WorkflowTaskExecutionEntity> WorkflowTaskExecutions { get; set; }
    public DbSet<WorkflowTriggerEntity> WorkflowTriggeres { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        ApplyAuditing();

        if ( string.IsNullOrEmpty(GetUserId()))
            return await base.SaveChangesAsync(cancellationToken);

        return await base.SaveChangesAsync(GetUserId(), cancellationToken);
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
                auditable.CreatedBy = GetUserId();
            }

            if (entry.State == EntityState.Modified)
            {
                auditable.LastModifiedOn = _systemClock.NowUtc;
                auditable.LastModifiedBy = GetUserId();
            }
        }
    }

    private string GetUserId() => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new PluginConfigEntityConfiguration(_jsonSerializer, _jsonDeserializer));
        builder.ApplyConfiguration(new WorkflowEntityfiguration());
        builder.ApplyConfiguration(new WorkflowExecutionEntityConfiguration());
        builder.ApplyConfiguration(new WorkflowTaskExecutionEntityConfiguration());
        builder.HasDefaultSchema("FlowSynx");
    }
}