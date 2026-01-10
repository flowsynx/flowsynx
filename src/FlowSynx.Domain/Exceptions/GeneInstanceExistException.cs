using FlowSynx.Domain.Errors;
using FlowSynx.Domain.GeneInstances;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneInstanceExistsException : DomainException
{
    public GeneInstanceExistsException(GeneInstanceId geneInstanceId)
        : base(
            DomainErrorCodes.GeneInstanceIdExists,
            $"Gene instance with ID {geneInstanceId} already exists"
        )
    {
    }
}