using FlowSynx.Application.Core.Services;
using Microsoft.AspNetCore.SignalR;

namespace FlowSynx.Services;

public class SignalREventPublisher<THub> : IEventPublisher where THub : Hub
{
    private readonly IHubContext<THub> _hubContext;

    public SignalREventPublisher(IHubContext<THub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task PublishAsync<TEvent>(
        string methodName,
        TEvent @event, 
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await _hubContext.Clients.All.SendAsync(
            methodName,
            @event,
            cancellationToken
        );
    }

    public async Task PublishToUserAsync<TEvent>(
        string userId,
        string methodName,
        TEvent @event, 
        CancellationToken cancellationToken = default)
        where TEvent : class
    {
        await _hubContext.Clients.User(userId).SendAsync(
            methodName,
            @event,
            cancellationToken
        );
    }
}