using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class ChromosomeIdNotFoundException : DomainException
{
    public ChromosomeIdNotFoundException(ChromosomeId chromosomeId)
        : base(
            DomainErrorCodes.ChromosomeIdNotFound,
            $"Chromosome with ID {chromosomeId} not found"
        )
    {
    }
}