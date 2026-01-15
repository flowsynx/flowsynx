using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genes.Actions.RegisterGene;

public class RegisterGeneRequest : IRequest<Result<RegisterGeneResult>>
{
    public required object Json { get; set; }
}