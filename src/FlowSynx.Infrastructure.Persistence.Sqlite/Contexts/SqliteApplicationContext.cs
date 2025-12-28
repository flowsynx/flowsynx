using FlowSynx.Application.Serializations;
using FlowSynx.Application.Services;
using FlowSynx.Domain.Aggregates;
using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Primitives;
using FlowSynx.Infrastructure.Encryption;
using FlowSynx.Infrastructure.Persistence;
using FlowSynx.Persistence.Sqlite.Configurations;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FlowSynx.Persistence.Sqlite.Contexts;

public class SqliteApplicationContext : AuditableContext
{
    private readonly ILogger<SqliteApplicationContext> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISystemClock _systemClock;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;
    private readonly IEncryptionService _encryptionService;

    public SqliteApplicationContext(
        DbContextOptions<SqliteApplicationContext> contextOptions,
        ILogger<SqliteApplicationContext> logger, 
        IHttpContextAccessor httpContextAccessor, 
        ISystemClock systemClock, 
        ISerializer serializer, 
        IDeserializer deserializer,
        IEncryptionService encryptionService) : base(contextOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public DbSet<GeneBlueprint> GeneBlueprints { get; set; }
    public DbSet<Chromosome> Chromosomes { get; set; }
    public DbSet<Genome> Genomes { get; set; }
    public DbSet<GeneInstance> GeneInstances { get; set; }
    public DbSet<LogEntry> LogEntries { get; set; }

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
            .Where(e => e.Entity is AuditableEntity &&
                (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            );

            foreach (var entry in entries)
            {
                var auditable = (AuditableEntity)entry.Entity;

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
            modelBuilder.ApplyConfiguration(new GeneBlueprintConfiguration(_serializer, _deserializer));
            modelBuilder.ApplyConfiguration(new GeneInstanceConfiguration(_serializer, _deserializer));
            modelBuilder.ApplyConfiguration(new ChromosomeConfiguration(_serializer, _deserializer));
            modelBuilder.ApplyConfiguration(new GenomeConfiguration(_serializer, _deserializer));
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseModelCreating, ex.Message);
            _logger.LogError(errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }
}