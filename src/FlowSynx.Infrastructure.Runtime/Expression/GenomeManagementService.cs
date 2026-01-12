using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Exceptions;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.Genomes;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public class GenomeManagementService : IGenomeManagementService
{
    private readonly IGeneBlueprintRepository _geneBlueprintRepository;
    private readonly IChromosomeRepository _chromosomeRepository;
    private readonly IGenomeRepository _genomeRepository;
    private readonly IJsonProcessingService _jsonService;
    private readonly IGenomeExecutionService _executionService;
    private readonly ILogger<GenomeManagementService> _logger;

    public GenomeManagementService(
        IGeneBlueprintRepository geneBlueprintRepository,
        IChromosomeRepository chromosomeRepository,
        IGenomeRepository genomeRepository,
        IJsonProcessingService jsonService,
        IGenomeExecutionService executionService,
        ILogger<GenomeManagementService> logger)
    {
        _geneBlueprintRepository = geneBlueprintRepository;
        _chromosomeRepository = chromosomeRepository;
        _genomeRepository = genomeRepository;
        _jsonService = jsonService;
        _executionService = executionService;
        _logger = logger;
    }

    public async Task<GeneBlueprint> RegisterGeneBlueprintAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate JSON
            var validation = await _jsonService.ValidateJsonAsync(json, "GeneBlueprint");
            if (!validation.Status.Valid)
            {
                throw new FlowSynx.Infrastructure.Runtime.Exceptions.ValidationException("Gene blueprint validation failed", validation.Errors);
            }

            // Parse JSON
            var geneBlueprint = await _jsonService.ParseGeneBlueprintAsync(json);

            // Check if already exists
            var existing = await _geneBlueprintRepository.GetByNameAndVersionAsync(
                geneBlueprint.Name, geneBlueprint.Version, cancellationToken);

            if (existing != null)
            {
                // Update existing
                existing.Spec = geneBlueprint.Spec;
                existing.Description = geneBlueprint.Description;
                existing.Metadata = geneBlueprint.Metadata;
                existing.Labels = geneBlueprint.Labels;
                existing.Annotations = geneBlueprint.Annotations;

                await _geneBlueprintRepository.UpdateAsync(existing, cancellationToken);

                _logger.LogInformation("Updated existing gene blueprint: {Name} v{Version}",
                    geneBlueprint.Name, geneBlueprint.Version);

                return existing;
            }
            else
            {
                // Add new
                await _geneBlueprintRepository.AddAsync(geneBlueprint, cancellationToken);

                _logger.LogInformation("Registered new gene blueprint: {Name} v{Version}",
                    geneBlueprint.Name, geneBlueprint.Version);

                return geneBlueprint;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register gene blueprint");
            throw;
        }
    }

    public async Task<Chromosome> RegisterChromosomeAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate JSON
            var validation = await _jsonService.ValidateJsonAsync(json, "Chromosome");
            if (!validation.Status.Valid)
            {
                throw new FlowSynx.Infrastructure.Runtime.Exceptions.ValidationException("Chromosome validation failed", validation.Errors);
            }

            // Parse JSON
            var chromosome = await _jsonService.ParseChromosomeAsync(json);

            // Check if already exists
            var existing = await _chromosomeRepository.GetByNameAsync(
                chromosome.Name, chromosome.Namespace,
                cancellationToken);

            if (existing != null)
            {
                // Update existing
                existing.Spec = chromosome.Spec;
                existing.Description = chromosome.Description;
                existing.Metadata = chromosome.Metadata;
                existing.Labels = chromosome.Labels;
                existing.Annotations = chromosome.Annotations;
                existing.Genes = chromosome.Genes;

                await _chromosomeRepository.UpdateAsync(existing, cancellationToken);

                _logger.LogInformation("Updated existing chromosome: {Name}", chromosome.Name);

                return existing;
            }
            else
            {
                // Add new
                await _chromosomeRepository.AddAsync(chromosome, cancellationToken);

                _logger.LogInformation("Registered new chromosome: {Name}", chromosome.Name);

                return chromosome;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register chromosome");
            throw;
        }
    }

    public async Task<Genome> RegisterGenomeAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate JSON
            var validation = await _jsonService.ValidateJsonAsync(json, "Genome");
            if (!validation.Status.Valid)
            {
                throw new FlowSynx.Infrastructure.Runtime.Exceptions.ValidationException("Genome validation failed", validation.Errors);
            }

            // Parse JSON
            var genome = await _jsonService.ParseGenomeAsync(json);

            // Check if already exists
            var existing = await _genomeRepository.GetByNameAsync(
                genome.Name, genome.Namespace, cancellationToken);

            if (existing != null)
            {
                // Update existing
                existing.Spec = genome.Spec;
                existing.Description = genome.Description;
                existing.Metadata = genome.Metadata;
                existing.Labels = genome.Labels;
                existing.Annotations = genome.Annotations;
                existing.SharedEnvironment = genome.SharedEnvironment;

                await _genomeRepository.UpdateAsync(existing, cancellationToken);

                _logger.LogInformation("Updated existing genome: {Name}", genome.Name);

                return existing;
            }
            else
            {
                // Add new
                await _genomeRepository.AddAsync(genome, cancellationToken);

                _logger.LogInformation("Registered new genome: {Name}", genome.Name);

                return genome;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register genome");
            throw;
        }
    }

    public async Task<ValidationResponse> ValidateJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonElement = System.Text.Json.JsonDocument.Parse(json).RootElement;

            if (jsonElement.TryGetProperty("kind", out var kindElement))
            {
                var kind = kindElement.GetString();
                return await _jsonService.ValidateJsonAsync(json, kind);
            }

            return new ValidationResponse
            {
                Metadata = new ValidationMetadata
                {
                    ValidatedAt = DateTimeOffset.UtcNow,
                    Resource = "unknown"
                },
                Status = new ValidationStatus
                {
                    Valid = false,
                    Score = 0,
                    Message = "Missing 'kind' field"
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResponse
            {
                Metadata = new ValidationMetadata
                {
                    ValidatedAt = DateTimeOffset.UtcNow,
                    Resource = "error"
                },
                Status = new ValidationStatus
                {
                    Valid = false,
                    Score = 0,
                    Message = ex.Message
                }
            };
        }
    }

    public async Task<IEnumerable<GeneBlueprint>> SearchGeneBlueprintsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _geneBlueprintRepository.SearchAsync(searchTerm, cancellationToken);
    }

    public async Task<IEnumerable<Chromosome>> GetChromosomesByGenomeAsync(Guid genomeId, CancellationToken cancellationToken = default)
    {
        return await _chromosomeRepository.GetByGenomeIdAsync(genomeId, cancellationToken);
    }

    public async Task<IEnumerable<Genome>> GetGenomesByOwnerAsync(string owner, CancellationToken cancellationToken = default)
    {
        return await _genomeRepository.GetByOwnerAsync(owner, cancellationToken);
    }

    public async Task<ExecutionResponse> ExecuteJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonElement = System.Text.Json.JsonDocument.Parse(json).RootElement;

            if (jsonElement.TryGetProperty("kind", out var kindElement))
            {
                var kind = kindElement.GetString();

                if (kind == "ExecutionRequest")
                {
                    var request = await _jsonService.ParseExecutionRequestAsync(json);
                    return await _executionService.ExecuteRequestAsync(request);
                }
                else if (kind == "GeneBlueprint")
                {
                    var geneBlueprint = await RegisterGeneBlueprintAsync(json);
                    return new ExecutionResponse
                    {
                        Metadata = new ExecutionResponseMetadata
                        {
                            Id = Guid.NewGuid().ToString(),
                            ExecutionId = $"register-{Guid.NewGuid()}",
                            StartedAt = DateTimeOffset.UtcNow,
                            CompletedAt = DateTimeOffset.UtcNow
                        },
                        Status = new ExecutionStatus
                        {
                            Phase = "succeeded",
                            Message = $"Gene blueprint '{geneBlueprint.Name}' registered successfully",
                            Progress = 100,
                            Health = "healthy"
                        }
                    };
                }
                else if (kind == "Chromosome")
                {
                    var chromosome = await RegisterChromosomeAsync(json);
                    return new ExecutionResponse
                    {
                        Metadata = new ExecutionResponseMetadata
                        {
                            Id = Guid.NewGuid().ToString(),
                            ExecutionId = $"register-{Guid.NewGuid()}",
                            StartedAt = DateTimeOffset.UtcNow,
                            CompletedAt = DateTimeOffset.UtcNow
                        },
                        Status = new ExecutionStatus
                        {
                            Phase = "succeeded",
                            Message = $"Chromosome '{chromosome.Name}' registered successfully",
                            Progress = 100,
                            Health = "healthy"
                        }
                    };
                }
                else if (kind == "Genome")
                {
                    var genome = await RegisterGenomeAsync(json);
                    return new ExecutionResponse
                    {
                        Metadata = new ExecutionResponseMetadata
                        {
                            Id = Guid.NewGuid().ToString(),
                            ExecutionId = $"register-{Guid.NewGuid()}",
                            StartedAt = DateTimeOffset.UtcNow,
                            CompletedAt = DateTimeOffset.UtcNow
                        },
                        Status = new ExecutionStatus
                        {
                            Phase = "succeeded",
                            Message = $"Genome '{genome.Name}' registered successfully",
                            Progress = 100,
                            Health = "healthy"
                        }
                    };
                }
            }

            throw new FlowSynx.Infrastructure.Runtime.Exceptions.ValidationException("Unknown or missing 'kind' field");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute JSON");
            throw;
        }
    }

    public async Task<ExecutionResponse> GetExecutionResultAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var executionRecord = await _executionService.GetExecutionRecordAsync(executionId, cancellationToken);

        if (executionRecord == null)
        {
            throw new NotFoundException($"Execution record not found: {executionId}");
        }

        return new ExecutionResponse
        {
            Metadata = new ExecutionResponseMetadata
            {
                Id = executionRecord.Id.ToString(),
                ExecutionId = executionRecord.ExecutionId,
                StartedAt = executionRecord.StartedAt,
                CompletedAt = executionRecord.CompletedAt,
                Duration = executionRecord.Duration
            },
            Status = new ExecutionStatus
            {
                Phase = executionRecord.Status,
                Message = executionRecord.ErrorMessage ?? "Execution completed",
                Progress = executionRecord.Progress,
                Health = executionRecord.Status == "completed" ? "healthy" : "unhealthy",
                Reason = executionRecord.ErrorCode
            },
            Results = executionRecord.Response,
            Errors = executionRecord.Status == "failed" ? new List<ExecutionError>
                {
                    new ExecutionError
                    {
                        Code = executionRecord.ErrorCode ?? "UNKNOWN_ERROR",
                        Message = executionRecord.ErrorMessage ?? "Execution failed",
                        Source = executionRecord.TargetType,
                        Timestamp = executionRecord.CompletedAt ?? DateTime.UtcNow
                    }
                } : new List<ExecutionError>(),
            Logs = executionRecord.Logs?.Select(log => new Application.Models.ExecutionLog
            {
                Level = log.Level,
                Message = log.Message,
                Source = log.Source,
                Timestamp = log.Timestamp,
                Data = log.Data
            }).ToList() ?? new List<Application.Models.ExecutionLog>(),
            Artifacts = executionRecord.Artifacts?.Select(artifact => new Application.Models.ExecutionArtifact
            {
                Name = artifact.Name,
                Type = artifact.Type,
                Content = artifact.Content,
                Size = artifact.Size,
                CreatedAt = artifact.CreatedAt
            }).ToList() ?? new List<Application.Models.ExecutionArtifact>()
        };
    }
}