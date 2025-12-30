namespace FlowSynx.Domain.GeneInstances;

public record GeneInstanceId(string Value)
{
    public static implicit operator string(GeneInstanceId id) => id.Value;
    public static explicit operator GeneInstanceId(string value) => new(value);
}