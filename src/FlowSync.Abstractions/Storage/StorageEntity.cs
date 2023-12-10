using EnsureThat;
using FlowSync.Abstractions.Common.Attributes;
using FlowSync.Abstractions.Common.Helpers;
using FlowSync.Abstractions.Exceptions;

namespace FlowSync.Abstractions.Storage;

public sealed class StorageEntity : IEquatable<StorageEntity>, IComparable<StorageEntity>, ICloneable
{
    [SortMember]
    public string Id => HashHelper.CreateMd5($"{this}");

    [SortMember]
    public StorageEntityItemKind Kind { get; }

    public bool IsDirectory => Kind == StorageEntityItemKind.Directory;

    public bool IsFile => Kind == StorageEntityItemKind.File;

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

    public string FullPath => StorageHelper.Combine(DirectoryPath, Name);

    public Dictionary<string, object> Metadata { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public bool IsRootFolder => Kind == StorageEntityItemKind.Directory && StorageHelper.IsRootPath(FullPath);

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
        Name = StorageHelper.NormalizePart(Name);
        DirectoryPath = StorageHelper.Normalize(folderPath);
        Kind = kind;
    }

    public string? GetExtension()
    {
        if (!IsFile)
            throw new StorageException(FlowSyncAbstractionsResource.StorageEntityGetExtensionTheSpecifiedPathIsNotAFile);

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
        var path = StorageHelper.Normalize(fullPath);

        if (StorageHelper.IsRootPath(path))
        {
            Name = StorageHelper.RootDirectoryPath;
            DirectoryPath = StorageHelper.RootDirectoryPath;
        }
        else
        {
            var parts = StorageHelper.Split(path);

            Name = parts.Last();
            DirectoryPath = StorageHelper.GetParent(path);
        }
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