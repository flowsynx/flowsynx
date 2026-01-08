using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Application.Core.Services;

public interface IGeneExpressionEngine
{
    Task<ExpressionResult> ExpressGeneAsync(
        GeneInstance gene,
        CellularEnvironment cellularEnvironment,
        Dictionary<string, object> sharedContext);

    Task<List<ExpressionResult>> ExpressChromosomeAsync(
        Chromosome chromosome,
        Dictionary<string, object> runtimeContext);
}