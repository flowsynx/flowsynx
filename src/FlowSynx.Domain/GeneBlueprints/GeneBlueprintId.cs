namespace FlowSynx.Domain.GeneBlueprints;

public record GeneBlueprintId(string Value)
{
    public static implicit operator string(GeneBlueprintId id) => id.Value;
    public static explicit operator GeneBlueprintId(string value) => new(value);
}