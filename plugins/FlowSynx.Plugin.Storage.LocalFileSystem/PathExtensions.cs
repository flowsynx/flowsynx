namespace FlowSynx.Plugin.Storage.LocalFileSystem;

internal static class PathExtensions
{
    public static string ToUnixPath(this string? path)
    {
        return path == null 
            ? string.Empty 
            : path.Replace("\\", "/");
    }
}