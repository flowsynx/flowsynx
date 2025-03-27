namespace FlowSynx.Plugins.LocalFileSystem.Models;

internal class LocalFileEntity
{
    public string Key { get; set; }
    public string Path { get; set; }
    public string? Content { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}