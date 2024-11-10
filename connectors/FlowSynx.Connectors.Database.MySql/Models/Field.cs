﻿namespace FlowSynx.Connectors.Database.MySql.Models;

public class Field
{
    public required string Name { get; set; }
    public string? Alias { get; set; }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Alias) 
            ? $"{Name}" 
            : $"{Name} AS {Alias}";
    }
}
