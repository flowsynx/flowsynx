namespace FlowSynx.Plugins.Azure.Blobs.Models;

internal class WriteParameters
{
    public string? Path { get; set; }
    public object? Data { get; set; }
    public bool Overwrite { get; set; } = false;
}