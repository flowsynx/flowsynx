namespace FlowSynx.Domain.Primitives;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}