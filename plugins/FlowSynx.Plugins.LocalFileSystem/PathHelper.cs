namespace FlowSynx.Plugins.LocalFileSystem;

/// <summary>
/// Path utilities
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Character used to split paths 
    /// </summary>
    public const char PathSeparator = '/';

    public static bool IsDirectory(string path) => path[^1] == PathSeparator;

    public static bool IsFile(string path) => !IsDirectory(path);

    public static string ToUnixPath(string? path)
    {
        return path == null
            ? string.Empty
            : path.Replace("\\", "/");
    }
}