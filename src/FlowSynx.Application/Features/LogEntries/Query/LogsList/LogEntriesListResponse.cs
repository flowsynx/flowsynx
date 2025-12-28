namespace FlowSynx.Application.Features.LogEntries.Query.LogEntriesList;

public class LogEntriesListResponse
{
    public Guid Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public string? Exception { get; set; }
}