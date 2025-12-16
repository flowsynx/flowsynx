namespace FlowSynx.Application.PluginHost.Parser;

public sealed class ParsedPluginType
{
    private const string LatestKeyword = "latest";

    public string Type { get; }
    public string CurrentVersion { get; }
    public string? TargetVersion { get; }
    public bool IsUpdate => TargetVersion is not null;
    public bool IsLatest =>
        (CurrentVersion == LatestKeyword && !IsUpdate) ||
        (TargetVersion == LatestKeyword);

    public ParsedPluginType(string type, string currentVersion): this(type, currentVersion, null) { }

    public ParsedPluginType(string type, string currentVersion, string? targetVersion)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        CurrentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
        TargetVersion = targetVersion;
    }

    /// <summary>
    /// Returns a readable representation of the plugin type and version, including updates when applicable.
    /// </summary>
    public override string ToString()
    {
        if (IsUpdate)
        {
            return $"{Type}:{CurrentVersion}->{TargetVersion}";
        }

        if (CurrentVersion is null)
        {
            return Type;
        }

        return $"{Type}:{CurrentVersion}";
    }
}
