using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.TenantContacts;
using FlowSynx.Domain.Tenants.Events;
using System.Text.RegularExpressions;

namespace FlowSynx.Domain.Tenants;

public class Tenant: AuditableEntity<TenantId>, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TenantStatus Status { get; private set; }

    public TenantConfiguration Configuration { get; private set; }
    public List<TenantContact> Contacts { get; private set; } = new();

    // Private constructor for EF Core
    private Tenant() { }

    public static Tenant Create(
        string name,
        string? description = null)
    {
        ValidateName(name);

        var tenantId = TenantId.CreateUnique();
        var slug = GenerateSlug(name);

        var tenant = new Tenant()
        {
            Id = tenantId,
            Name = name.Trim(),
            Slug = slug,
            Description = description?.Trim(),
            Status = TenantStatus.Active,
            Configuration = TenantConfigurationDefaults.Create()
        };

        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, tenant.Name, tenant.Slug));

        return tenant;
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Tenant name cannot be empty");

        var oldName = Name;
        Name = newName.Trim();
        Slug = GenerateSlug(newName);

        AddDomainEvent(new TenantRenamedEvent(Id, oldName, Name));
    }

    public void UpdateDescription(string? description)
    {
        Description = description?.Trim();

        AddDomainEvent(new TenantDescriptionUpdatedEvent(Id));
    }

    public void Activate()
    {
        if (Status == TenantStatus.Active)
            return;

        Status = TenantStatus.Active;

        AddDomainEvent(new TenantActivatedEvent(Id));
    }

    public void Suspend(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Suspension reason is required");

        Status = TenantStatus.Suspended;

        AddDomainEvent(new TenantSuspendedEvent(Id, reason));
    }

    public void Terminate(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Termination reason is required");

        Status = TenantStatus.Terminated;

        AddDomainEvent(new TenantTerminatedEvent(Id, reason));
    }

    public void AddContact(string email, string name, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Contact email cannot be empty");

        if (Contacts.Any(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Contact with email {email} already exists");

        var contact = new TenantContact(Id, email, name, isPrimary);
        Contacts.Add(contact);

        if (isPrimary)
        {
            // Ensure only one primary contact
            foreach (var c in Contacts.Where(c => c != contact && c.IsPrimary))
            {
                c.SetPrimary(false);
            }
        }

        AddDomainEvent(new TenantContactAddedEvent(Id, email, name, isPrimary));
    }

    // Private helper
    private static string GenerateSlug(string name)
    {
        var slug = name
            .ToLowerInvariant()
            .Replace(" & ", "-and-")
            .Replace(" and ", "-and-")
            .Replace("+", "-plus-")
            .Replace("@", "-at-")
            .Replace(' ', '-')
            .Replace('_', '-')
            .Replace('.', '-');

        // Remove all non-alphanumeric characters except hyphens
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');

        if (slug.Length > 64)
        {
            slug = slug[..64].Trim('-');
        }

        if (slug.Length < 3)
        {
            slug = $"{slug}-{Random.Shared.Next(1000):000}";
        }

        return slug;
    }

    public void ChangeName(string newName)
    {
        ValidateName(newName);

        var oldName = Name;
        Name = newName.Trim();

        AddDomainEvent(new TenantRenamedEvent(Id, oldName, Name));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tenant name cannot be empty");

        if (name.Length < 2)
            throw new DomainException("Tenant name must be at least 2 characters long");

        if (name.Length > 100)
            throw new DomainException("Tenant name cannot exceed 100 characters");
    }

    public TenantConfiguration FallBackToDefaultConfiguration()
    {
        Configuration = TenantConfigurationDefaults.Create();

        AddDomainEvent(new TenantConfigurationCreatedEvent(Id));

        return Configuration;
    }

    public void UpdateSettings(TenantConfiguration newSettings, string changeReason)
    {
        Configuration = newSettings;
        AddDomainEvent(new TenantConfigurationUpdatedEvent(Id, changeReason));
    }
}