using System;
using System.Collections.Generic;
using System.Text;

namespace FlowSynx.Application.Models;

public class ExecutionArtifact
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public object? Content { get; set; }
    public long Size { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
