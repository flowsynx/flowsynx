using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs.Networking;

namespace FlowSynx.Application.Tenancy;

public sealed class TenantContextAccessor : ITenantContext
{
    private static readonly AsyncLocal<TenantContextHolder> _current = new();

    private static TenantContextHolder Current
    {
        get => _current.Value ??= new TenantContextHolder(new TenantContext());
        set => _current.Value = value;
    }

    public TenantId TenantId
    {
        get => Current.Context.TenantId;
        set => Current.Context.TenantId = value;
    }

    public bool IsValid
    {
        get => Current.Context.IsValid;
        set => Current.Context.IsValid = value;
    }

    public TenantStatus Status
    {
        get => Current.Context.Status;
        set => Current.Context.Status = value;
    }

    public TenantCorsPolicy? CorsPolicy
    {
        get => Current.Context.CorsPolicy;
        set => Current.Context.CorsPolicy = value;
    }

    public TenantRateLimitingPolicy? RateLimitingPolicy
    {
        get => Current.Context.RateLimitingPolicy;
        set => Current.Context.RateLimitingPolicy = value;
    }

    public string UserId
    {
        get => Current.Context.UserId;
        set => Current.Context.UserId = value;
    }

    public string? UserAgent
    {
        get => Current.Context.UserAgent;
        set => Current.Context.UserAgent = value;
    }

    public string? IPAddress
    {
        get => Current.Context.IPAddress;
        set => Current.Context.IPAddress = value;
    }

    public string? Endpoint
    {
        get => Current.Context.Endpoint;
        set => Current.Context.Endpoint = value;
    }

    public static void Set(TenantContext newContext)
    {
        Current = new TenantContextHolder(newContext ?? new TenantContext());
    }

    private sealed class TenantContextHolder
    {
        public TenantContext Context { get; }
        public TenantContextHolder(TenantContext context) => Context = context;
    }
}