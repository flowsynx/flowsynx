using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Application.Features.Tenants.Actions.AddTenant;

internal class AddTenantHandler : IActionHandler<AddTenantRequest, Result<AddTenantResult>>
{
    private readonly ILogger<AddTenantHandler> _logger;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;

    public AddTenantHandler(
        ILogger<AddTenantHandler> logger,
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<AddTenantResult>> Handle(AddTenantRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var existingTenant = await _tenantRepository.GetByNameAsync(request.Name, cancellationToken);
            if (existingTenant != null)
            {
                throw new TenantAlreadyExistException(request.Name);
            }

            if (!Enum.TryParse<TenantStatus>(request.Status, true, out var tenantStatus))
                throw new TenantInvalidStatusException(request.Name, request.Status);

            var tenantEntity = Tenant.Create(
                request.Name,
                request.Description,
                tenantStatus);

            await _tenantRepository.AddAsync(tenantEntity, cancellationToken);

            var response = new AddTenantResult
            {
                TenantId = tenantEntity.Id
            };

            return await Result<AddTenantResult>.SuccessAsync(response,
                string.Format(ApplicationResources.Feature_Tenant_AddedSuccessfully, tenantEntity.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            return await Result<AddTenantResult>.FailAsync(ex.ToString());
        }
    }
}
