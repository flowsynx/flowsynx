using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Activities.Requests.ActivitiesList;

internal class ActivitiesListHandler : IActionHandler<ActivitiesListRequest, PaginatedResult<ActivitiesListResult>>
{
    private readonly ILogger<ActivitiesListHandler> _logger;
    private readonly IActivityRepository _activityRepository;
    private readonly ICurrentUserService _currentUserService;

    public ActivitiesListHandler(
        ILogger<ActivitiesListHandler> logger, 
        IActivityRepository activityRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<ActivitiesListResult>> Handle(ActivitiesListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var activities = await _activityRepository.GetByNamespaceAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Namespace,
                cancellationToken);

            var response = activities.Select(activity => new ActivitiesListResult
            {
                Id = activity.Id.ToString(),
                Name = activity.Name,
                Namespace = activity.Namespace,
                Version = activity.Version,
                Description = activity.Description,
                Labels = activity.Labels,
                Annotations = activity.Annotations,
                Owner = activity.Owner,
                Status = activity.Status.ToString().ToLower(),
                IsShared = activity.IsShared
            });

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Activities list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);
            return await PaginatedResult<ActivitiesListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in ActivitiesListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<ActivitiesListResult>.FailureAsync(ex.Message);
        }
    }
}
