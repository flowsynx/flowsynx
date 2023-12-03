using EnsureThat;
using FlowSync.Abstractions.Common.Attributes;
using FlowSync.Abstractions.Common.Helpers;

namespace FlowSync.Abstractions.Storage;

public sealed class StorageEntity : IEquatable<StorageEntity>, IComparable<StorageEntity>, ICloneable
{
    private const char PathSeparator = '/';
    private readonly string _pathSeparatorString = new string(PathSeparator, 1);
    private const string RootDirectoryPath = "/";
    private const string LevelUpDirectoryName = "..";

    [SortMember]
    public string Id => HashHelper.CreateMd5($"{this}");

    [SortMember]
    public StorageEntityItemKind Kind { get; }

    private bool IsDirectory => Kind == StorageEntityItemKind.Directory;

    private bool IsFile => Kind == StorageEntityItemKind.File;

    public string DirectoryPath { get; private set; } = null!;

    [SortMember]
    public string Name { get; private set; } = null!;

    [SortMember]
    public long? Size { get; set; }

    [SortMember]
    public string? MimeType => IsFile ? MimeTypeMap.GetMimeType(GetExtension()) : "";

    public string? HashCode { get; set; }

    public DateTimeOffset? CreatedTime { get; set; }

    [SortMember]
    public DateTimeOffset? ModifiedTime { get; set; }

    public string FullPath => Combine(DirectoryPath, Name);

    public Dictionary<string, object> Metadata { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public bool IsRootFolder => Kind == StorageEntityItemKind.Directory && IsRootPath(FullPath);

    public bool TryGetMetadata<TValue>(string name, out TValue value, TValue defaultValue)
    {
        if (string.IsNullOrEmpty(name) || !Metadata.TryGetValue(name, out var objValue))
        {
            value = defaultValue;
            return false;
        }

        if (objValue is TValue val)
        {
            value = val;
            return true;
        }

        value = defaultValue;
        return false;
    }

    public void TryAddMetadata(params object[] keyValues)
    {
        for (var i = 0; i < keyValues.Length; i += 2)
        {
            var key = (string)keyValues[i];
            var value = keyValues[i + 1];

            if (value is string s && string.IsNullOrEmpty(s))
                continue;

            Metadata[key] = value;
        }
    }

    public StorageEntity(string fullPath, StorageEntityItemKind kind)
    {
        SetFullPath(fullPath);
        Kind = kind;
    }

    public StorageEntity(string folderPath, string name, StorageEntityItemKind kind)
    {
        EnsureArg.IsNotNullOrEmpty(name, nameof(name));
        Name = name;
        Name = NormalizePart(Name);
        DirectoryPath = Normalize(folderPath);
        Kind = kind;
    }

    public string? GetExtension()
    {
        if (!IsFile)
            throw new ArgumentException("The specified FileSystemPath is not a file.");
        var name = Name;
        var extensionIndex = name?.LastIndexOf('.') ?? -1;
        return extensionIndex < 0 ? "" : name?[extensionIndex..];
    }

    public override int GetHashCode()
    {
        return FullPath.GetHashCode() * Kind.GetHashCode();
    }

    public override string ToString()
    {
        var k = Kind == StorageEntityItemKind.File ? "file" : "directory";
        return $"{k}: {Name}@{DirectoryPath}";
    }

    public static implicit operator StorageEntity(string fullPath)
    {
        return new StorageEntity(fullPath, StorageEntityItemKind.File);
    }

    public static implicit operator string(StorageEntity storageEntity)
    {
        return storageEntity.FullPath;
    }

    public static bool operator ==(StorageEntity pathA, StorageEntity pathB)
    {
        return pathA.Equals(pathB);
    }

    public static bool operator !=(StorageEntity pathA, StorageEntity pathB)
    {
        return !(pathA == pathB);
    }

    private void SetFullPath(string fullPath)
    {
        var path = Normalize(fullPath);

        if (IsRootPath(path))
        {
            Name = RootDirectoryPath;
            DirectoryPath = RootDirectoryPath;
        }
        else
        {
            var parts = Split(path);

            Name = parts.Last();
            DirectoryPath = GetParent(path);
        }
    }

    private string Normalize(string path, bool removeTrailingSlash = false)
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
        path = string.Join(_pathSeparatorString, r);

        return path;
        //return removeTrailingSlash
        //   ? path
        //   : PathSeparatorString + path;
    }

    private bool IsRootPath(string path)
    {
        return string.IsNullOrEmpty(path) || path == RootDirectoryPath;
    }

    private string[] Split(string path)
    {
        return path.Split(new[] { PathSeparator }, StringSplitOptions.RemoveEmptyEntries).Select(NormalizePart).ToArray();
    }

    private string Combine(params string[] parts)
    {
        return Combine(parts.AsEnumerable());
    }

    private string Combine(IEnumerable<string> parts)
    {
        var enumerable = parts.ToList();
        if (!enumerable.Any()) return Normalize(string.Empty);
        return Normalize(string.Join(_pathSeparatorString, enumerable.Where(p => !string.IsNullOrEmpty(p)).Select(NormalizePart)));
    }

    private string GetParent(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        path = Normalize(path);

        var parts = Split(path);
        if (parts.Length == 0) return string.Empty;

        return parts.Length > 1 ? Combine(parts.Take(parts.Length - 1)) : _pathSeparatorString;
    }

    private string NormalizePart(string part)
    {
        if (part == null) throw new ArgumentNullException(nameof(part));
        return part.Trim(PathSeparator);
    }

    public int CompareTo(StorageEntity? other)
    {
        return string.Compare(FullPath, other?.FullPath, StringComparison.Ordinal);
    }

    public bool Equals(StorageEntity? other)
    {
        if (ReferenceEquals(other, null))
            return false;

        return other.FullPath == FullPath && other.Kind == Kind;
    }

    public override bool Equals(object? other)
    {
        if (ReferenceEquals(other, null))
            return false;
        if (ReferenceEquals(other, this))
            return true;

        return other is StorageEntity path && Equals(path);
    }

    public object Clone()
    {
        var clone = (StorageEntity)MemberwiseClone();
        clone.Metadata = new Dictionary<string, object>(Metadata, StringComparer.OrdinalIgnoreCase);
        return clone;
    }
}