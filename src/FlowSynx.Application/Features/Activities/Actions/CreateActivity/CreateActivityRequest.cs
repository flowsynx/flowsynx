using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Activities.Actions.CreateActivity;

public class CreateActivityRequest : IRequest<Result<CreateActivityResult>>
{
    public required object Json { get; set; }
}