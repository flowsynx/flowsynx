using FlowSynx.Application.Features.Logs.Query.List;
using FlowSynx.Application.Features.PluginConfig.Command.Add;
using FlowSynx.Application.Features.PluginConfig.Command.Delete;
using FlowSynx.Application.Features.PluginConfig.Command.Update;
using FlowSynx.Application.Features.PluginConfig.Query.Details;
using FlowSynx.Application.Features.PluginConfig.Query.List;
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
    //#region Connectors
    //public static Task<Result<object>> About(this IMediator mediator, AboutRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}

    //public static Task<Result<InterchangeData>> List(this IMediator mediator, ListRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}

    //public static Task<Result<Unit>> Write(this IMediator mediator, WriteRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}

    //public static Task<Result<InterchangeData>> Read(this IMediator mediator, ReadRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}

    //public static Task<Result<Unit>> Delete(this IMediator mediator, DeleteRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}

    //public static Task<Result<object>> Exist(this IMediator mediator, ExistRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}

    //public static Task<Result<Unit>> Create(this IMediator mediator, CreateRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}

    //public static Task<Result<Unit>> Transfer(this IMediator mediator, TransferRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}

    //public static Task<Result<CompressResult>> Compress(this IMediator mediator, CompressRequest request, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(request, cancellationToken);
    //}
    //#endregion

    //#region Workflow
    //public static Task<Result<object?>> Workflow(this IMediator mediator, WorkflowRequest workflow, CancellationToken cancellationToken)
    //{
    //    return mediator.Send(workflow, cancellationToken);
    //}
    //#endregion

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
}