﻿using System.Text.Json;
using FlowSynx.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlowSynx.Persistence.Postgres.Models.Audit;

public class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public string UserId { get; set; }
    public string TableName { get; set; }
    public Dictionary<string, object> KeyValues { get; } = new();
    public Dictionary<string, object> OldValues { get; } = new();
    public Dictionary<string, object> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();
    public AuditType AuditType { get; set; }
    public List<string> ChangedColumns { get; } = new();
    public bool HasTemporaryProperties => TemporaryProperties.Any();

    public AuditEntity ToAudit()
    {
        var audit = new AuditEntity
        {
            UserId = UserId,
            Type = AuditType.ToString(),
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