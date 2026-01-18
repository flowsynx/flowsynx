using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.BuildingBlocks.Results;

namespace FlowSynx.Application.Features.Genomes.Requests.GenomeDetails;

public class GenomeDetailsRequest : IAction<Result<GenomeDetailsResult>>
{
    public Guid Id { get; set; }
}
