namespace FlowSynx.Domain.Primitives;

public abstract class AuditableEntity : AuditableEntity<Guid>
{

}

public abstract class AuditableEntity<TId> : Entity<TId> where TId : notnull
{
    public string? CreatedBy { get; set; }
    public DateTime? CreatedOn { get; set; }
    public string? LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
