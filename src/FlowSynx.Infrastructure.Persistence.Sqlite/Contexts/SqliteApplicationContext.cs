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
    //private readonly ITenantService _tenantService;
    private Guid? _tenantId;
    private string? _userId;

    public SqliteApplicationContext(
        DbContextOptions<SqliteApplicationContext> contextOptions,
        ILogger<SqliteApplicationContext> logger,
        IHttpContextAccessor httpContextAccessor,
        ISystemClock systemClock,
        ISerializer serializer,
        IDeserializer deserializer,
        IEncryptionService encryptionService)
        //ITenantService tenantService)
        : base(contextOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        //_tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
    }

    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<TenantConfiguration> TenantConfigurations { get; set; } = null!;
    public DbSet<GeneBlueprint> GeneBlueprints { get; set; } = null!;
    public DbSet<Chromosome> Chromosomes { get; set; } = null!;
    public DbSet<Genome> Genomes { get; set; } = null!;
    public DbSet<GeneInstance> GeneInstances { get; set; } = null!;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SetCurrentUserAndTenantAsync();
            ApplyTenantScope();
            ApplyAuditing();

            return string.IsNullOrEmpty(_userId)
                ? await base.SaveChangesAsync(cancellationToken)
                : await base.SaveChangesAsync(_userId, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseSaveData, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        try
        {
            base.OnModelCreating(modelBuilder);

            //SetTenantFilters(modelBuilder);
            ApplyEntityConfigurations(modelBuilder);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseModelCreating, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private async Task SetCurrentUserAndTenantAsync()
    {
        //if (_tenantId == null)
        //{
        //    _tenantId = _tenantService.GetCurrentTenantId();
        //}

        if (_userId == null)
        {
            _userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
        }
    }

    private void ApplyTenantScope()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _tenantId.GetValueOrDefault();
            }
        }
    }

    private void ApplyAuditing()
    {
        try
        {
            var entries = ChangeTracker.Entries<AuditableEntity>()
                .Where(e => e.State == EntityState.Added ||
                           e.State == EntityState.Modified ||
                           e.State == EntityState.Deleted);

            foreach (var entry in entries)
            {
                var auditable = entry.Entity;

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedOn = _systemClock.UtcNow;
                        auditable.CreatedBy = _userId;
                        break;

                    case EntityState.Modified:
                        auditable.LastModifiedOn = _systemClock.UtcNow;
                        auditable.LastModifiedBy = _userId;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.AuditsApplying, ex.Message);
            _logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    //private void SetTenantFilters(ModelBuilder modelBuilder)
    //{
    //    var tenantScopedTypes = modelBuilder.Model.GetEntityTypes()
    //        .Where(t => typeof(ITenantScoped).IsAssignableFrom(t.ClrType))
    //        .Select(t => t.ClrType)
    //        .ToList();

    //    foreach (var entityType in tenantScopedTypes)
    //    {
    //        var method = typeof(SqliteApplicationContext)
    //            .GetMethod(nameof(SetTenantFilter),
    //                BindingFlags.NonPublic | BindingFlags.Static,
    //                new[] { typeof(ModelBuilder) })!
    //            .MakeGenericMethod(entityType);

    //        method.Invoke(null, new object[] { modelBuilder });
    //    }
    //}

    //private static void SetTenantFilter<TEntity>(ModelBuilder builder)
    //    where TEntity : class, ITenantScoped
    //{
    //    builder.Entity<TEntity>()
    //        .HasQueryFilter(e => EF.Property<Guid>(e, "TenantId") == Guid.Empty);
    //    // Note: Actual tenant ID should be dynamically applied at query time
    //    // Consider using a query filter service for runtime tenant filtering
    //}

    private void ApplyEntityConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new GeneBlueprintConfiguration(_serializer, _deserializer));
        modelBuilder.ApplyConfiguration(new GeneInstanceConfiguration(_serializer, _deserializer));
        modelBuilder.ApplyConfiguration(new ChromosomeConfiguration(_serializer, _deserializer));
        modelBuilder.ApplyConfiguration(new GenomeConfiguration(_serializer, _deserializer));
    }
}