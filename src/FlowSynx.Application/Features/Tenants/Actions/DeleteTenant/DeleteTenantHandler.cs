using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Features.Tenants.Actions.DeleteTenant;

internal class DeleteTenantHandler : IActionHandler<DeleteTenantRequest, Result<DeleteTenantResult>>
{
    private readonly ILogger<DeleteTenantHandler> _logger;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteTenantHandler(
        ILogger<DeleteTenantHandler> logger,
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<DeleteTenantResult>> Handle(DeleteTenantRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var tenantId = TenantId.Create(request.tenantId);
            await _tenantRepository.DeleteAsync(tenantId, cancellationToken);

            var response = new DeleteTenantResult
            {
                TenantId = tenantId
            };

            return await Result<DeleteTenantResult>.SuccessAsync(response,
                string.Format(ApplicationResources.Feature_Tenant_DeletedSuccessfully));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<DeleteTenantResult>.FailAsync(ex.ToString());
        }
    }
}
