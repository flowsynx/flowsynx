using FlowSync.Abstractions.Helpers;

namespace FlowSync.Abstractions.Entities;

public sealed class FileSystemEntity : IEquatable<FileSystemEntity>, IComparable<FileSystemEntity>, ICloneable
{
    [FilterMember]
    public string Id => GetEntityId();

    [FilterMember]
    public EntityItemKind Kind { get; }

    private bool IsDirectory => Kind == EntityItemKind.Directory;

    private bool IsFile => Kind == EntityItemKind.File;

    public string DirectoryPath { get; private set; } = null!;

    [FilterMember]
    public string Name { get; private set; } = null!;

    [FilterMember]
    public long? Size { get; set; }

    [FilterMember]
    public string? MimeType => GetMimeType();

    public string? HashCode { get; set; }

    public DateTimeOffset? CreatedTime { get; set; }

    [FilterMember]
    public DateTimeOffset? ModifiedTime { get; set; }

    public string FullPath => PathHelper.Combine(DirectoryPath, Name);

    public Dictionary<string, object> Properties { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public bool TryGetProperty<TValue>(string name, out TValue value, TValue defaultValue)
    {
        if (string.IsNullOrEmpty(name) || !Properties.TryGetValue(name, out var objValue))
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

    public void TryAddProperties(params object[] keyValues)
    {
        for (var i = 0; i < keyValues.Length; i += 2)
        {
            var key = (string)keyValues[i];
            var value = keyValues[i + 1];

            if (value is string s && string.IsNullOrEmpty(s))
                continue;

            Properties[key] = value;
        }
    }

    public Dictionary<string, string> Metadata { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public FileSystemEntity(string fullPath, EntityItemKind kind)
    {
        SetFullPath(fullPath);
        Kind = kind;
    }
    
    public FileSystemEntity(string folderPath, string name, EntityItemKind kind)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Name = PathHelper.NormalizePart(Name);
        DirectoryPath = PathHelper.Normalize(folderPath);
        Kind = kind;
    }

    public bool IsRootFolder => Kind == EntityItemKind.Directory && PathHelper.IsRootPath(FullPath);

    public override string ToString()
    {
        var k = Kind ==  EntityItemKind.File ? "file" : "directory";
        return $"{k}: {Name}@{DirectoryPath}";
    }

    public string GetEntityId()
    {
        return HashHelper.CreateMd5($"{this}");
    }

    public string GetMimeType() => MimeTypeHelper.GetMimeType(GetExtension());

    public override int GetHashCode()
    {
        return FullPath.GetHashCode() * Kind.GetHashCode();
    }
    
    public static implicit operator FileSystemEntity(string fullPath)
    {
        return new FileSystemEntity(fullPath, EntityItemKind.File);
    }

    public static implicit operator string(FileSystemEntity fileSystemEntity)
    {
        return fileSystemEntity.FullPath;
    }
    
    public void PrependPath(string path)
    {
        if (string.IsNullOrEmpty(path) || PathHelper.IsRootPath(path))
            return;

        DirectoryPath = PathHelper.Combine(path, DirectoryPath);
    }

    public void SetFullPath(string fullPath)
    {
        var path = PathHelper.Normalize(fullPath);

        if (PathHelper.IsRootPath(path))
        {
            Name = PathHelper.RootDirectoryPath;
            DirectoryPath = PathHelper.RootDirectoryPath;
        }
        else
        {
            var parts = PathHelper.Split(path);

            Name = parts.Last();
            DirectoryPath = PathHelper.GetParent(path);
        }
    }

    public string? GetExtension()
    {
        if (!IsFile)
            throw new ArgumentException("The specified FileSystemPath is not a file.");
        var name = Name;
        var extensionIndex = name?.LastIndexOf('.') ?? -1;
        return extensionIndex < 0 ? "" : name?[extensionIndex..];
    }

    public object Clone()
    {
        var clone = (FileSystemEntity)MemberwiseClone();
        clone.Metadata = new Dictionary<string, string>(Metadata, StringComparer.OrdinalIgnoreCase);
        clone.Properties = new Dictionary<string, object>(Properties, StringComparer.OrdinalIgnoreCase);
        return clone;
    }

    public int CompareTo(FileSystemEntity? other)
    {
        return string.Compare(FullPath, other?.FullPath, StringComparison.Ordinal);
    }

    public bool Equals(FileSystemEntity? other)
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

        return other is FileSystemEntity path && Equals(path);
    }

    public static bool operator ==(FileSystemEntity pathA, FileSystemEntity pathB)
    {
        return pathA.Equals(pathB);
    }

    public static bool operator !=(FileSystemEntity pathA, FileSystemEntity pathB)
    {
        return !(pathA == pathB);
    }
}