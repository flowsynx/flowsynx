namespace FlowSynx.Plugins.LocalFileSystem.Models;

public class WriteParameter
{
    public object Data { get; set; }
    public bool Overwrite { get; set; } = false;
}