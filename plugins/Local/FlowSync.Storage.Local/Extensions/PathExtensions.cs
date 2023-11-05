namespace FlowSync.Storage.Local.Extensions;

internal static class PathExtensions
{
    public static string ToUnixPath(this string path)
    {
        return path.Replace("\\", "/");
    }

    public static string ToWindowsPath(this string path)
    {
        return path.Replace("/", "\\");
    }
}