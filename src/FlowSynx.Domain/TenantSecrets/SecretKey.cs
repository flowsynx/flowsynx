namespace FlowSynx.Domain.TenantSecrets;

public sealed record SecretKey
{
    public string Value { get; }
    public string Prefix { get; }
    public string Name { get; }

    private SecretKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SecretKey cannot be empty", nameof(value));

        if (value.Length > 200)
            throw new ArgumentException("SecretKey cannot exceed 200 characters", nameof(value));

        Value = value;

        // Parse prefix and name
        var parts = value.Split(':');
        Prefix = parts.Length > 1 ? parts[0] : string.Empty;
        Name = parts.Length > 1 ? parts[1] : parts[0];
    }

    public static SecretKey Create(string value) => new(value);
    public bool HasPrefix => !string.IsNullOrEmpty(Prefix);
}