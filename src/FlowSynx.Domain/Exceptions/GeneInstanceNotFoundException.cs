using FlowSynx.Domain.Errors;
using FlowSynx.Domain.GeneInstances;

namespace FlowSynx.Domain.Exceptions;

public sealed class GeneInstanceNotFoundException : DomainException
{
    public GeneInstanceNotFoundException(GeneInstanceId geneInstanceId)
        : base(
            DomainErrorCodes.GeneInstanceIdNotFound,
            $"Gene instance {geneInstanceId} not found"
        )
    {
    }
}