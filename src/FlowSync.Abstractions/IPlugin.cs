namespace FlowSync.Abstractions;

public interface IPlugin
{
    Guid Id { get; }
    string Name { get; }
    PluginNamespace Namespace { get; }
    string Type => $"FlowSync.{Namespace}/{Name}";
    string? Description { get; }
}