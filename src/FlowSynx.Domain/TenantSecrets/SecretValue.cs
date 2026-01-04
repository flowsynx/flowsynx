namespace FlowSynx.Domain.TenantSecrets;

public record SecretValue
{
    public string Value { get; init; }
    public bool IsEncrypted { get; init; }
    public DateTime? ExpiresAt { get; init; }

    private SecretValue(string value, bool isEncrypted = false, DateTime? expiresAt = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SecretValue cannot be empty", nameof(value));

        Value = value;
        IsEncrypted = isEncrypted;
        ExpiresAt = expiresAt;
    }

    public static SecretValue Create(string value, bool encrypt = false, DateTime? expiresAt = null)
        => new(value, encrypt, expiresAt);

    public SecretValue Encrypt() => this with { IsEncrypted = true };
    public SecretValue Decrypt() => this with { IsEncrypted = false };
}