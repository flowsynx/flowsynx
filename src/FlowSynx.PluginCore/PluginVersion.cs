namespace FlowSynx.PluginCore;

public class PluginVersion
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    // Constructor to initialize version numbers
    public PluginVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    // Parse a string like "1.0.0" to a PluginVersion object
    public static PluginVersion Parse(string version)
    {
        var parts = version.Split('.');

        if (parts.Length != 3)
            throw new FormatException("Invalid version format. Expected format: Major.Minor.Patch");

        return new PluginVersion(
            int.Parse(parts[0]),
            int.Parse(parts[1]),
            int.Parse(parts[2])
        );
    }

    // Comparison method for PluginVersion objects
    public int CompareTo(PluginVersion other)
    {
        if (other == null) return 1;

        // Compare major version
        if (Major != other.Major)
            return Major.CompareTo(other.Major);

        // If major is the same, compare minor version
        if (Minor != other.Minor)
            return Minor.CompareTo(other.Minor);

        // If minor is the same, compare patch version
        return Patch.CompareTo(other.Patch);
    }

    // Overriding Equals and GetHashCode for correct comparison
    public override bool Equals(object obj)
    {
        return obj is PluginVersion version && CompareTo(version) == 0;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch);
    }

    // Overload comparison operators
    public static bool operator <(PluginVersion v1, PluginVersion v2)
    {
        return v1.CompareTo(v2) < 0;
    }

    public static bool operator >(PluginVersion v1, PluginVersion v2)
    {
        return v1.CompareTo(v2) > 0;
    }

    public static bool operator <=(PluginVersion v1, PluginVersion v2)
    {
        return v1.CompareTo(v2) <= 0;
    }

    public static bool operator >=(PluginVersion v1, PluginVersion v2)
    {
        return v1.CompareTo(v2) >= 0;
    }

    public static bool operator ==(PluginVersion v1, PluginVersion v2)
    {
        return v1.CompareTo(v2) == 0;
    }

    public static bool operator !=(PluginVersion v1, PluginVersion v2)
    {
        return v1.CompareTo(v2) != 0;
    }

    // ToString override to represent the version as a string
    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}
