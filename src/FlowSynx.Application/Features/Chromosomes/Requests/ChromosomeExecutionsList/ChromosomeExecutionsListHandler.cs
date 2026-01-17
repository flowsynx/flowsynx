using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Chromosomes.Requests.ChromosomeExecutionsList;

internal class ChromosomeExecutionsListHandler 
    : IActionHandler<ChromosomeExecutionsListRequest, PaginatedResult<ChromosomeExecutionsListResult>>
{
    private readonly ILogger<ChromosomeExecutionsListHandler> _logger;
    private readonly IGenomeExecutionService _genomeExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public ChromosomeExecutionsListHandler(
        ILogger<ChromosomeExecutionsListHandler> logger, 
        IGenomeExecutionService genomeExecutionService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _genomeExecutionService = genomeExecutionService ?? throw new ArgumentNullException(nameof(genomeExecutionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<ChromosomeExecutionsListResult>> Handle(ChromosomeExecutionsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var histories = await _genomeExecutionService.GetExecutionHistoryAsync(
                "chromosome",
                request.ChromosomeId,
                cancellationToken);

            var response = histories.Select(history => new ChromosomeExecutionsListResult
            {
                ExecutionId = history.ExecutionId,
                TargetType = history.TargetType,
                TargetId = history.TargetId,
                TargetName = history.TargetName,
                Namespace = history.Namespace,
                Request = history.Request,
                Response = history.Response,
                Context = history.Context,
                Parameters = history.Parameters,
                Metadata = history.Metadata,
                Status = history.Status,
                Progress = history.Progress,
                ErrorMessage = history.ErrorMessage,
                ErrorCode = history.ErrorCode,
                StartedAt = history.StartedAt,
                CompletedAt = history.CompletedAt,
                Duration = history.Duration,
                TriggeredBy = history.TriggeredBy,
                Logs = history.Logs.Select(l=>new ChromosomeExecutionsLog
                {
                    Timestamp = l.Timestamp,
                    Level = l.Level,
                    Source = l.Source,
                    Message = l.Message,
                    Data = l.Data
                }).ToList(),
                Artifacts = history.Artifacts.Select(a=>new ChromosomeExecutionsArtifact
                {
                    Name = a.Name,
                    Type = a.Type,
                    Content = a.Content,
                    Size = a.Size,
                    CreatedAt = a.CreatedAt
                }).ToList()
            }).ToList();

            var pagedItems = response.ToPaginatedList(
                request.Page,
                request.PageSize,
                out var totalCount,
                out var page,
                out var pageSize);

            _logger.LogInformation(
                "Chromosome execution history list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);

            return await PaginatedResult<ChromosomeExecutionsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in ChromosomeExecutionsListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<ChromosomeExecutionsListResult>.FailureAsync(ex.Message);
        }
    }
}
