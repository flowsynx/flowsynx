using FlowSynx.Domain.GeneBlueprints;

namespace FlowSynx.Application.Core.Services;

public interface IGeneBlueprintRegistry
{
    Task RegisterBlueprintAsync(GeneBlueprint blueprint);
    Task<GeneBlueprint> GetBlueprintAsync(string geneId, string version = null);
    Task<List<GeneBlueprint>> SearchBlueprintsAsync(string searchTerm);
    Task<bool> UnregisterBlueprintAsync(string geneId, string version);
    Task<bool> IsCompatibleAsync(string geneId, string runtimeVersion, string platform);
}