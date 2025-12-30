namespace FlowSynx.Domain.Genomes;

public record GenomeId(string Value)
{
    public static implicit operator string(GenomeId id) => id.Value;
    public static explicit operator GenomeId(string value) => new(value);
}