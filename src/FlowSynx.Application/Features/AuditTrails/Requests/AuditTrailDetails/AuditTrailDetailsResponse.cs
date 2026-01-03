namespace FlowSynx.Application.Features.AuditTrails.Requests.AuditTrailDetails;

public class AuditTrailDetailsResult
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public string? PrimaryKey { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedColumns { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}