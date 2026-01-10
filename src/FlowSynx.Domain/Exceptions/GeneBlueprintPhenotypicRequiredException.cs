using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneBlueprintPhenotypicRequiredException : DomainException
{
    public GeneBlueprintPhenotypicRequiredException()
        : base(
            DomainErrorCodes.GeneBlueprintPhenotypicRequired,
            "Gene blueprint phenotypic is required"
        )
    {
    }
}