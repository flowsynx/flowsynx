using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genes.Actions.CreateGene;

public class CreateGeneRequest : IRequest<Result<CreateGeneResult>>
{
    public required object Json { get; set; }
}