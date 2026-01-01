namespace FlowSynx.Application.Tenancy;

public interface ITenantResolver
{
    Task<TenantResolutionResult> ResolveAsync(CancellationToken ct = default);
}