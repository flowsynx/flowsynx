using Microsoft.EntityFrameworkCore;
using FlowSynx.Application.Services;
using FlowSynx.Persistence.Core.Postgres.Configurations;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using FlowSynx.PluginCore.Exceptions;
using FlowSynx.Application.Models;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.Domain.PluginConfig;
using FlowSynx.Domain.Plugin;
using FlowSynx.Domain;
using FlowSynx.Application.Serialization;

namespace FlowSynx.Persistence.Core.Postgres.Contexts;

public class ApplicationContext : AuditableContext
{
    private readonly ILogger<ApplicationContext> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISystemClock _systemClock;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IJsonDeserializer _jsonDeserializer;
    private readonly IEncryptionService _encryptionService;

    public ApplicationContext(DbContextOptions<ApplicationContext> contextOptions,
        ILogger<ApplicationContext> logger, IHttpContextAccessor httpContextAccessor, 
        ISystemClock systemClock, IJsonSerializer jsonSerializer, 
        IJsonDeserializer jsonDeserializer,
        IEncryptionService encryptionService)
        : base(contextOptions)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(systemClock);
        ArgumentNullException.ThrowIfNull(jsonSerializer);
        ArgumentNullException.ThrowIfNull(jsonDeserializer);
        ArgumentNullException.ThrowIfNull(encryptionService);
        ArgumentNullException.ThrowIfNull(logger);
        _httpContextAccessor = httpContextAccessor;
        _systemClock = systemClock;
        _jsonSerializer = jsonSerializer;
        _jsonDeserializer = jsonDeserializer;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public DbSet<PluginEntity> Plugins { get; set; }
    public DbSet<PluginConfigurationEntity> PluginConfiguration { get; set; }
    public DbSet<WorkflowEntity> Workflows { get; set; }
    public DbSet<WorkflowExecutionEntity> WorkflowExecutions { get; set; }
    public DbSet<WorkflowTaskExecutionEntity> WorkflowTaskExecutions { get; set; }
    public DbSet<WorkflowApprovalEntity> WorkflowApprovals { get; set; }
    public DbSet<WorkflowTriggerEntity> WorkflowTriggeres { get; set; }
    public DbSet<WorkflowQueueEntity> WorkflowQueue { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        try
        {
            HandleSoftDelete();
            ApplyAuditing();

            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return await base.SaveChangesAsync(cancellationToken);

            return await base.SaveChangesAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseSaveData, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private void HandleSoftDelete()
    {
        try
        {
            var entries = ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted && e.Entity is ISoftDeletable);
            foreach (var entry in entries)
            {
                entry.State = EntityState.Modified;
                ((ISoftDeletable)entry.Entity).IsDeleted = true;
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseDeleteData, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private void ApplyAuditing()
    {
        try
        {
            var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditableEntity &&
                (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            );

            foreach (var entry in entries)
            {
                var auditable = (IAuditableEntity)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedOn = _systemClock.UtcNow;
                    auditable.CreatedBy = GetUserId();
                }

                if (entry.State == EntityState.Modified)
                {
                    auditable.LastModifiedOn = _systemClock.UtcNow;
                    auditable.LastModifiedBy = GetUserId();
                }
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.AuditsApplying, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private string GetUserId() => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        try
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new PluginEntityConfiguration(_jsonSerializer, _jsonDeserializer));
            modelBuilder.ApplyConfiguration(new PluginConfigEntityConfiguration(_jsonSerializer, _jsonDeserializer, _encryptionService));
            modelBuilder.ApplyConfiguration(new WorkflowEntityConfiguration(_encryptionService));
            modelBuilder.ApplyConfiguration(new WorkflowExecutionEntityConfiguration(_encryptionService));
            modelBuilder.ApplyConfiguration(new WorkflowTaskExecutionEntityConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowApprovalEntityConfiguration());
            modelBuilder.ApplyConfiguration(new WorkflowTriggerEntityConfiguration(_jsonSerializer, _jsonDeserializer));
            modelBuilder.ApplyConfiguration(new WorkflowQueueEntityConfiguration());
            modelBuilder.HasDefaultSchema("FlowSynx");
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseModelCreating, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}