namespace FlowSynx.Plugins.Amazon.S3;

/// <summary>
/// Path utilities
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Character used to split paths 
    /// </summary>
    public const char PathSeparator = '/';

    /// <summary>
    /// Character used to split paths as a string value
    /// </summary>
    public static readonly string PathSeparatorString = new string(PathSeparator, 1);

    /// <summary>
    /// Returns '/'
    /// </summary>
    public static readonly string RootDirectoryPath = "/";

    /// <summary>
    /// Folder name for leveling up the path
    /// </summary>
    public static readonly string LevelUpFolderName = "..";

    public static bool IsDirectory(string path) => path[^1] == PathSeparator;

    public static bool IsFile(string path) => !IsDirectory(path);

    public static string AddTrailingPathSeparator(string path)
    {
        return AddPathSeparator(path, path.Length);
    }

    public static string AddPathSeparator(string path, int index)
    {
        return !path.EndsWith("/") ? path.Insert(index, PathSeparatorString) : path;
    }

    /// <summary>
    /// Combines parts of path
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string Combine(IEnumerable<string> parts)
    {
        return Normalize(string.Join(PathSeparatorString, parts.Where(p => !string.IsNullOrEmpty(p)).Select(NormalizePart)));
    }

    /// <summary>
    /// Combines parts of path
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string Combine(params string[] parts)
    {
        return Combine((IEnumerable<string>)parts);
    }

    /// <summary>
    /// Gets parent path of this item.
    /// </summary>
    public static string GetParent(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        path = Normalize(path);

        var parts = Split(path);
        if (parts.Length == 0) return string.Empty;

        return parts.Length > 1
           ? AddTrailingPathSeparator(Combine(parts.Take(parts.Length - 1)))
           : PathSeparatorString;
    }

    /// <summary>
    /// Normalizes path. Normalization makes sure that:
    /// - When path is null or empty returns root path '/'
    /// - path separators are trimmed from both ends
    /// </summary>
    /// <param name="path"></param>
    public static string Normalize(string path)
    {
        if (IsRootPath(path)) return RootDirectoryPath;

        var parts = Split(path);

        var r = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (part == LevelUpFolderName)
            {
                if (r.Count > 0)
                {
                    r.RemoveAt(r.Count - 1);
                }
            }
            else
            {
                r.Add(part);
            }

        }
        path = string.Join(PathSeparatorString, r);
        return path;
    }

    /// <summary>
    /// Normalizes path part
    /// </summary>
    /// <param name="part"></param>
    /// <returns></returns>
    public static string NormalizePart(string part)
    {
        if (part == null) throw new ArgumentNullException(nameof(part));
        return part.Trim(PathSeparator);
    }

    /// <summary>
    /// Splits path in parts. Leading and trailing path separators are totally ignored. Note that it returns
    /// null if input path is null. Parent folder signatures are returned as a part of split, they are not removed.
    /// If you want to get an absolute normalized path use <see cref="Normalize(string, bool)"/>
    /// </summary>
    public static string[] Split(string path)
    {
        return string.IsNullOrEmpty(path) ? new string[]{} : path.Split(new[] { PathSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(NormalizePart).ToArray();
    }

    /// <summary>
    /// Checks if path is root folder path, which can be an empty string, null, or the actual root path.
    /// </summary>
    public static bool IsRootPath(string path)
    {
        return string.IsNullOrEmpty(path) || path == RootDirectoryPath;
    }

    /// <summary>
    /// Compare that two path entries are equal. This takes into account path entries which are slightly different as strings but identical in physical location.
    /// </summary>
    public static bool ComparePath(string path1, string path2)
    {
        return Normalize(path1) == Normalize(path2);
    }

    public static void CopyFilesRecursively(string sourcePath, string targetPath, CancellationToken cancellationToken)
    {
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

    public static string ToUnixPath(string? path)
    {
        return path == null
            ? string.Empty
            : path.Replace("\\", "/");
    }
}