﻿namespace FlowSynx.PluginCore;

public class PluginContextData: IEquatable<PluginContextData>, ICloneable
{
    public string Id { get; set; }
    public string SourceType { get; set; }  // e.g., "Database", "File", "Blob"
    public string? Format { get; set; }  // e.g., "CSV", "JSON", "XML", "Binary"
    public Dictionary<string, object> Metadata { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    public byte[]? RawData { get; set; }  // For binary content
    public string? Content { get; set; }  // For text-based content
    public List<Dictionary<string, object>>? StructuredData { get; set; }  // For tabular data

    public PluginContextData(string id, string sourceType)
    {
        Id = id;
        SourceType = sourceType;
    }

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

    public void TryAddMetadata(string key, object? value)
    {
        if (key != null && value != null)
        {
            Metadata.TryAdd(key, value);
        }
    }

    public void TryAddMetadata(params object[] keyValues)
    {
        for (var i = 0; i < keyValues.Length; i += 2)
        {
            var key = (string)keyValues[i];
            var value = keyValues[i + 1];

            if (key != null && value != null)
            {
                if (value is string s && string.IsNullOrEmpty(s))
                    continue;

                Metadata[key] = value;
            }
        }
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode() * SourceType.GetHashCode();
    }

    public override string ToString()
    {
        return $"{SourceType}:{Id}";
    }

    public static implicit operator string(PluginContextData storageEntity)
    {
        return storageEntity.Id;
    }

    public static bool operator ==(PluginContextData pathA, PluginContextData pathB)
    {
        return pathA.Equals(pathB);
    }

    public static bool operator !=(PluginContextData pathA, PluginContextData pathB)
    {
        return !(pathA == pathB);
    }

    public bool Equals(PluginContextData? other)
    {
        if (ReferenceEquals(other, null))
            return false;

        return other.Id == Id && other.SourceType == SourceType;
    }

    public override bool Equals(object? other)
    {
        if (ReferenceEquals(other, null))
            return false;
        if (ReferenceEquals(other, this))
            return true;

        return other is PluginContextData path && Equals(path);
    }

    public object Clone()
    {
        var clone = (PluginContextData)MemberwiseClone();
        clone.Metadata = new Dictionary<string, object>(Metadata, StringComparer.OrdinalIgnoreCase);
        return clone;
    }
}