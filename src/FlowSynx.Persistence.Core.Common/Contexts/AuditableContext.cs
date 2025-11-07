using FlowSynx.Domain.Audit;
using FlowSynx.Persistence.Core.Common.Models.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlowSynx.Persistence.Core.Common.Contexts;

public abstract class AuditableContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AuditEntity> Audits { get; set; }

    public virtual async Task<int> SaveChangesAsync(
        string userId, 
        CancellationToken cancellationToken = new())
    {
        var auditEntries = OnBeforeSaveChanges(userId);
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges(string userId)
    {
        ChangeTracker.DetectChanges();

        var auditEntries = new List<AuditEntry>();

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
        return entry.Entity is AuditEntity
            || entry.State is EntityState.Detached
            || entry.State is EntityState.Unchanged;
    }

    private static AuditEntry CreateAuditEntry(
        EntityEntry entry, 
        string userId)
    {
        return new AuditEntry(entry, userId, entry.Entity.GetType().Name);
    }

    private static void ProcessEntryProperties(
        EntityEntry entry, 
        AuditEntry auditEntry)
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
        AuditEntry auditEntry, 
        PropertyEntry property, 
        string propertyName)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                auditEntry.AuditType = AuditType.Create;
                auditEntry.NewValues[propertyName] = property.CurrentValue!;
                break;

            case EntityState.Deleted:
                auditEntry.AuditType = AuditType.Delete;
                auditEntry.OldValues[propertyName] = property.OriginalValue!;
                break;

            case EntityState.Modified:
                if (property.IsModified && !Equals(property.OriginalValue, property.CurrentValue))
                {
                    auditEntry.ChangedColumns.Add(propertyName);
                    auditEntry.AuditType = AuditType.Update;
                    auditEntry.OldValues[propertyName] = property.OriginalValue;
                    auditEntry.NewValues[propertyName] = property.CurrentValue!;
                }
                break;

            default:
                auditEntry.AuditType = AuditType.None;
                break;
        }
    }

    private void AddCompletedAuditEntries(IEnumerable<AuditEntry> auditEntries)
    {
        foreach (var auditEntry in auditEntries.Where(e => !e.HasTemporaryProperties))
        {
            Audits.Add(auditEntry.ToAudit());
        }
    }

    private Task OnAfterSaveChanges(
        List<AuditEntry>? auditEntries, 
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
