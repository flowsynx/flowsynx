using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneExpressedProteinRequiredException : DomainException
{
    public GeneExpressedProteinRequiredException()
        : base(
            DomainErrorCodes.GeneExpressedProteinRequired,
            "Gene expressed protein is required"
        )
    {
    }
}