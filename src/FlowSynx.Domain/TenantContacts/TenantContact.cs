using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;

namespace FlowSynx.Domain.TenantContacts;

public class TenantContact: AuditableEntity<Guid>, ITenantScoped
{
    public TenantId TenantId { get; set; }
    public string Email { get; private set; }
    public string Name { get; private set; }
    public bool IsPrimary { get; private set; }
    public Tenant? Tenant { get; set; }

    public TenantContact(TenantId tenantId, string email, string name, bool isPrimary = false)
    {
        TenantId = tenantId;
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");

        if (!IsValidEmail(email))
            throw new DomainException($"Invalid email format: {email}");

        Email = email.ToLowerInvariant();
        Name = name?.Trim() ?? string.Empty;
        IsPrimary = isPrimary;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    internal void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }
}