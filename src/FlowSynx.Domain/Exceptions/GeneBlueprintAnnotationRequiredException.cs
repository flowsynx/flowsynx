using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneBlueprintAnnotationRequiredException : DomainException
{
    public GeneBlueprintAnnotationRequiredException()
        : base(
            DomainErrorCodes.GeneBlueprintAnnotationRequired,
            "Gene blueprint annotation is required"
        )
    {
    }
}