using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Infrastructure.Runtime.Errors;
using FlowSynx.Infrastructure.Runtime.Exceptions;

namespace FlowSynx.Infrastructure.Security.Exceptions;

public sealed class GenBlueprintNotFoundException : RuntimeException
{
    public GenBlueprintNotFoundException(Guid geneBlueprintId)
        : base(
            RuntimeErrorCodes.GenBlueprintNotFound,
            $"Gene blueprint not found: {geneBlueprintId}"
        )
    {
    }
}