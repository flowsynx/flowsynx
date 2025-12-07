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

    public override string ToString()
        => IsUpdate
            ? $"{Type}:{CurrentVersion}->{TargetVersion}"
            : CurrentVersion is null ? Type : $"{Type}:{CurrentVersion}";
}