namespace FlowSynx.Domain.DomainEvents;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
