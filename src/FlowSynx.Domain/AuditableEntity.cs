namespace FlowSynx.Domain;

public abstract class AuditableEntity : IAuditableEntity
{
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}

public abstract class AuditableEntity<TId> : IAuditableEntity<TId>
{
    public required TId Id { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
