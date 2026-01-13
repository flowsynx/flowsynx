using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Tenants.Requests.TenantsDetails;

internal class TenantDetailsHandler : IActionHandler<TenantDetailsRequest, Result<TenantDetailsResult>>
{
    private readonly ILogger<TenantDetailsHandler> _logger;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;

    public TenantDetailsHandler(
        ILogger<TenantDetailsHandler> logger,
        ITenantRepository tenantRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<TenantDetailsResult>> Handle(TenantDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var tenant = await _tenantRepository.GetByIdAsync(TenantId.Create(request.TenantId), cancellationToken)
                ?? throw new TenantNotFoundException(request.TenantId);

            var response = new TenantDetailsResult
            {
                Id = tenant.Id.Value,
                Name = tenant.Name,
                Slug = tenant.Slug,
                Description = tenant.Description,
                Status = tenant.Status.ToString()
            };
            _logger.LogInformation("Tenant details for '{TenantId}' has been retrieved successfully.", request.TenantId);
            return await Result<TenantDetailsResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FlowSynx exception caught in TenantDetailsHandler for tenant '{TenantId}'.", request.TenantId);
            return await Result<TenantDetailsResult>.FailAsync(ex.Message);
        }
    }
}
