using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;
using Void = FlowSynx.Application.Core.Dispatcher.Void;

namespace FlowSynx.Application.Features.Chromosomes.Actions.DeleteChromosome;

public class DeleteChromosomeRequest : IRequest<Result<Void>>
{
    public required Guid Id { get; set; }
}