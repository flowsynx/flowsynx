using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;
using Void = FlowSynx.Application.Core.Dispatcher.Void;

namespace FlowSynx.Application.Features.Genomes.Actions.DeleteGenome;

public class DeleteGenomeRequest : IRequest<Result<Void>>
{
    public required Guid Id { get; set; }
}