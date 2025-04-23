using FlowSynx.Application.Features.Audit.Query.Details;
using FlowSynx.Application.Features.Audit.Query.List;
using FlowSynx.Application.Features.Logs.Query.List;
using FlowSynx.Application.Features.PluginConfig.Command.Add;
using FlowSynx.Application.Features.PluginConfig.Command.Delete;
using FlowSynx.Application.Features.PluginConfig.Command.Update;
using FlowSynx.Application.Features.PluginConfig.Query.Details;
using FlowSynx.Application.Features.PluginConfig.Query.List;
using FlowSynx.Application.Features.Plugins.Command.Uninstall;
using FlowSynx.Application.Features.Plugins.Command.Install;
using FlowSynx.Application.Features.Plugins.Command.Update;
using FlowSynx.Application.Features.Plugins.Query.Details;
using FlowSynx.Application.Features.Plugins.Query.List;
using FlowSynx.Application.Features.Version.Query;
using FlowSynx.Application.Features.Workflows.Command.Add;
using FlowSynx.Application.Features.Workflows.Command.Delete;
using FlowSynx.Application.Features.Workflows.Command.Execute;
using FlowSynx.Application.Features.Workflows.Command.Update;
using FlowSynx.Application.Features.Workflows.Query.Details;
using FlowSynx.Application.Features.Workflows.Query.List;
using FlowSynx.Application.Wrapper;
using MediatR;

namespace FlowSynx.Application.Extensions;

public static class MediatorExtensions
{
    #region Workflow
    public static Task<Result<IEnumerable<WorkflowListResponse>>> Workflows(this IMediator mediator, 
        CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowListRequest(), cancellationToken);
    }

    public static Task<Result<WorkflowDetailsResponse>> WorkflowDetails(this IMediator mediator,
        string id, CancellationToken cancellationToken)
    {
        return mediator.Send(new WorkflowDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<AddWorkflowResponse>> AddWorkflow(this IMediator mediator,
        string request, CancellationToken cancellationToken)
    {
        return mediator.Send(new AddWorkflowRequest { Definition = request }, cancellationToken);
    }

    public static Task<Result<Unit>> UpdateWorkflow(this IMediator mediator, string id, string definition,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new UpdateWorkflowRequest { Id = id, Definition = definition }, cancellationToken);
    }

    public static Task<Result<Unit>> DeleteWorkflow(this IMediator mediator, string id,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new DeleteWorkflowRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<Unit>> ExecuteWorkflow(this IMediator mediator, Guid id,
    CancellationToken cancellationToken)
    {
        return mediator.Send(new ExecuteWorkflowRequest { WorkflowId = id }, cancellationToken);
    }
    #endregion

    #region Version
    public static Task<Result<VersionResponse>> Version(this IMediator mediator, VersionRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Plugins
    public static Task<Result<IEnumerable<PluginsListResponse>>> PluginsList(this IMediator mediator, 
        CancellationToken cancellationToken)
    {
        return mediator.Send(new PluginsListRequest(), cancellationToken);
    }

    public static Task<Result<PluginDetailsResponse>> PluginDetails(this IMediator mediator, 
        string id, CancellationToken cancellationToken)
    {
        return mediator.Send(new PluginDetailsRequest { Id = id }, cancellationToken);
    }

    public static Task<Result<Unit>> InstallPlugin(this IMediator mediator,
        InstallPluginRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> UpdatePlugin(this IMediator mediator,
        UpdatePluginRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> UninstallPlugin(this IMediator mediator,
        UninstallPluginRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region PluginConfig
    public static Task<Result<IEnumerable<PluginConfigListResponse>>> PluginsConfiguration(this IMediator mediator,
        CancellationToken cancellationToken)
    {
        return mediator.Send(new PluginConfigListRequest(), cancellationToken);
    }

    public static Task<Result<PluginConfigDetailsResponse>> PluginConfigurationDetails(this IMediator mediator, 
         string id, CancellationToken cancellationToken)
    {
        return mediator.Send(new PluginConfigDetailsRequest { Id = id}, cancellationToken);
    }

    public static Task<Result<AddPluginConfigResponse>> AddPluginConfiguration(this IMediator mediator, 
        AddPluginConfigRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> UpdatePluginConfiguration(this IMediator mediator, 
        UpdatePluginConfigRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    public static Task<Result<Unit>> DeletePluginConfiguration(this IMediator mediator, string id, 
        CancellationToken cancellationToken)
    {
        return mediator.Send(new DeletePluginConfigRequest { Id = id }, cancellationToken);
    }
    #endregion

    #region Logs
    public static Task<Result<IEnumerable<LogsListResponse>>> Logs(this IMediator mediator, LogsListRequest request, 
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region Audits
    public static Task<Result<IEnumerable<AuditsListResponse>>> Audits(this IMediator mediator, CancellationToken cancellationToken)
    {
        return mediator.Send(new AuditsListRequest(), cancellationToken);
    }

    public static Task<Result<AuditDetailsResponse>> AuditDetails(this IMediator mediator,
     string id, CancellationToken cancellationToken)
    {
        return mediator.Send(new AuditDetailsRequest { Id = id }, cancellationToken);
    }
    #endregion
}