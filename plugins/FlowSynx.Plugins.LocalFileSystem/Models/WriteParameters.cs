namespace FlowSynx.Plugins.LocalFileSystem.Models;

public class WriteParameters
{
    public string? Path { get; set; }
    public object? Data { get; set; }
    public bool Overwrite { get; set; } = false;
}