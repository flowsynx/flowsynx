namespace FlowSynx.Domain.DomainEvents;

public abstract record DomainEvent: IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public string EventType { get; } = string.Empty;

    protected DomainEvent()
    {
        EventType = GetType().Name;
    }
}
