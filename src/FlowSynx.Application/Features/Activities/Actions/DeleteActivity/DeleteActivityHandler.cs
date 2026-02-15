using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Serializations;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;
using Void = FlowSynx.Application.Core.Dispatcher.Void;

namespace FlowSynx.Application.Features.Activities.Actions.DeleteActivity;

internal class DeleteActivityHandler : IActionHandler<DeleteActivityRequest, Result<Void>>
{
    private readonly ILogger<DeleteActivityHandler> _logger;
    private readonly ISerializer _serializer;
    private readonly IActivityRepository _activityRepository;
    private readonly ICurrentUserService _currentUserService;

    public DeleteActivityHandler(
        ILogger<DeleteActivityHandler> logger, 
        ISerializer serializer,
        IActivityRepository activityRepository,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<Result<Void>> Handle(DeleteActivityRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            await _activityRepository.DeleteAsync(
                TenantId.FromString(_currentUserService.TenantId()),
                _currentUserService.UserId(), request.Id, cancellationToken);

            return await Result<Void>.SuccessAsync(ApplicationResources.Feature_Gene_DeletedSuccessfully);
        }
        catch (ValidationException vex)
        {
            var errorMessages = vex.Errors
                .SelectMany(kvp => kvp.Value.Select(msg => $"{kvp.Key}: {msg}"))
                .ToList();
            return await Result<Void>.FailAsync(errorMessages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return await Result<Void>.FailAsync(ex.Message);
        }
    }
}