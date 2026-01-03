//using FlowSynx.PluginCore.Exceptions;
//using Microsoft.Extensions.Logging;
//using FlowSynx.Domain.Primitives;
//using FlowSynx.Application.Core.Dispatcher;

//namespace FlowSynx.Application.Features.Metrics.Requests;

//internal class SummaryHandler : IActionHandler<SummaryRequest, Result<SummaryResult>>
//{
//    private readonly ILogger<SummaryHandler> _logger;
//    private readonly IWorkflowService _workflowService;
//    private readonly IWorkflowExecutionService _workflowExecutionService;
//    private readonly ICurrentUserService _currentUserService;

//    public SummaryHandler(
//        ILogger<SummaryHandler> logger,
//        IWorkflowService workflowService,
//        IWorkflowExecutionService workflowExecutionService,
//        ICurrentUserService currentUserService)
//    {
//        ArgumentNullException.ThrowIfNull(logger);
//        ArgumentNullException.ThrowIfNull(workflowService);
//        ArgumentNullException.ThrowIfNull(workflowExecutionService);
//        ArgumentNullException.ThrowIfNull(currentUserService);
//        _logger = logger;
//        _workflowService = workflowService;
//        _workflowExecutionService = workflowExecutionService;
//        _currentUserService = currentUserService;
//    }

//    public async Task<Result<SummaryResult>> Handle(SummaryRequest request, CancellationToken cancellationToken)
//    {
//        try
//        {
//            _currentUserService.ValidateAuthentication();
//            var userId = _currentUserService.UserId();

//            var response = new SummaryResult()
//            {
//                ActiveWorkflows = await _workflowService.GetActiveWorkflowsCountAsync(userId, cancellationToken),
//                RunningTasks = await _workflowExecutionService.GetRunningWorkflowCountAsync(userId, cancellationToken),
//                CompletedToday = await _workflowExecutionService.GetCompletedWorkflowsCountAsync(userId, cancellationToken),
//                FailedWorkflows = await _workflowExecutionService.GetFailedWorkflowsCountAsync(userId, cancellationToken)
//            };

//            return await Result<SummaryResult>.SuccessAsync(response);
//        }
//        catch (FlowSynxException ex)
//        {
//            _logger.LogError(ex, "FlowSynx exception caught in SummaryHandler.");
//            return await Result<SummaryResult>.FailAsync(ex.Message);
//        }
//    }
//}
