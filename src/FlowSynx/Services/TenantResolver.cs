using FlowSynx.Application.Abstractions.Persistence;
using FlowSynx.Application.Tenancy;
using FlowSynx.Domain.Tenants;
using FlowSynx.Infrastructure.Security.Secrets.Extensions;
using FlowSynx.Infrastructure.Security.Secrets.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FlowSynx.Infrastructure.Persistence.Sqlite.Services;

public sealed class TenantResolver : ITenantResolver
{
    private const string TenantIdClaimType = "tenant_id";
    private const string TenantIdHeaderName = "X-Tenant-Id";

    private readonly ITenantRepository _tenantRepository;
    private readonly ISecretProviderFactory _secretProviderFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _cache;

    public TenantResolver(
        ITenantRepository tenantRepository,
        ISecretProviderFactory secretProviderFactory,
        IHttpContextAccessor httpContextAccessor,
        IMemoryCache cache)
    {
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _secretProviderFactory = secretProviderFactory ?? throw new ArgumentNullException(nameof(secretProviderFactory));
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
        if (_cache.TryGetValue(cacheKey, out var cachedObj) && cachedObj is TenantResolutionResult cached && cached.IsValid)
        {
            return cached;
        }

        var tenantId = TenantId.Create(resolvedTenantId);

        var provider = await _secretProviderFactory.GetProviderForTenantAsync(tenantId);
        var secrets = await provider.GetSecretsAsync();

        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null || tenant.Status != TenantStatus.Active)
        {
            return new TenantResolutionResult { IsValid = false };
        }

        var tenantResult = new TenantResolutionResult
        {
            IsValid = true,
            TenantId = tenant.Id,
            Status = tenant.Status,
            CorsPolicy = secrets.GetCorsPolicy(tenantId),
            RateLimitingPolicy = secrets.GetRateLimitingPolicy()
        };

        if (tenant is null)
        {
            return new TenantResolutionResult { IsValid = false };
        }

        // Cache with a short TTL; adjust as needed
        _cache.Set(cacheKey, tenantResult, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        });

        return tenantResult;
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