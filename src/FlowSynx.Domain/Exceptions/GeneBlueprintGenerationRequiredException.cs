using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneBlueprintGenerationRequiredException : DomainException
{
    public GeneBlueprintGenerationRequiredException()
        : base(
            DomainErrorCodes.GeneBlueprintGenerationRequired,
            "Gene blueprint generation is required"
        )
    {
    }
}