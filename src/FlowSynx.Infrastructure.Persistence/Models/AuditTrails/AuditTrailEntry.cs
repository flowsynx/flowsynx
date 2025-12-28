using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Enums;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Persistence.Models.AuditTrails;

public class AuditTrailEntry(EntityEntry entry, string userId, string tableName)
{
    public EntityEntry Entry { get; } = entry;
    public string UserId { get; set; } = userId;
    public string TableName { get; set; } = tableName;
    public Dictionary<string, object> KeyValues { get; } = new();
    public Dictionary<string, object> OldValues { get; } = new();
    public Dictionary<string, object> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();
    public AuditTrailType AuditTrailType { get; set; }
    public List<string> ChangedColumns { get; } = new();
    public bool HasTemporaryProperties => TemporaryProperties.Any();

    public AuditTrail ToAudit()
    {
        var audit = new AuditTrail
        {
            UserId = UserId,
            Type = AuditTrailType.ToString(),
            TableName = TableName,
            DateTime = DateTime.UtcNow,
            PrimaryKey = JsonSerializer.Serialize(KeyValues),
            OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues),
            AffectedColumns = ChangedColumns.Count == 0 ? null : JsonSerializer.Serialize(ChangedColumns)
        };
        return audit;
    }
}