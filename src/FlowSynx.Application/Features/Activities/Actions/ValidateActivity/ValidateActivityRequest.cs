using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Activities.Actions.ValidateActivity;

public class ValidateActivityRequest : IRequest<Result<ValidationResponse>>
{
    public required object Json { get; set; }
}