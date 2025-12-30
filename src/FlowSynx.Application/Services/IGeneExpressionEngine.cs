using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Application.Services;

public interface IGeneExpressionEngine
{
    Task<GeneExecutionResult> ExpressGeneAsync(
        GeneInstance gene,
        CellularEnvironment environment,
        Dictionary<string, object> sharedContext);

    Task<List<GeneExecutionResult>> ExpressChromosomeAsync(
        Chromosome chromosome,
        Dictionary<string, object> runtimeContext);
}