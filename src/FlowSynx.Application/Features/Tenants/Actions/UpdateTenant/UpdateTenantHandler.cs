using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Tenants;
using Void = FlowSynx.Application.Core.Dispatcher.Void;

namespace FlowSynx.Application.Features.Tenants.Actions.UpdateTenant;

internal class UpdateTenantHandler : IActionHandler<UpdateTenantRequest, Result<Void>>
{
    private readonly ILogger<UpdateTenantHandler> _logger;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTenantHandler(
        ILogger<UpdateTenantHandler> logger,
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<Void>> Handle(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var tenantId = TenantId.Create(request.TenantId);
            var existingTenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
            if (existingTenant == null)
            {
                throw new TenantNotFoundException(tenantId);
            }

            if (!Enum.TryParse<TenantStatus>(request.Status, true, out var tenantStatus))
                throw new TenantInvalidStatusException(request.Name, request.Status);

            existingTenant.UpdateName(request.Name);
            existingTenant.UpdateDescription(request.Description);
            switch (tenantStatus)
            {
                case TenantStatus.Active:
                    existingTenant.Activate();
                    break;
                case TenantStatus.Suspended:
                    existingTenant.Suspend("Updated via UpdateTenant action");
                    break;
                case TenantStatus.Terminated:
                    existingTenant.Terminate("Updated via UpdateTenant action");
                    break;
                default:
                    throw new TenantInvalidStatusException(request.Name, request.Status);
            }

            await _tenantRepository.UpdateAsync(existingTenant, cancellationToken);
            return await Result<Void>.SuccessAsync(string.Format(ApplicationResources.Feature_Tenant_UpdatedSuccessfully));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<Void>.FailAsync(ex.ToString());
        }
    }
}