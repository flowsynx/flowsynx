using FlowSynx.Domain.AuditTrails;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.GeneBlueprints;
using FlowSynx.Domain.GeneInstances;
using FlowSynx.Domain.Genomes;
using FlowSynx.Domain.TenantContacts;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Domain.TenantSecrets;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Abstractions;

public interface IDatabaseContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantSecret> TenantSecrets { get; }
    DbSet<TenantSecretConfig> TenantSecretConfigs { get; }
    DbSet<TenantContact> TenantContacts { get; }
    DbSet<GeneBlueprint> GeneBlueprints { get; }
    DbSet<Chromosome> Chromosomes { get; }
    DbSet<Genome> Genomes { get; }
    DbSet<GeneInstance> GeneInstances { get; }
    DbSet<AuditTrail> AuditTrails { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}