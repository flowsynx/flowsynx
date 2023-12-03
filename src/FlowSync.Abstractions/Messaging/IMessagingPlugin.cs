namespace FlowSync.Abstractions.Messaging;

public interface IMessagingPlugin : IPlugin, IDisposable
{
    void SetSpecifications(IDictionary<string, object>? specifications);
}