using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genes;

namespace FlowSynx.Application.Core.Services;

public interface IGeneExecutor
{
    Task<object> ExecuteAsync(
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context);

    bool CanExecute(ExecutableComponent executable);
}