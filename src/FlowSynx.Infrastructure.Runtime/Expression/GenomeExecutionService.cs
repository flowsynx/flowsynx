using FlowSynx.Application.Core.Persistence;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public class GenomeExecutionService : IGenomeExecutionService
{
    private readonly IExecutionRepository _executionRepository;
    private readonly IGeneRepository _geneRepository;
    private readonly IChromosomeRepository _chromosomeRepository;
    private readonly IGenomeRepository _genomeRepository;
    private readonly IJsonProcessingService _jsonService;
    private readonly IGeneExecutorFactory _executorFactory;
    private readonly ILogger<GenomeExecutionService> _logger;

    public GenomeExecutionService(
        IExecutionRepository executionRepository,
        IGeneRepository geneRepository,
        IChromosomeRepository chromosomeRepository,
        IGenomeRepository genomeRepository,
        IJsonProcessingService jsonService,
        IGeneExecutorFactory executorFactory,
        ILogger<GenomeExecutionService> logger)
    {
        _executionRepository = executionRepository;
        _geneRepository = geneRepository;
        _chromosomeRepository = chromosomeRepository;
        _genomeRepository = genomeRepository;
        _jsonService = jsonService;
        _executorFactory = executorFactory;
        _logger = logger;
    }

    public async Task<ExecutionResponse> ExecuteGeneAsync(
        TenantId tenantId,
        string userId,
        Guid geneId, 
        Dictionary<string, object> parameters, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        // Create execution record
        var executionRecord = new ExecutionRecord
        {
            ExecutionId = executionId,
            TargetType = "gene",
            TargetId = geneId,
            TargetName = $"gene-{geneId}",
            Namespace = "default",
            Request = new Dictionary<string, object>
            {
                ["parameters"] = parameters,
                ["context"] = context
            },
            Status = "running",
            Progress = 0,
            StartedAt = startedAt,
            TriggeredBy = "system"
        };

        await _executionRepository.AddAsync(executionRecord);

        try
        {
            // Load gene
            var gene = await _geneRepository.GetByIdAsync(tenantId, userId, geneId, cancellationToken) 
                ?? throw new Exception($"Gene not found: {geneId}");

            // Create gene instance
            var geneInstance = new GeneInstance
            {
                Id = "execution-instance",
                GeneRef = new GeneReference
                {
                    Name = gene.Name,
                    Version = gene.Version,
                    Namespace = gene.Namespace
                },
                Parameters = parameters ?? new Dictionary<string, object>(),
                Config = new GeneConfigJson
                {
                    Operation = gene.Specification.ExpressionProfile?.DefaultOperation,
                    Mode = "default"
                }
            };

            // Get executor
            var executor = _executorFactory.CreateExecutor(gene.Specification.Executable);

            // Update progress
            executionRecord.Progress = 50;
            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            // Execute
            var result = await executor.ExecuteAsync(
                new GeneJson
                {
                    Metadata = new GeneMetadata
                    {
                        Name = gene.Name,
                        Namespace = gene.Namespace,
                        Id = gene.Id.ToString(),
                        Version = gene.Version
                    },
                    Specification = gene.Specification
                },
                geneInstance,
                parameters ?? new Dictionary<string, object>(),
                context ?? new Dictionary<string, object>());

            // Update execution record
            executionRecord.Progress = 100;
            executionRecord.Status = "completed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.Duration = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.Response = new Dictionary<string, object>
            {
                ["result"] = result,
                ["success"] = true
            };

            executionRecord.Logs.Add(new Domain.Genomes.ExecutionLog
            {
                Level = "info",
                Message = $"Gene '{gene.Name}' executed successfully",
                Source = gene.Name,
                Timestamp = DateTime.UtcNow
            });

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            // Return response
            return new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    Duration = executionRecord.Duration
                },
                Status = new ExecutionStatus
                {
                    Phase = "succeeded",
                    Message = "Gene execution completed",
                    Progress = 100,
                    Health = "healthy"
                },
                Results = new Dictionary<string, object>
                {
                    ["result"] = result
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gene execution failed for {GeneId}", geneId);

            // Update execution record with error
            executionRecord.Status = "failed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.Duration = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.ErrorMessage = ex.Message;
            executionRecord.ErrorCode = "EXECUTION_FAILED";
            executionRecord.Response = new Dictionary<string, object>
            {
                ["error"] = ex.Message,
                ["success"] = false
            };

            executionRecord.Logs.Add(new Domain.Genomes.ExecutionLog
            {
                Level = "error",
                Message = ex.Message,
                Source = "execution",
                Timestamp = DateTime.UtcNow,
                Data = new Dictionary<string, object>
                {
                    ["exception"] = ex.GetType().Name,
                    ["stackTrace"] = ex.StackTrace
                }
            });

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            return new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    Duration = executionRecord.Duration
                },
                Status = new ExecutionStatus
                {
                    Phase = "failed",
                    Message = ex.Message,
                    Progress = 100,
                    Health = "unhealthy",
                    Reason = "ExecutionError"
                },
                Errors = new List<ExecutionError>
                    {
                        new ExecutionError
                        {
                            Code = "EXECUTION_FAILED",
                            Message = ex.Message,
                            Source = "gene",
                            Timestamp = DateTime.UtcNow
                        }
                    }
            };
        }
    }

    public async Task<ExecutionResponse> ExecuteChromosomeAsync(
        TenantId tenantId,
        string userId,
        Guid chromosomeId, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        // Create execution record
        var executionRecord = new ExecutionRecord
        {
            ExecutionId = executionId,
            TargetType = "chromosome",
            TargetId = chromosomeId,
            TargetName = $"chromosome-{chromosomeId}",
            Namespace = "default",
            Request = new Dictionary<string, object>
            {
                ["context"] = context
            },
            Status = "running",
            Progress = 0,
            StartedAt = startedAt,
            TriggeredBy = "system"
        };

        await _executionRepository.AddAsync(executionRecord, cancellationToken);

        try
        {
            // Load chromosome with genes
            var chromosome = await _chromosomeRepository.GetByIdAsync(chromosomeId, cancellationToken);
            if (chromosome == null)
            {
                throw new Exception($"Chromosome not found: {chromosomeId}");
            }

            var results = new Dictionary<string, object>();
            var geneResults = new List<object>();

            // Execute genes in order
            var genes = chromosome.Genes.OrderBy(g => g.Order).ToList();
            int totalGenes = genes.Count;

            for (int i = 0; i < totalGenes; i++)
            {
                var gene = genes[i];

                // Update progress
                executionRecord.Progress = (int)((i * 100) / totalGenes);
                await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

                try
                {
                    // Load gene
                    var geneByName = await _geneRepository.GetByNameAndVersionAsync(
                        gene.GeneId,
                        "latest", cancellationToken);

                    if (geneByName == null)
                    {
                        throw new Exception($"Gene blueprint not found: {gene.GeneId}");
                    }

                    // Execute gene
                    var geneResult = await ExecuteGeneAsync(
                        tenantId,
                        userId,
                        geneByName.Id,
                        gene.Parameters,
                        context);

                    geneResults.Add(new
                    {
                        geneId = gene.GeneId,
                        result = geneResult.Results?.GetValueOrDefault("result"),
                        status = geneResult.Status.Phase
                    });

                    executionRecord.Logs.Add(new Domain.Genomes.ExecutionLog
                    {
                        Level = "info",
                        Message = $"Gene '{gene.GeneId}' executed successfully",
                        Source = gene.GeneId,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    executionRecord.Logs.Add(new Domain.Genomes.ExecutionLog
                    {
                        Level = "error",
                        Message = $"Gene '{gene.GeneId}' failed: {ex.Message}",
                        Source = gene.GeneId,
                        Timestamp = DateTime.UtcNow
                    });

                    // Check if we should continue based on chromosome error handling
                    var errorHandling = chromosome.Spec.Environment?.ErrorHandling;
                    if (errorHandling?.ErrorHandling == "propagate")
                    {
                        throw;
                    }
                    // else continue with other genes
                }
            }

            // Update execution record
            executionRecord.Progress = 100;
            executionRecord.Status = "completed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.Duration = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.Response = new Dictionary<string, object>
            {
                ["geneResults"] = geneResults,
                ["success"] = true
            };

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            // Return response
            return new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    Duration = executionRecord.Duration
                },
                Status = new ExecutionStatus
                {
                    Phase = "succeeded",
                    Message = "Chromosome execution completed",
                    Progress = 100,
                    Health = "healthy"
                },
                Results = new Dictionary<string, object>
                {
                    ["geneResults"] = geneResults
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chromosome execution failed for {ChromosomeId}", chromosomeId);

            executionRecord.Status = "failed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.Duration = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.ErrorMessage = ex.Message;
            executionRecord.ErrorCode = "EXECUTION_FAILED";

            executionRecord.Logs.Add(new Domain.Genomes.ExecutionLog
            {
                Level = "error",
                Message = ex.Message,
                Source = "chromosome",
                Timestamp = DateTime.UtcNow
            });

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            return new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    Duration = executionRecord.Duration
                },
                Status = new ExecutionStatus
                {
                    Phase = "failed",
                    Message = ex.Message,
                    Progress = executionRecord.Progress,
                    Health = "unhealthy",
                    Reason = "ExecutionError"
                },
                Errors = new List<ExecutionError>
                    {
                        new ExecutionError
                        {
                            Code = "EXECUTION_FAILED",
                            Message = ex.Message,
                            Source = "chromosome",
                            Timestamp = DateTime.UtcNow
                        }
                    }
            };
        }
    }

    public async Task<ExecutionResponse> ExecuteGenomeAsync(
        TenantId tenantId,
        string userId,
        Guid genomeId, 
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startedAt = DateTime.UtcNow;

        // Create execution record
        var executionRecord = new ExecutionRecord
        {
            ExecutionId = executionId,
            TargetType = "genome",
            TargetId = genomeId,
            TargetName = $"genome-{genomeId}",
            Namespace = "default",
            Request = new Dictionary<string, object>
            {
                ["context"] = context
            },
            Status = "running",
            Progress = 0,
            StartedAt = startedAt,
            TriggeredBy = "system"
        };

        await _executionRepository.AddAsync(executionRecord, cancellationToken);

        try
        {
            // Load genome with chromosomes
            var genome = await _genomeRepository.GetByIdAsync(genomeId, cancellationToken);
            if (genome == null)
            {
                throw new Exception($"Genome not found: {genomeId}");
            }

            var chromosomeResults = new List<object>();

            // Execute chromosomes
            var chromosomes = genome.Chromosomes.ToList();
            int totalChromosomes = chromosomes.Count;

            for (int i = 0; i < totalChromosomes; i++)
            {
                var chromosome = chromosomes[i];

                // Update progress
                executionRecord.Progress = (int)((i * 100) / totalChromosomes);
                await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

                try
                {
                    var chromosomeResult = await ExecuteChromosomeAsync(tenantId, userId, chromosome.Id, context);

                    chromosomeResults.Add(new
                    {
                        chromosomeId = chromosome.Id,
                        chromosomeName = chromosome.Name,
                        result = chromosomeResult,
                        status = chromosomeResult.Status.Phase
                    });

                    executionRecord.Logs.Add(new Domain.Genomes.ExecutionLog
                    {
                        Level = "info",
                        Message = $"Chromosome '{chromosome.Name}' executed successfully",
                        Source = chromosome.Name,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    executionRecord.Logs.Add(new Domain.Genomes.ExecutionLog
                    {
                        Level = "error",
                        Message = $"Chromosome '{chromosome.Name}' failed: {ex.Message}",
                        Source = chromosome.Name,
                        Timestamp = DateTime.UtcNow
                    });

                    // Check genome execution strategy
                    var executionStrategy = genome.Spec.Execution?.Strategy;
                    if (executionStrategy == "stop-on-error")
                    {
                        throw;
                    }
                    // else continue with other chromosomes
                }
            }

            // Update execution record
            executionRecord.Progress = 100;
            executionRecord.Status = "completed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.Duration = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.Response = new Dictionary<string, object>
            {
                ["chromosomeResults"] = chromosomeResults,
                ["success"] = true
            };

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            // Return response
            return new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    Duration = executionRecord.Duration
                },
                Status = new ExecutionStatus
                {
                    Phase = "succeeded",
                    Message = "Genome execution completed",
                    Progress = 100,
                    Health = "healthy"
                },
                Results = new Dictionary<string, object>
                {
                    ["chromosomeResults"] = chromosomeResults
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Genome execution failed for {GenomeId}", genomeId);

            executionRecord.Status = "failed";
            executionRecord.CompletedAt = DateTime.UtcNow;
            executionRecord.Duration = (long)((executionRecord.CompletedAt - startedAt)?.TotalMilliseconds ?? 0);
            executionRecord.ErrorMessage = ex.Message;
            executionRecord.ErrorCode = "EXECUTION_FAILED";

            executionRecord.Logs.Add(new Domain.Genomes.ExecutionLog
            {
                Level = "error",
                Message = ex.Message,
                Source = "genome",
                Timestamp = DateTime.UtcNow
            });

            await _executionRepository.UpdateAsync(executionRecord, cancellationToken);

            return new ExecutionResponse
            {
                Metadata = new ExecutionResponseMetadata
                {
                    Id = executionRecord.Id.ToString(),
                    ExecutionId = executionId,
                    StartedAt = startedAt,
                    CompletedAt = executionRecord.CompletedAt,
                    Duration = executionRecord.Duration
                },
                Status = new ExecutionStatus
                {
                    Phase = "failed",
                    Message = ex.Message,
                    Progress = executionRecord.Progress,
                    Health = "unhealthy",
                    Reason = "ExecutionError"
                },
                Errors = new List<ExecutionError>
                    {
                        new ExecutionError
                        {
                            Code = "EXECUTION_FAILED",
                            Message = ex.Message,
                            Source = "genome",
                            Timestamp = DateTime.UtcNow
                        }
                    }
            };
        }
    }

    public async Task<ExecutionResponse> ExecuteRequestAsync(
        TenantId tenantId,
        string userId,
        ExecutionRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var target = request.Spec.Target;
            var parameters = request.Spec.Parameters ?? new Dictionary<string, object>();
            var context = new Dictionary<string, object>();

            // Merge environment and context
            if (request.Spec.Environment != null)
            {
                foreach (var kvp in request.Spec.Environment)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }
            if (request.Spec.Context != null)
            {
                foreach (var kvp in request.Spec.Context)
                {
                    context[kvp.Key] = kvp.Value;
                }
            }

            switch (target.Type.ToLower())
            {
                case "gene":
                    var gene = await _geneRepository.GetByNameAndVersionAsync(
                        target.Name, target.Version ?? "latest");
                    if (gene == null)
                        throw new Exception($"Gene not found: {target.Name}");

                    return await ExecuteGeneAsync(tenantId, userId, gene.Id, parameters, context);

                case "chromosome":
                    var chromosome = await _chromosomeRepository.GetByNameAsync(
                        target.Name, target.Namespace ?? "default");
                    if (chromosome == null)
                        throw new Exception($"Chromosome not found: {target.Name}");

                    return await ExecuteChromosomeAsync(tenantId, userId, chromosome.Id, context);

                case "genome":
                    var genome = await _genomeRepository.GetByNameAsync(
                        target.Name, target.Namespace ?? "default");
                    if (genome == null)
                        throw new Exception($"Genome not found: {target.Name}");

                    return await ExecuteGenomeAsync(tenantId, userId, genome.Id, context);

                default:
                    throw new Exception($"Unknown target type: {target.Type}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution request failed");
            throw;
        }
    }

    public async Task<ExecutionRecord?> GetExecutionRecordAsync(
        Guid executionId, 
        CancellationToken cancellationToken = default)
    {
        return await _executionRepository.GetByIdAsync(executionId, cancellationToken);
    }

    public async Task<IEnumerable<ExecutionRecord>> GetExecutionHistoryAsync(
        string targetType, 
        Guid targetId, 
        int limit = 50, 
        CancellationToken cancellationToken = default)
    {
        return (await _executionRepository.GetByTargetAsync(targetType, targetId))
            .Take(limit)
            .ToList();
    }
}