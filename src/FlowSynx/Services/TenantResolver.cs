using FlowSynx.Application.Core.Persistence;
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
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

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
        if (httpContext is null)
        {
            return Failure(TenantResolutionStatus.Error, "HTTP context is unavailable.");
        }

        var resolution = ResolveTenantId(httpContext);
        if (resolution.ResolutionStatus != TenantResolutionStatus.Active)
        {
            return resolution;
        }

        var tenantId = resolution.TenantId!;

        var cacheKey = $"tenant:resolution:{tenantId.Value:D}";
        if (_cache.TryGetValue(cacheKey, out var cachedObj) && cachedObj is TenantResolutionResult cached)
        {
            return cached;
        }

        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
            if (tenant is null)
            {
                return CacheAndReturn(
                    cacheKey,
                    Failure(TenantResolutionStatus.NotFound, "Tenant does not exist."));
            }

            var provider = await _secretProviderFactory.GetProviderForTenantAsync(tenantId);
            if (provider is null)
            {
                return Failure(
                    TenantResolutionStatus.Error,
                    "Secret provider for tenant could not be resolved.");
            }

            var secrets = await provider.GetSecretsAsync();

            var result = new TenantResolutionResult
            {
                ResolutionStatus = TenantResolutionStatus.Active,
                TenantId = tenant.Id,
                TenantStatus = tenant.Status,
                CorsPolicy = secrets.GetCorsPolicy(tenantId),
                RateLimitingPolicy = secrets.GetRateLimitingPolicy()
            };

            return CacheAndReturn(cacheKey, result);
        }
        catch (Exception ex)
        {
            return Failure(
                TenantResolutionStatus.Error,
                $"Tenant resolution failed: {ex.Message}");
        }
    }

    // ----------------------------
    // Resolution helpers
    // ----------------------------
    private TenantResolutionResult ResolveTenantId(HttpContext context)
    {
        var claimTenantId = TryGetGuid(
            context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(TenantIdClaimType)?.Value
                : null);

        var headerTenantId = TryGetGuid(
            context.Request.Headers[TenantIdHeaderName].FirstOrDefault());

        if (claimTenantId.HasValue)
        {
            if (headerTenantId.HasValue && headerTenantId != claimTenantId)
            {
                return Failure(
                    TenantResolutionStatus.Forbidden,
                    "Tenant ID header does not match authenticated tenant.");
            }

            return Success(claimTenantId.Value);
        }

        if (headerTenantId.HasValue)
        {
            return Success(headerTenantId.Value);
        }

        return Failure(
            TenantResolutionStatus.Invalid,
            "Tenant ID could not be resolved from claim or header.");
    }

    private static Guid? TryGetGuid(string? value) =>
        Guid.TryParse(value, out var guid) ? guid : null;

    // ----------------------------
    // Result helpers
    // ----------------------------
    private static TenantResolutionResult Success(Guid tenantId) =>
        new()
        {
            ResolutionStatus = TenantResolutionStatus.Active,
            TenantId = TenantId.Create(tenantId)
        };

    private static TenantResolutionResult Failure(
        TenantResolutionStatus status,
        string message) =>
        new()
        {
            ResolutionStatus = status,
            Messages = new[] { message }
        };

    private TenantResolutionResult CacheAndReturn(
        string key,
        TenantResolutionResult result)
    {
        if (result.ResolutionStatus == TenantResolutionStatus.Active)
        {
            _cache.Set(key, result, CacheDuration);
        }

        return result;
    }
}