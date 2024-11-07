namespace FlowSynx.Connectors.Database.MySql.Models;

public class Sort
{
    public required string Name { get; set; }
    public string? Direction { get; set; }

    public override string ToString()
    {
        return $"{Name} {Direction}";
    }
}
