namespace FlowSynx.Connectors.Database.MySql.Models;

/// <summary>
/// Inspired by SqlBuilder open source project (https://github.com/koshovyi/SqlBuilder/tree/master)
/// </summary>
public class JoinItem
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}