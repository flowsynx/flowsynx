namespace FlowSynx.Domain;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}