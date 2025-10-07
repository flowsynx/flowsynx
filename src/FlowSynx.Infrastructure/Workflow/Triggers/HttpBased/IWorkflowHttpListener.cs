namespace FlowSynx.Infrastructure.Workflow.Triggers.HttpBased;

public interface IWorkflowHttpListener
{
    void RegisterRoute(string userId, string method, string route, Func<HttpRequestData, Task> handler);
    bool TryGetHandler(string userId, string method, string path, out Func<HttpRequestData, Task>? handler);
}