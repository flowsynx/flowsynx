namespace FlowSync.Abstractions.KeyValues;

public interface IKeyValuePlugin : IPlugin, IDisposable
{
    void SetSpecifications(IDictionary<string, object>? specifications);
}