namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeExecutionsList;

public class ChromosomeExecutionsListResult
{
    public string ExecutionId { get; set; }
    public string TargetType { get; set; } // "gene", "chromosome", "genome"
    public Guid TargetId { get; set; }
    public string TargetName { get; set; }
    public string Namespace { get; set; }
    public Dictionary<string, object> Request { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Response { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public string Status { get; set; } // "pending", "running", "completed", "failed", "cancelled"
    public int Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public long Duration { get; set; }
    public string TriggeredBy { get; set; }
    public ICollection<ChromosomeExecutionsLog> Logs { get; set; } = new List<ChromosomeExecutionsLog>();
    public ICollection<ChromosomeExecutionsArtifact> Artifacts { get; set; } = new List<ChromosomeExecutionsArtifact>();
}

public class ChromosomeExecutionsLog
{
    public string Level { get; set; } // "info", "warn", "error", "debug"
    public string Message { get; set; }
    public string Source { get; set; }
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ChromosomeExecutionsArtifact
{
    public string Name { get; set; }
    public string Type { get; set; } // "file", "data", "report"
    public object Content { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}