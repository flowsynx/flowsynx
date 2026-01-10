using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Errors;

namespace FlowSynx.Domain.Exceptions;

public sealed class ChromosomeIdExistsException : DomainException
{
    public ChromosomeIdExistsException(ChromosomeId chromosomeId)
        : base(
            DomainErrorCodes.ChromosomeIdExists,
            $"Chromosome with ID {chromosomeId} already exists"
        )
    {
    }
}