using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Activities.Requests.ActivityDetails;

public class ActivityDetailsRequest : IAction<Result<ActivityDetailsResult>>
{
    public Guid Id { get; set; }
}
