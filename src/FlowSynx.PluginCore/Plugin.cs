namespace FlowSynx.PluginCore;

public abstract class Plugin
{
    public abstract Guid Id { get; }
    public abstract string Name { get; }
    public abstract PluginNamespace Namespace { get; }
    public string Type => $"FlowSynx.{Namespace}/{Name}";
    public abstract string? Description { get; }
    public abstract PluginSpecifications? Specifications { get; set; }
    public abstract Type SpecificationsType { get; }
    public abstract Task Initialize();
    public abstract Task<object?> ExecuteAsync(PluginParameters parameters, CancellationToken cancellationToken);
}