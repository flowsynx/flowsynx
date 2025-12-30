namespace FlowSynx.Domain.Chromosomes;

public record ChromosomeId(string Value)
{
    public static implicit operator string(ChromosomeId id) => id.Value;
    public static explicit operator ChromosomeId(string value) => new(value);
}