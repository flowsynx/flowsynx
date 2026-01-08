using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Application.Core.Services;

public interface IGeneExpressionEngine
{
    Task<GeneExecutionResult> ExpressGeneAsync(
        GeneInstance gene,
        EnvironmentalFactor environmentalFactor,
        Dictionary<string, object> sharedContext);

    Task<List<GeneExecutionResult>> ExpressChromosomeAsync(
        Chromosome chromosome,
        Dictionary<string, object> runtimeContext);
}