﻿namespace FlowSynx.Core.Models;

public class JsonSerializationConfiguration
{
    public bool Indented { get; set; } = false;
    public bool NameCaseInsensitive { get; set; } = true;
    public List<object>? Converters { get; set; }
}