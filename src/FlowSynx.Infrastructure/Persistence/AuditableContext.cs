using FlowSynx.Domain.Entities;
using FlowSynx.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlowSynx.Infrastructure.Persistence;

public abstract class AuditableContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AuditTrail> Audits { get; set; }

    public virtual async Task<int> SaveChangesAsync(
        string userId, 
        CancellationToken cancellationToken = new())
    {
        var auditEntries = OnBeforeSaveChanges(userId);
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditTrailEntry> OnBeforeSaveChanges(string userId)
    {
        ChangeTracker.DetectChanges();

        var auditEntries = new List<AuditTrailEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (ShouldSkipEntry(entry))
                continue;

            var auditEntry = CreateAuditEntry(entry, userId);
            auditEntries.Add(auditEntry);

            ProcessEntryProperties(entry, auditEntry);
        }

        AddCompletedAuditEntries(auditEntries);

        return auditEntries
            .Where(e => e.HasTemporaryProperties)
            .ToList();
    }

    private static bool ShouldSkipEntry(EntityEntry entry)
    {
        return entry.Entity is AuditTrailEntry
            || entry.State is EntityState.Detached
            || entry.State is EntityState.Unchanged;
    }

    private static AuditTrailEntry CreateAuditEntry(
        EntityEntry entry, 
        string userId)
    {
        return new AuditTrailEntry(entry, userId, entry.Entity.GetType().Name);
    }

    private static void ProcessEntryProperties(
        EntityEntry entry, 
        AuditTrailEntry auditEntry)
    {
        foreach (var property in entry.Properties)
        {
            if (property.IsTemporary)
            {
                auditEntry.TemporaryProperties.Add(property);
                continue;
            }

            var propertyName = property.Metadata.Name;

            if (property.Metadata.IsPrimaryKey())
            {
                auditEntry.KeyValues[propertyName] = property.CurrentValue!;
                continue;
            }

            HandlePropertyChange(entry, auditEntry, property, propertyName);
        }
    }

    private static void HandlePropertyChange(
        EntityEntry entry, 
        AuditTrailEntry auditEntry, 
        PropertyEntry property, 
        string propertyName)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                auditEntry.AuditTrailType = AuditTrailType.Create;
                auditEntry.NewValues[propertyName] = property.CurrentValue!;
                break;

            case EntityState.Deleted:
                auditEntry.AuditTrailType = AuditTrailType.Delete;
                auditEntry.OldValues[propertyName] = property.OriginalValue!;
                break;

            case EntityState.Modified:
                if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                {
                    auditEntry.ChangedColumns.Add(propertyName);
                    auditEntry.AuditTrailType = AuditTrailType.Update;
                    auditEntry.OldValues[propertyName] = property.OriginalValue;
                    auditEntry.NewValues[propertyName] = property.CurrentValue!;
                }
                break;

            default:
                auditEntry.AuditTrailType = AuditTrailType.None;
                break;
        }
    }

    private void AddCompletedAuditEntries(IEnumerable<AuditTrailEntry> auditEntries)
    {
        foreach (var auditEntry in auditEntries.Where(e => !e.HasTemporaryProperties))
        {
            Audits.Add(auditEntry.ToAudit());
        }
    }

    private Task OnAfterSaveChanges(
        List<AuditTrailEntry>? auditEntries, 
        CancellationToken cancellationToken = new())
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue!;
                }
            }
            Audits.Add(auditEntry.ToAudit());
        }
        return SaveChangesAsync(cancellationToken);
    }
}
