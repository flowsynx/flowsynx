namespace FlowSynx.Plugins.LocalFileSystem.Models;

internal class CreateParameters
{
    public string Path { get; set; } = string.Empty;
    public bool? Hidden { get; set; } = false;
}