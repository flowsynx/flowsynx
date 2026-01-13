using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;

namespace FlowSynx.Application.Core.Services;

public interface IGeneExecutor
{
    Task<object> ExecuteAsync(
        GeneBlueprintJson blueprint,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context);

    bool CanExecute(ExecutableComponent executable);
}