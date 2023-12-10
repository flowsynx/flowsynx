using System.Diagnostics.Contracts;
using System.IO;
using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Abstractions.Common.Helpers;

/// <summary>
/// Storage Path utilities
/// </summary>
public static class StorageHelper
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

    /// <summary>
    /// Combines parts of path
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string Combine(IEnumerable<string> parts)
    {
        return Normalize(parts == null ? null : string.Join(PathSeparatorString, parts.Where(p => !string.IsNullOrEmpty(p)).Select(NormalizePart)));
    }

    /// <summary>
    /// Gets parent path of this item.
    /// </summary>
    public static string GetParent(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        path = Normalize(path);

        var parts = Split(path);
        if (parts.Length == 0) return null;

        return parts.Length > 1
           ? Combine(parts.Take(parts.Length - 1))
           : PathSeparatorString;
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
    /// Normalizes path. Normalisation makes sure that:
    /// - When path is null or empty returns root path '/'
    /// - path separators are trimmed from both ends
    /// </summary>
    /// <param name="path"></param>
    /// <param name="removeTrailingSlash"></param>
    public static string Normalize(string path, bool removeTrailingSlash = false)
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
        //return removeTrailingSlash
        //   ? path
        //   : PathSeparatorString + path;
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
        return string.IsNullOrEmpty(path) ? null : path.Split(new[] { PathSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(NormalizePart).ToArray();
    }

    /// <summary>
    /// Checks if path is root folder path, which can be an empty string, null, or the actual root path.
    /// </summary>
    public static bool IsRootPath(string path)
    {
        return string.IsNullOrEmpty(path) || path == RootDirectoryPath;
    }

    /// <summary>
    /// Gets the root folder name
    /// </summary>
    public static string GetRootFolder(string path)
    {
        var parts = Split(path);
        return parts.Length == 1 ? null : parts[0];
    }

    /// <summary>
    /// Compare that two path entries are equal. This takes into account path entries which are slightly different as strings but identical in physical location.
    /// </summary>
    public static bool ComparePath(string path1, string path2)
    {
        return Normalize(path1) == Normalize(path2);
    }
}