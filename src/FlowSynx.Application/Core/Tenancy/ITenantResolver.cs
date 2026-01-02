namespace FlowSynx.Application.Core.Tenancy;

public interface ITenantResolver
{
    Task<TenantResolutionResult> ResolveAsync(CancellationToken ct = default);
}