﻿namespace FlowSynx.Connectors.Stream.Csv.Options;

public class WriteOptions
{
    public string Headers { get; set; } = string.Empty;
    public bool? Overwite { get; set; } = false;
}