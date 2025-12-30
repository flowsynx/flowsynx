using FlowSynx.Application.Services;
using FlowSynx.Domain.Entities;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Services;

public class TenantService : ITenantService
{
    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly ILogger<TenantService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;
    private Tenant? _currentTenant;

    public TenantService(
        IDbContextFactory<SqliteApplicationContext> appContextFactory,
        ILogger<TenantService> logger,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public Guid? GetCurrentTenantId() => GetTenantId();

    private Guid? GetTenantId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            var tenantClaim = httpContext.User.FindFirst("tenant_id");
            if (Guid.TryParse(tenantClaim?.Value, out var tenantId))
                return tenantId;
        }

        // Fallback to header for service-to-service calls
        var tenantHeader = httpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (Guid.TryParse(tenantHeader, out var headerTenantId))
            return headerTenantId;

        return null;
    }

    public async Task<Tenant?> GetCurrentTenantAsync(CancellationToken cancellationToken)
    {
        if (_currentTenant != null)
            return _currentTenant;

        var tenantId = GetCurrentTenantId();
        if (!tenantId.HasValue)
            throw new UnauthorizedAccessException("Tenant not identified");

        var cacheKey = $"tenant_{tenantId}";

        _currentTenant = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

            await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

            if (tenant == null || !tenant.IsActive)
                throw new UnauthorizedAccessException("Tenant not found or inactive");

            return tenant;
        });

        return _currentTenant!;
    }

    public async Task<bool> SetCurrentTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        var tenant = await context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);

        if (tenant == null)
            return false;

        _currentTenant = tenant;
        return true;
    }
}