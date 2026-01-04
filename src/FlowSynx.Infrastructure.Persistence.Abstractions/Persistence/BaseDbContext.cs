using FlowSynx.Application.Abstractions.Services;
using FlowSynx.Domain.AuditTrails;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.TenantContacts;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Domain.TenantSecrets;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FlowSynx.Infrastructure.Persistence.Abstractions;

public abstract class BaseDbContext : DbContext, IDatabaseContext
{
    protected readonly ILogger<BaseDbContext> Logger;
    protected readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISystemClock _systemClock;

    private string _userId;
    private TenantId _tenantId;
    private string _userIpAddress;
    private string _userAgent;
    private string _endpoint;

    protected BaseDbContext(
            DbContextOptions options,
            ILogger<BaseDbContext> logger,
            IHttpContextAccessor httpContextAccessor,
            ISystemClock systemClock): base(options)
    {
        Logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantSecret> TenantSecrets { get; set; }
    public DbSet<TenantSecretConfig> TenantSecretConfigs { get; set; }
    public DbSet<TenantContact> TenantContacts { get; set; }
    public DbSet<GeneBlueprint> GeneBlueprints { get; set; }
    public DbSet<Chromosome> Chromosomes { get; set; }
    public DbSet<Genome> Genomes { get; set; }
    public DbSet<GeneInstance> GeneInstances { get; set; }
    public DbSet<AuditTrail> AuditTrails { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            SetCurrentUserIdAndTenantId();
            ApplyTenantScope();
            ApplyAuditing();

            return string.IsNullOrEmpty(_userId)
                ? await base.SaveChangesAsync(cancellationToken)
                : await SaveChangesAsync(_tenantId, _userId, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseSaveData, ex.Message);
            Logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        try
        {
            base.OnModelCreating(modelBuilder);
        }
        catch (Exception ex)
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.DatabaseModelCreating, ex.Message);
            Logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    private TenantId CurrentTenantId()
    {
        var TenantIdClaimType = "tenant_id";
        var TenantIdHeaderName = "X-Tenant-Id";
        var httpContext = _httpContextAccessor.HttpContext;

        // Extract claim and header (do not use out-of-scope variables later)
        var claimValue = httpContext?.User?.Identity?.IsAuthenticated == true
            ? httpContext!.User.FindFirst(TenantIdClaimType)?.Value
            : null;

        _ = Guid.TryParse(claimValue, out var claimTenantId);

        var headerValue = httpContext?.Request.Headers[TenantIdHeaderName].FirstOrDefault();
        _ = Guid.TryParse(headerValue, out var headerTenantId);

        // Decide source of truth
        var resolvedTenantId = ResolveTenantId(claimTenantId, headerTenantId);
        return resolvedTenantId == Guid.Empty ? TenantId.Create(Guid.Parse("00000000-0000-0000-0000-000000000001")) : TenantId.Create(resolvedTenantId);
    }

    private static Guid ResolveTenantId(Guid claimTenantId, Guid headerTenantId)
    {
        // If authenticated, require claim and optionally enforce match with header
        if (claimTenantId != Guid.Empty)
        {
            // If header present and mismatched, fail-fast
            if (headerTenantId != Guid.Empty && headerTenantId != claimTenantId)
            {
                return Guid.Empty;
            }

            return claimTenantId;
        }

        // Allow header for anonymous/service calls if claim not present
        if (headerTenantId != Guid.Empty)
        {
            return headerTenantId;
        }

        return Guid.Empty;
    }

    private void SetCurrentUserIdAndTenantId()
    {
        _userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
        _tenantId = CurrentTenantId();
        _userIpAddress = GetClientIpAddress() ?? "Unknown";
        _userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
        _endpoint = _httpContextAccessor.HttpContext?.Request.Path.ToString() ?? "Unknown";
    }

    private void ApplyTenantScope()
    {
        if (_tenantId == null)
            return;

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added)
            {
                // Ensure new entities get the current tenant ID
                entry.Entity.TenantId = CurrentTenantId();
            }
            else if (entry.State == EntityState.Modified)
            {
                // Prevent changing tenant ID on existing entities
                entry.Property(nameof(ITenantScoped.TenantId)).IsModified = false;
            }
        }
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return null;

        // Try to get IP from various headers
        var headers = new[] { "X-Forwarded-For", "X-Real-IP" };
        foreach (var header in headers)
        {
            if (httpContext.Request.Headers.TryGetValue(header, out var value))
            {
                var ip = value.ToString().Split(',')[0].Trim();
                if (!string.IsNullOrEmpty(ip))
                    return ip;
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString();
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
            Logger.LogError(ex, errorMessage.ToString());
            throw new FlowSynxException(errorMessage);
        }
    }

    public virtual async Task<int> SaveChangesAsync(
        TenantId tenantId,
        string userId,
        CancellationToken cancellationToken = new())
    {
        var auditEntries = OnBeforeSaveChanges(tenantId, userId);
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditTrailEntry> OnBeforeSaveChanges(TenantId tenantId, string userId)
    {
        ChangeTracker.DetectChanges();

        var auditEntries = new List<AuditTrailEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (ShouldSkipEntry(entry))
                continue;

            var auditEntry = CreateAuditEntry(entry, tenantId, userId);
            auditEntries.Add(auditEntry);

            ProcessEntryProperties(entry, auditEntry);
        }

        AddCompletedAuditEntries(auditEntries);

        return auditEntries
            .Where(e => e.HasTemporaryProperties)
            .ToList();
    }

    private static bool ShouldSkipEntry(EntityEntry entry)
    {
        return entry.Entity is AuditTrailEntry
            || entry.State is EntityState.Detached
            || entry.State is EntityState.Unchanged;
    }

    private AuditTrailEntry CreateAuditEntry(
        EntityEntry entry,
        TenantId tenantId,
        string userId)
    {
        return new AuditTrailEntry(entry, userId, tenantId, entry.Entity.GetType().Name);
    }

    private void ProcessEntryProperties(
        EntityEntry entry,
        AuditTrailEntry auditEntry)
    {
        foreach (var property in entry.Properties)
        {
            if (property.IsTemporary)
            {
                auditEntry.TemporaryProperties.Add(property);
                continue;
            }

            var propertyName = property.Metadata.Name;

            if (property.Metadata.IsPrimaryKey())
            {
                auditEntry.KeyValues[propertyName] = property.CurrentValue!;
                continue;
            }

            HandlePropertyChange(entry, auditEntry, property, propertyName);
        }
    }

    private void HandlePropertyChange(
        EntityEntry entry,
        AuditTrailEntry auditEntry,
        PropertyEntry property,
        string propertyName)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                auditEntry.AuditTrailType = AuditTrailType.Create;
                auditEntry.Ipaddress = _userIpAddress;
                auditEntry.UserAgent = _userAgent;
                auditEntry.Endpoint = _endpoint;
                auditEntry.NewValues[propertyName] = property.CurrentValue!;
                break;

            case EntityState.Deleted:
                auditEntry.AuditTrailType = AuditTrailType.Delete;
                auditEntry.OldValues[propertyName] = property.OriginalValue!;
                break;

            case EntityState.Modified:
                if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                {
                    auditEntry.ChangedColumns.Add(propertyName);
                    auditEntry.AuditTrailType = AuditTrailType.Update;
                    auditEntry.OldValues[propertyName] = property.OriginalValue;
                    auditEntry.NewValues[propertyName] = property.CurrentValue!;
                }
                break;

            default:
                auditEntry.AuditTrailType = AuditTrailType.None;
                break;
        }
    }

    private void AddCompletedAuditEntries(IEnumerable<AuditTrailEntry> auditEntries)
    {
        foreach (var auditEntry in auditEntries.Where(e => !e.HasTemporaryProperties))
        {
            AuditTrails.Add(auditEntry.ToAudit());
        }
    }

    private Task OnAfterSaveChanges(
        List<AuditTrailEntry>? auditEntries,
        CancellationToken cancellationToken = new())
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
            }
            AuditTrails.Add(auditEntry.ToAudit());
        }
        return SaveChangesAsync(cancellationToken);
    }
}