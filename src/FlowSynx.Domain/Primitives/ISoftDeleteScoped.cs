namespace FlowSynx.Domain.Primitives;

public interface ISoftDeleteScoped
{
    bool IsDeleted { get; set; }
}