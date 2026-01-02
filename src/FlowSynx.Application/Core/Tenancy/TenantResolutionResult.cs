using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.Tenants.ValueObjects;

namespace FlowSynx.Application.Core.Tenancy;

public sealed record TenantResolutionResult
{
    public TenantId TenantId { get; init; }
    public bool IsValid { get; set; }
    public AuthenticationMode AuthenticationMode { get; init; }
    public TenantStatus Status { get; init; }
}