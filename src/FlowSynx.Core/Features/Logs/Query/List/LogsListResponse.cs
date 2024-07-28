namespace FlowSynx.Core.Features.Logs.Query.List;

public class LogsListResponse
{
    public DateTime TimeStamp { get; set; }
    public required string Level { get; set; }
    public string? Machine { get; set; }
    public string? UserName { get; set; }
    public required string Message { get; set; }
}