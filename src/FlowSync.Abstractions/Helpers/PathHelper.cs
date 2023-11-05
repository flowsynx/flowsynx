namespace FlowSync.Abstractions.Helpers;

internal class PathHelper
{
    public const char PathSeparator = '/';
    public static readonly string PathSeparatorString = new string(PathSeparator, 1);
    public static readonly string RootDirectoryPath = "/";
    public static readonly string LevelUpDirectoryName = "..";

    public static string Combine(IEnumerable<string> parts)
    {
        if (!parts.Any()) return Normalize(null);
        return Normalize(string.Join(PathSeparatorString, parts.Where(p => !string.IsNullOrEmpty(p)).Select(NormalizePart)));
    }

    public static string? GetParent(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        path = Normalize(path);

        var parts = Split(path);
        if (parts.Length == 0) return null;

        return parts.Length > 1
           ? Combine(parts.Take(parts.Length - 1))
           : PathSeparatorString;
    }

    public static string Combine(params string[] parts)
    {
        return Combine((IEnumerable<string>)parts);
    }

    public static string Normalize(string? path, bool removeTrailingSlash = false)
    {
        if (IsRootPath(path)) return RootDirectoryPath;

        var parts = Split(path);

        var r = new List<string>(parts.Length);
        foreach (var part in parts)
        {
            if (part == LevelUpDirectoryName)
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

        return removeTrailingSlash
           ? path
           : PathSeparatorString + path;
    }

    public static string NormalizePart(string part)
    {
        if (part == null) throw new ArgumentNullException(nameof(part));
        return part.Trim(PathSeparator);
    }

    public static string[]? Split(string? path)
    {
        return string.IsNullOrEmpty(path)
            ? null
            : path.Split(new[] { PathSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(NormalizePart).ToArray();
    }

    public static bool IsRootPath(string? path)
    {
        return string.IsNullOrEmpty(path) || path == RootDirectoryPath;
    }

    public static string? GetRootFolder(string path)
    {
        var parts = Split(path);
        return parts.Length == 1 ? null : parts[0];
    }

    public static string RemoveRootFolder(string path)
    {
        var parts = Split(path);
        return parts.Length == 1 ? path : Combine(parts.Skip(1));
    }

    public static bool ComparePath(string path1, string path2)
    {
        return Normalize(path1) == Normalize(path2);
    }

    public static string Rename(string path, string newFileName)
    {
        var parts = Split(path);
        return parts!.Length == 1 ? newFileName : Combine(Combine(parts.Take(parts.Length - 1)), newFileName);
    }
}