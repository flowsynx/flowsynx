using FlowSynx.Application.Core.Interfaces;
using FlowSynx.Application.Core.Services;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.Primitives;

namespace FlowSynx.Infrastructure.Common;

public class GeneValidator : IGeneValidator
{
    private readonly IGeneBlueprintRepository _blueprintRepository;

    public GeneValidator(IGeneBlueprintRepository blueprintRepository)
    {
        _blueprintRepository = blueprintRepository ?? throw new ArgumentNullException(nameof(blueprintRepository));
    }

    public async Task<ValidationResult> ValidateGeneInstanceAsync(
        GeneInstance instance, 
        GeneBlueprint blueprint, 
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        if (blueprint == null)
        {
            return await ValidationResult.FailAsync($"Gene blueprint not found for gene ID: {instance.GeneBlueprintId}");
        }

        // Validate parameters against blueprint
        foreach (var paramDef in blueprint.GeneticParameters)
        {
            if (paramDef.Required && !instance.Parameters.ContainsKey(paramDef.Name))
            {
                errors.Add($"Required parameter '{paramDef.Name}' is missing");
            }

            if (instance.Parameters.TryGetValue(paramDef.Name, out var value))
            {
                if (!IsTypeCompatible(value, paramDef.Type))
                {
                    errors.Add($"Parameter '{paramDef.Name}' has incompatible type. Expected: {paramDef.Type}");
                }
            }
        }

        // Check for extra parameters not defined in blueprint
        foreach (var param in instance.Parameters.Keys)
        {
            if (!blueprint.GeneticParameters.Any(p => p.Name == param))
            {
                errors.Add($"Parameter '{param}' is not defined in the gene blueprint");
            }
        }

        return errors.Count > 0 ? ValidationResult.Fail(errors) : ValidationResult.Success();
    }

    public async Task<ValidationResult> ValidateChromosomeAsync(
        Chromosome chromosome, 
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        // Load blueprints for all genes
        var blueprintTasks = chromosome.Genes
            .Select(g => _blueprintRepository.GetByIdAsync(g.GeneBlueprintId, cancellationToken))
            .ToList();

        var blueprints = await Task.WhenAll(blueprintTasks);

        // Validate each gene
        for (int i = 0; i < chromosome.Genes.Count; i++)
        {
            var gene = chromosome.Genes[i];
            var blueprint = blueprints[i];
            var geneResult = await ValidateGeneInstanceAsync(gene, blueprint, cancellationToken);

            if (!geneResult.IsValid)
            {
                errors.AddRange(geneResult.Messages.Select(e => $"[{gene.Id}] {e}"));
            }

            // Store blueprint reference
            gene.Blueprint = blueprint;
        }

        // Validate dependencies exist
        var geneIds = chromosome.Genes.Select(g => g.Id.Value).ToHashSet();
        foreach (var gene in chromosome.Genes)
        {
            foreach (var dep in gene.Dependencies)
            {
                if (!geneIds.Contains(dep.Value))
                {
                    errors.Add($"Gene '{gene.Id}' depends on non-existent gene '{dep}'");
                }
            }
        }

        // Check for circular dependencies
        if (HasCircularDependencies(chromosome))
        {
            errors.Add("Circular dependencies detected in chromosome");
        }

        // Check compatibility matrix conflicts
        for (int i = 0; i < chromosome.Genes.Count; i++)
        {
            for (int j = i + 1; j < chromosome.Genes.Count; j++)
            {
                var gene1 = chromosome.Genes[i];
                var gene2 = chromosome.Genes[j];
                var blueprint1 = blueprints[i];
                var blueprint2 = blueprints[j];

                if (blueprint1?.CompatibilityMatrix?.IncompatibleGenes?.Contains(gene2.GeneBlueprintId.Value) == true ||
                    blueprint2?.CompatibilityMatrix?.IncompatibleGenes?.Contains(gene1.GeneBlueprintId.Value) == true)
                {
                    errors.Add($"Incompatible genes: {gene1.GeneBlueprintId} and {gene2.GeneBlueprintId}");
                }
            }
        }

        return errors.Count > 0 ? ValidationResult.Fail(errors) : ValidationResult.Success();
    }

    public async Task<ValidationResult> ValidateGenomeAsync(
        Genome genome, 
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        foreach (var chromosome in genome.Chromosomes)
        {
            var chromosomeResult = await ValidateChromosomeAsync(chromosome, cancellationToken);
            if (!chromosomeResult.IsValid)
            {
                errors.AddRange(chromosomeResult.Messages.Select(e => $"[Chromosome {chromosome.Id}] {e}"));
            }
        }

        return errors.Count > 0 ? ValidationResult.Fail(errors) : ValidationResult.Success();
    }

    private bool IsTypeCompatible(object value, string expectedType)
    {
        if (value == null) return true;

        return expectedType.ToLower() switch
        {
            "string" => value is string,
            "int" => value is int,
            "float" => value is float,
            "double" => value is double,
            "bool" => value is bool,
            "object" => true,
            _ => true
        };
    }

    private bool HasCircularDependencies(Chromosome chromosome)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var graph = BuildDependencyGraph(chromosome);

        foreach (var gene in chromosome.Genes)
        {
            if (HasCycleDFS(gene.Id.Value, graph, visited, recursionStack))
                return true;
        }

        return false;
    }

    private Dictionary<string, List<string>> BuildDependencyGraph(Chromosome chromosome)
    {
        var graph = new Dictionary<string, List<string>>();

        foreach (var gene in chromosome.Genes)
        {
            graph[gene.Id.Value] = gene.Dependencies.Select(d => d.Value).ToList();
        }

        return graph;
    }

    private bool HasCycleDFS(string node, Dictionary<string, List<string>> graph,
                           HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (!graph.ContainsKey(node)) return false;

        if (recursionStack.Contains(node)) return true;
        if (visited.Contains(node)) return false;

        visited.Add(node);
        recursionStack.Add(node);

        foreach (var neighbor in graph[node])
        {
            if (HasCycleDFS(neighbor, graph, visited, recursionStack))
                return true;
        }

        recursionStack.Remove(node);
        return false;
    }
}