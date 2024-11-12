namespace FlowSynx.Connectors.Database.MySql.Models;

public class Sort
{
    public required string Name { get; set; }
    public string? Direction { get; set; }

    public string GetDirection()
    {
        if (string.IsNullOrEmpty(Direction))
            return "ASC";

        if ((Direction).Equals("ASC", StringComparison.OrdinalIgnoreCase))
            return "ASC";
        
        if ((Direction).Equals("DESC", StringComparison.OrdinalIgnoreCase))
            return "DESC";

        throw new Exception("Sort direction is not supported. It should be ASC or DESC");
    }
}
