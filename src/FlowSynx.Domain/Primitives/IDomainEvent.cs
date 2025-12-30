namespace FlowSynx.Domain.Primitives;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
