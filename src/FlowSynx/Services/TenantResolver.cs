using FlowSynx.Application.Core.Tenancy;
using FlowSynx.Domain.Tenants;
using FlowSynx.Persistence.Sqlite.Contexts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Services;

public sealed class TenantResolver : ITenantResolver
{
    private const string TenantIdClaimType = "tenant_id";
    private const string TenantIdHeaderName = "X-Tenant-Id";

    private readonly IDbContextFactory<SqliteApplicationContext> _appContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;

    public TenantResolver(
        IDbContextFactory<SqliteApplicationContext> appContextFactory,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache)
    {
        _appContextFactory = appContextFactory ?? throw new ArgumentNullException(nameof(appContextFactory));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<TenantResolutionResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
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
        if (resolvedTenantId == Guid.Empty)
        {
            return new TenantResolutionResult { IsValid = false };
        }

        // Cache by tenant id to avoid repeated DB lookups
        var cacheKey = $"tenants:active:{resolvedTenantId:D}";
        if (_cache.TryGetValue(cacheKey, out TenantResolutionResult cached) && cached.IsValid)
        {
            return cached;
        }

        var tenantId = TenantId.Create(resolvedTenantId);
        await using var context = await _appContextFactory.CreateDbContextAsync(cancellationToken);
        var tenant = await context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId && t.Status == TenantStatus.Active)
            .Select(t => new TenantResolutionResult
            {
                IsValid = true,
                TenantId = t.Id,
                Status = t.Status,
                AuthenticationMode = t.Configuration.Security.Authentication.Mode
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return new TenantResolutionResult { IsValid = false };
        }

        // Cache with a short TTL; adjust as needed
        _cache.Set(cacheKey, tenant, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        });

        return tenant;
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
}
