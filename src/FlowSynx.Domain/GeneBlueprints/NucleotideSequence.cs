namespace FlowSynx.Domain.GeneBlueprints;

public record NucleotideSequence(
        string Name,
        string Type, // "string", "int", "float", "bool", "object"
        string Description,
        object DefaultValue,
        bool Required = false,
        List<string> ValidationRules = null);
