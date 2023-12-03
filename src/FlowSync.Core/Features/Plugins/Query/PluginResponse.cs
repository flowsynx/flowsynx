﻿namespace FlowSync.Core.Features.Plugins.Query;

public class PluginResponse
{
    public required Guid Id { get; set; }
    public required string Type { get; set; }
    public string? Description { get; set; }
}