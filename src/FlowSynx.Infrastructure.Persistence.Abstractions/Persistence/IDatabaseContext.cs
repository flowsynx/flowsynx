using FlowSynx.Domain.Activities;
using FlowSynx.Domain.ActivityInstances;
using FlowSynx.Domain.AuditTrails;
using FlowSynx.Domain.TenantContacts;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.TenantSecretConfigs;
using FlowSynx.Domain.TenantSecrets;
using FlowSynx.Domain.WorkflowApplications;
using FlowSynx.Domain.WorkflowExecutions;
using FlowSynx.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace FlowSynx.Infrastructure.Persistence.Abstractions;

public interface IDatabaseContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<TenantSecret> TenantSecrets { get; }
    DbSet<TenantSecretConfig> TenantSecretConfigs { get; }
    DbSet<TenantContact> TenantContacts { get; }
    DbSet<Activity> Activities { get; }
    DbSet<Workflow> Workflows { get; }
    DbSet<WorkflowApplication> WorkflowApplications { get; }
    DbSet<ActivityRun> ActivityRuns { get; }
    DbSet<AuditTrail> AuditTrails { get; }
    DbSet<WorkflowExecution> WorkflowExecutions { get; }
    DbSet<WorkflowExecutionLog> WorkflowExecutionLogs { get; }
    DbSet<WorkflowExecutionArtifact> WorkflowExecutionArtifacts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}