namespace FlowSynx.Plugins.LocalFileSystem.Models;

public class PurgeParameters
{
    public string Path { get; set; } = string.Empty;
    public bool? Force { get; set; } = false;
}