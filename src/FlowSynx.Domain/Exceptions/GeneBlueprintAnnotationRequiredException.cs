using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneAnnotationRequiredException : DomainException
{
    public GeneAnnotationRequiredException()
        : base(
            DomainErrorCodes.GeneAnnotationRequired,
            "Gene annotation is required"
        )
    {
    }
}