using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Models;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genes.Actions.ValidateGene;

public class ValidateGeneRequest : IRequest<Result<ValidationResponse>>
{
    public required object Json { get; set; }
}