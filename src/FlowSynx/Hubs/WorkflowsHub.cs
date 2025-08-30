using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlowSynx.Hubs;

[Authorize]
public class WorkflowsHub : Hub
{
    public async Task NotifyUpdateAsync(object update)
    {
        var userId = Context.User?.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
            return; // optionally log unauthorized attempt

        await Clients.User(userId).SendAsync("WorkflowUpdated", update);
    }
}