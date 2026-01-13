using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneBlueprintExpressedProteinRequiredException : DomainException
{
    public GeneBlueprintExpressedProteinRequiredException()
        : base(
            DomainErrorCodes.GeneBlueprintExpressedProteinRequired,
            "Gene blueprint expressed protein is required"
        )
    {
    }
}