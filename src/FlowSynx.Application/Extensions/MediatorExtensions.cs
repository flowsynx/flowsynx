using FlowSynx.Application.Features.Logs.Query.List;
using FlowSynx.Application.Features.PluginConfig.Command.Add;
using FlowSynx.Application.Features.PluginConfig.Command.Delete;
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
        WorkflowListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<WorkflowDetailsResponse>> WorkflowDetails(this IMediator mediator,
        WorkflowDetailsRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<AddWorkflowResponse>> AddWorkflow(this IMediator mediator,
        AddWorkflowRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> UpdateWorkflow(this IMediator mediator, UpdateWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> DeleteWorkflow(this IMediator mediator, DeleteWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<object?>> ExecuteWorkflow(this IMediator mediator, Guid id,
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
        PluginsListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<PluginDetailsResponse>> PluginDetails(this IMediator mediator, 
        PluginDetailsRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }
    #endregion

    #region PluginConfig
    public static Task<Result<IEnumerable<PluginConfigListResponse>>> PluginsConfiguration(this IMediator mediator,
        PluginConfigListRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<PluginConfigDetailsResponse>> PluginConfigurationDetails(this IMediator mediator, 
        PluginConfigDetailsRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<AddPluginConfigResponse>> AddConfig(this IMediator mediator, 
        AddPluginConfigRequest request, CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
    }

    public static Task<Result<Unit>> DeleteConfig(this IMediator mediator, DeletePluginConfigRequest request, 
        CancellationToken cancellationToken)
    {
        return mediator.Send(request, cancellationToken);
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