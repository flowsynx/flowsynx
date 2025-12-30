using FlowSynx.Domain.Exceptions;

namespace FlowSynx.Domain.Tenants;

public record TenantId
{
    public Guid Value { get; }

    private TenantId(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("Tenant ID cannot be empty");

        Value = value;
    }

    public static TenantId CreateUnique()
    {
        return new TenantId(Guid.NewGuid());
    }

    public static TenantId Create(Guid value)
    {
        return new TenantId(value);
    }

    public static TenantId FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Tenant ID string cannot be null or empty");

        return new TenantId(Guid.Parse(value));
    }

    public override string ToString() => Value.ToString();
}