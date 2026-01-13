using Microsoft.Extensions.Logging;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Tenants.Requests.TenantsList;

internal class TenantsListHandler : IActionHandler<TenantsListRequest, PaginatedResult<TenantsListResult>>
{
    private readonly ILogger<TenantsListHandler> _logger;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentUserService _currentUserService;

    public TenantsListHandler(ILogger<TenantsListHandler> logger, ITenantRepository tenantRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tenantRepository = tenantRepository ?? throw new ArgumentNullException(nameof(tenantRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<TenantsListResult>> Handle(TenantsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var tenants = await _tenantRepository.GetAllAsync(cancellationToken);
            var response = tenants.Select(tenant => new TenantsListResult
            {
                Id = tenant.Id.Value,
                Name = tenant.Name,
                Slug = tenant.Slug,
                Description = tenant.Description,
                Status = tenant.Status.ToString()
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Audit list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<TenantsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in AuditsListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<TenantsListResult>.FailureAsync(ex.Message);
        }
    }
}
