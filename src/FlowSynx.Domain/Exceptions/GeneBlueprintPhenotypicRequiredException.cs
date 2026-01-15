using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class GenePhenotypicRequiredException : DomainException
{
    public GenePhenotypicRequiredException()
        : base(
            DomainErrorCodes.GenePhenotypicRequired,
            "Gene phenotypic is required"
        )
    {
    }
}