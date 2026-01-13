using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Execute;

public class ExecuteRequest : IRequest<Result<ExecutionResponse>>
{
    public required object Json { get; set; }
}