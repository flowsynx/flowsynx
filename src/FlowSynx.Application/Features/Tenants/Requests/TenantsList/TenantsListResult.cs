using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Features.Tenants.Requests.TenantsList;

public class TenantsListResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}