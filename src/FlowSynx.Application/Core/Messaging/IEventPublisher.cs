namespace FlowSynx.Application.Core.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        string methodName,
        TEvent @event, 
        CancellationToken cancellationToken = default)
        where TEvent : class;

    Task PublishToUserAsync<TEvent>(
        string userId,
        string methodName,
        TEvent @event, 
        CancellationToken cancellationToken = default)
        where TEvent : class;
}