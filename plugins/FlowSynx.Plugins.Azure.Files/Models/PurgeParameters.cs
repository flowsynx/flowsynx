namespace FlowSynx.Plugins.Azure.Files.Models;

internal class PurgeParameters
{
    public string Path { get; set; } = string.Empty;
    public bool? Force { get; set; } = false;
}