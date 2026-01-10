using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;

namespace FlowSynx.Application.Core.Services;

public abstract class GeneExpressor
{
    public abstract Task<object> ExpressAsync(
        GeneInstance gene,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context);

    public virtual bool CanExpress(ExpressedProtein expressedProtein) => false;
}