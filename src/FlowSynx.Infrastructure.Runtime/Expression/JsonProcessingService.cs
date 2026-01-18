using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genes;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Genomes;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public class JsonProcessingService : IJsonProcessingService
{
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonProcessingService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
    }

    public async Task<Gene> ParseGeneAsync(string json)
    {
        try
        {
            var geneJson = JsonSerializer.Deserialize<GeneJson>(json, _jsonOptions);

            // Convert to domain entity
            return new Gene
            {
                Id = Guid.NewGuid(),
                Name = geneJson.Metadata.Name,
                Namespace = geneJson.Metadata.Namespace,
                Version = geneJson.Metadata.Version,
                Description = geneJson.Specification.Description,
                Specification = geneJson.Specification,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["apiVersion"] = geneJson.ApiVersion,
                    ["kind"] = geneJson.Kind,
                    ["originalJson"] = json
                },
                Labels = geneJson.Metadata.Labels ?? new System.Collections.Generic.Dictionary<string, string>(),
                Annotations = geneJson.Metadata.Annotations ?? new System.Collections.Generic.Dictionary<string, string>(),
                Owner = geneJson.Metadata.Owner,
                IsShared = geneJson.Metadata.Shared,
                Status = "active"
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse GeneBlueprint JSON: {ex.Message}", ex);
        }
    }

    public async Task<Chromosome> ParseChromosomeAsync(string json)
    {
        try
        {
            var chromosomeJson = JsonSerializer.Deserialize<ChromosomeJson>(json, _jsonOptions);

            // Convert to domain entity
            var chromosome = new Chromosome
            {
                Id = Guid.NewGuid(),
                Name = chromosomeJson.Metadata.Name,
                Namespace = chromosomeJson.Metadata.Namespace,
                Description = chromosomeJson.Specification.Description,
                Specification = chromosomeJson.Specification,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["apiVersion"] = chromosomeJson.ApiVersion,
                    ["kind"] = chromosomeJson.Kind,
                    ["originalJson"] = json
                },
                Labels = chromosomeJson.Metadata.Labels ?? new System.Collections.Generic.Dictionary<string, string>(),
                Annotations = chromosomeJson.Metadata.Annotations ?? new System.Collections.Generic.Dictionary<string, string>()
            };

            // Parse gene instances
            if (chromosomeJson.Specification.Genes != null)
            {
                int order = 0;
                foreach (var geneJson in chromosomeJson.Specification.Genes)
                {
                    var geneInstance = new Domain.GeneInstances.GeneInstance
                    {
                        Id = Guid.NewGuid(),
                        GeneId = geneJson.GeneRef.Name,
                        Parameters = geneJson.Parameters ?? new System.Collections.Generic.Dictionary<string, object>(),
                        Config = new GeneConfig
                        {
                            Operation = geneJson.Config?.Operation,
                            Mode = geneJson.Config?.Mode ?? "default",
                            Parallel = geneJson.Config?.Parallel ?? false,
                            Priority = geneJson.Config?.Priority ?? 1,
                            //Timeout = geneJson.Timeout,
                            //Retry = geneJson.Retry
                        },
                        Metadata = new System.Collections.Generic.Dictionary<string, object>
                        {
                            ["id"] = geneJson.Id,
                            ["dependencies"] = geneJson.Dependencies,
                            ["when"] = geneJson.When
                        },
                        Order = order++
                    };

                    chromosome.Genes.Add(geneInstance);
                }
            }

            return chromosome;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Chromosome JSON: {ex.Message}", ex);
        }
    }

    public async Task<Genome> ParseGenomeAsync(string json)
    {
        try
        {
            var genomeJson = JsonSerializer.Deserialize<GenomeJson>(json, _jsonOptions);

            // Convert to domain entity
            return new Genome
            {
                Id = Guid.NewGuid(),
                Name = genomeJson.Metadata.Name,
                Namespace = genomeJson.Metadata.Namespace,
                Description = genomeJson.Specification.Description,
                Specification = genomeJson.Specification,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["apiVersion"] = genomeJson.ApiVersion,
                    ["kind"] = genomeJson.Kind,
                    ["originalJson"] = json
                },
                Labels = genomeJson.Metadata.Labels ?? new System.Collections.Generic.Dictionary<string, string>(),
                Annotations = genomeJson.Metadata.Annotations ?? new System.Collections.Generic.Dictionary<string, string>(),
                SharedEnvironment = genomeJson.Specification.Environment?.Variables ?? new System.Collections.Generic.Dictionary<string, object>(),
                Owner = genomeJson.Metadata.Owner,
                IsShared = genomeJson.Metadata.Shared
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse Genome JSON: {ex.Message}", ex);
        }
    }

    public async Task<ExecutionRequest> ParseExecutionRequestAsync(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ExecutionRequest>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse ExecutionRequest JSON: {ex.Message}", ex);
        }
    }

    public string SerializeToJson<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, _jsonOptions);
    }

    public async Task<ValidationResponse> ValidateJsonAsync(string json, string expectedKind)
    {
        var response = new ValidationResponse
        {
            Metadata = new ValidationMetadata
            {
                ValidatedAt = DateTimeOffset.UtcNow,
                Resource = expectedKind
            },
            Status = new ValidationStatus
            {
                Valid = true,
                Score = 100,
                Message = "Validation passed"
            }
        };

        try
        {
            // Parse to check JSON structure
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(json, _jsonOptions);

            // Check if it has required fields
            if (jsonElement.TryGetProperty("kind", out var kindElement))
            {
                var kind = kindElement.GetString();
                if (kind != expectedKind)
                {
                    response.Status.Valid = false;
                    response.Status.Score = 0;
                    response.Status.Message = $"Expected kind '{expectedKind}', got '{kind}'";
                    response.Errors.Add(new ValidationError
                    {
                        Field = "kind",
                        Message = $"Expected '{expectedKind}', got '{kind}'",
                        Code = "KIND_MISMATCH",
                        Severity = "error"
                    });
                }
            }
            else
            {
                response.Status.Valid = false;
                response.Status.Score = 0;
                response.Status.Message = "Missing 'kind' field";
                response.Errors.Add(new ValidationError
                {
                    Field = "$",
                    Message = "Missing 'kind' field",
                    Code = "MISSING_KIND",
                    Severity = "error"
                });
            }
        }
        catch (JsonException ex)
        {
            response.Status.Valid = false;
            response.Status.Score = 0;
            response.Status.Message = "Invalid JSON";
            response.Errors.Add(new ValidationError
            {
                Field = "$",
                Message = ex.Message,
                Code = "INVALID_JSON",
                Severity = "fatal"
            });
        }
        catch (Exception ex)
        {
            response.Status.Valid = false;
            response.Status.Score = 0;
            response.Status.Message = "Validation failed";
            response.Errors.Add(new ValidationError
            {
                Field = "$",
                Message = ex.Message,
                Code = "VALIDATION_ERROR",
                Severity = "fatal"
            });
        }

        return response;
    }
}