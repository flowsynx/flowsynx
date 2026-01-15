using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genes;
using FlowSynx.Domain.Genomes;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public interface IJsonProcessingService
{
    Task<Gene> ParseGeneAsync(string json);
    Task<Chromosome> ParseChromosomeAsync(string json);
    Task<Genome> ParseGenomeAsync(string json);
    Task<ExecutionRequest> ParseExecutionRequestAsync(string json);

    string SerializeToJson<T>(T obj);
    Task<ValidationResponse> ValidateJsonAsync(string json, string expectedKind);
}
