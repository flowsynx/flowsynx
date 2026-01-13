//using FlowSynx.Application.Core.Persistence;
//using FlowSynx.Application.Core.Services;
//using FlowSynx.Domain.Chromosomes;
//using FlowSynx.Domain.Enums;
//using FlowSynx.Domain.Exceptions;
//using FlowSynx.Domain.GeneInstances;
//using FlowSynx.Domain.ValueObjects;
//using FlowSynx.Infrastructure.Security.Exceptions;

//namespace FlowSynx.Infrastructure.Runtime.Expression;

//public class GeneExpressionEngine : IGeneExpressionEngine
//{
//    private readonly IGeneBlueprintRepository _blueprintRepository;
//    private readonly List<IGeneExpressor> _geneExpressors;

//    public GeneExpressionEngine(IGeneBlueprintRepository blueprintRepository)
//    {
//        _blueprintRepository = blueprintRepository ?? throw new ArgumentNullException(nameof(blueprintRepository));
//        _geneExpressors = new List<IGeneExpressor>
//        {
//            new AssemblyGeneExpressor()
//        };
//    }

//    public async Task<ExpressionResult> ExpressGeneAsync(
//        GeneInstance gene,
//        CellularEnvironment environment,
//        Dictionary<string, object> sharedContext,
//        CancellationToken cancellationToken)
//    {
//        var startTime = DateTime.UtcNow;

//        try
//        {
//            // Load blueprint if not already loaded
//            if (gene.Blueprint == null)
//            {
//                var blueprint = await _blueprintRepository.GetByIdAsync(gene.GeneBlueprintId, cancellationToken) 
//                    ?? throw new GenBlueprintNotFoundException(gene.GeneBlueprintId);

//                gene.Blueprint = blueprint;
//            }

//            // Check conditions
//            if (!ShouldExpressGene(gene, sharedContext))
//            {
//                return new ExpressionResult(
//                    gene.Id,
//                    ExpressionStatus.Skipped,
//                    expressionTime: DateTime.UtcNow - startTime);
//            }

//            // Get immune response (gene-level or fallback to chromosome-level)
//            var immuneResponse = gene.Blueprint.ImmuneSystem ?? environment.ImmuneSystem;

//            // Find executor
//            var expressor = _geneExpressors.FirstOrDefault(e =>
//                e.CanExpress(gene.Blueprint.ExpressedProtein)) 
//                ?? throw new GenExpressorNotFoundException(gene.Blueprint.ExpressedProtein.Type);

//            // Execute with retry
//            var result = await ExecuteWithRetryAsync(
//                () => expressor.ExpressAsync(gene, gene.NucleotideSequences, sharedContext),
//                immuneResponse);

//            return new ExpressionResult(
//                gene.Id,
//                ExpressionStatus.Expressed,
//                result: result,
//                expressionTime: DateTime.UtcNow - startTime,
//                metrics: new Dictionary<string, object> { ["execution_count"] = 1 });
//        }
//        catch (Exception ex)
//        {
//            return new ExpressionResult(
//                gene.Id,
//                ExpressionStatus.Dysregulated,
//                error: ex,
//                expressionTime: DateTime.UtcNow - startTime,
//                metrics: new Dictionary<string, object> { ["error"] = ex.Message });
//        }
//    }

//    public async Task<List<ExpressionResult>> ExpressChromosomeAsync(
//        Chromosome chromosome,
//        Dictionary<string, object> runtimeContext,
//        CancellationToken cancellationToken)
//    {
//        var results = new List<ExpressionResult>();

//        // Get execution order (topological sort based on dependencies)
//        var executionOrder = GetExecutionOrder(chromosome);

//        foreach (var geneId in executionOrder)
//        {
//            var gene = chromosome.GetGene(new GeneInstanceId(geneId));
//            if (gene == null) continue;

//            // Check if dependencies are successful
//            if (!AreDependenciesSuccessful(gene, results))
//            {
//                results.Add(new ExpressionResult(
//                    gene.Id,
//                    ExpressionStatus.Skipped));
//                continue;
//            }

//            var result = await ExpressGeneAsync(gene, chromosome.CellularEnvironment, runtimeContext, cancellationToken);
//            results.Add(result);

//            // Update shared context with result
//            runtimeContext[$"gene_result_{gene.Id}"] = result;
//        }

//        // Store results in chromosome aggregate
//        chromosome.AddExpressionResults(results);

//        return results;
//    }

//    private bool ShouldExpressGene(GeneInstance gene, Dictionary<string, object> context)
//    {
//        var regulatoryConditions = gene.ExpressionProfile.RegulatoryConditions;
//        if (regulatoryConditions.Count == 0) return true;

//        foreach (var regulatoryCondition in regulatoryConditions)
//        {
//            if (!EvaluateCondition(regulatoryCondition, context))
//                return false;
//        }

//        return true;
//    }

//    private bool EvaluateCondition(RegulatoryCondition condition, Dictionary<string, object> context)
//    {
//        if (!context.TryGetValue(condition.Field, out var value))
//            return false;

//        return condition.Operator switch
//        {
//            "equals" => value?.ToString() == condition.Value?.ToString(),
//            "greater_than" => Convert.ToDouble(value) > Convert.ToDouble(condition.Value),
//            "contains" => value?.ToString()?.Contains(condition.Value?.ToString() ?? "") == true,
//            _ => true
//        };
//    }

//    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, ImmuneSystem immuneSystem)
//    {
//        int retryCount = 0;
//        Exception lastException = null;

//        while (retryCount <= immuneSystem.MaximumMutationAttempts)
//        {
//            try
//            {
//                return await action();
//            }
//            catch (Exception ex)
//            {
//                lastException = ex;
//                retryCount++;

//                if (retryCount > immuneSystem.MaximumMutationAttempts)
//                    break;

//                if (immuneSystem.RecoveryLatency > 0)
//                {
//                    await Task.Delay(immuneSystem.RecoveryLatency);
//                }
//            }
//        }

//        if (immuneSystem.ResponsePattern == "fallback" &&
//            !string.IsNullOrEmpty(immuneSystem.AlternateExpressionPath))
//        {
//            // Execute fallback operation
//            return default;
//        }

//        throw new AggregateException("All retry attempts failed", lastException);
//    }

//    private List<string> GetExecutionOrder(Chromosome chromosome)
//    {
//        var visited = new HashSet<string>();
//        var order = new List<string>();
//        var graph = BuildDependencyGraph(chromosome);

//        foreach (var gene in chromosome.Genes)
//        {
//            if (!visited.Contains(gene.Id.Value))
//            {
//                TopologicalSortDFS(gene.Id.Value, graph, visited, order);
//            }
//        }

//        order.Reverse();
//        return order;
//    }

//    private Dictionary<string, List<string>> BuildDependencyGraph(Chromosome chromosome)
//    {
//        var graph = new Dictionary<string, List<string>>();

//        foreach (var gene in chromosome.Genes)
//        {
//            graph[gene.Id.Value] = gene.RegulatoryNetwork.Select(d => d.Value).ToList();
//        }

//        return graph;
//    }

//    private void TopologicalSortDFS(string node, Dictionary<string, List<string>> graph,
//                                  HashSet<string> visited, List<string> order)
//    {
//        visited.Add(node);

//        if (graph.TryGetValue(node, out var dependencies))
//        {
//            foreach (var dep in dependencies)
//            {
//                if (!visited.Contains(dep))
//                {
//                    TopologicalSortDFS(dep, graph, visited, order);
//                }
//            }
//        }

//        order.Add(node);
//    }

//    private bool AreDependenciesSuccessful(GeneInstance gene, List<ExpressionResult> results)
//    {
//        foreach (var depId in gene.RegulatoryNetwork)
//        {
//            var depResult = results.FirstOrDefault(r => r.GeneInstanceId == depId);
//            if (depResult == null || !depResult.IsExpressed)
//                return false;
//        }

//        return true;
//    }
//}