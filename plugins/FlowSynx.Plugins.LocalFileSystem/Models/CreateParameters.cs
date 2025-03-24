namespace FlowSynx.Plugins.LocalFileSystem.Models;

public class CreateParameters
{
    public string Path { get; set; } = string.Empty;
    public bool? Hidden { get; set; } = false;
}