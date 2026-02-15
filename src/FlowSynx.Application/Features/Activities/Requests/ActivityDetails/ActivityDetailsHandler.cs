using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Activities.Requests.ActivityDetails;

internal class ActivityDetailsHandler : IActionHandler<ActivityDetailsRequest, Result<ActivityDetailsResult>>
{
    private readonly ILogger<ActivityDetailsHandler> _logger;
    private readonly IActivityRepository _activityRepository;
    private readonly ICurrentUserService _currentUserService;

    public ActivityDetailsHandler(
        ILogger<ActivityDetailsHandler> logger, 
        IActivityRepository activityRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<ActivityDetailsResult>> Handle(ActivityDetailsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var activity = await _activityRepository.GetByIdAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(),
                request.Id,
                cancellationToken);

            var response = new ActivityDetailsResult
            {
                Name = activity.Name,
                Namespace = activity.Namespace,
                Version = activity.Version,
                Description = activity.Description,
                Specification = activity.Specification,
                Metadata = activity.Metadata,
                Labels = activity.Labels,
                Annotations = activity.Annotations,
                Owner = activity.Owner,
                Status = activity.Status.ToString().ToLower(),
                IsShared = activity.IsShared
            };

            _logger.LogInformation(
                "Activity details retrieved successfully for activity {ActivityId}.",
                request.Id);

            return await Result<ActivityDetailsResult>.SuccessAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in ActivityDetailsHandler for activity {ActivityId}.",
                request.Id);
            return await Result<ActivityDetailsResult>.FailAsync(ex.Message);
        }
    }
}
