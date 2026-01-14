using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;
using Void = FlowSynx.Application.Core.Dispatcher.Void;

namespace FlowSynx.Application.Features.Tenants.Actions.UpdateTenant;

public class UpdateTenantRequest : UpdateTenantDefinitionRequest, IRequest<Result<Void>>
{
    public required Guid TenantId { get; set; }
}

public class UpdateTenantDefinitionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}