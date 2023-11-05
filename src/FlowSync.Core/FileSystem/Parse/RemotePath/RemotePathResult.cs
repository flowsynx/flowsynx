namespace FlowSync.Core.FileSystem.Parse.RemotePath;

internal class RemotePathResult
{
    public required string FileSystemName { get; set; }
    public required string FileSystemType { get; set; }
    public required IDictionary<string, object>? Specifications { get; set; }
    public required string Path { get; set; }
}