using FlowSynx.Application.Core.Dispatcher;
using FlowSynx.Application.Core.Extensions;
using FlowSynx.Application.Core.Services;
using FlowSynx.BuildingBlocks.Results;
using FlowSynx.Domain.Genomes;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Application.Features.Genes.Requests.GeneExecutionsList;

internal class GeneExecutionsListHandler : IActionHandler<GeneExecutionsListRequest, PaginatedResult<GeneExecutionsListResult>>
{
    private readonly ILogger<GeneExecutionsListHandler> _logger;
    private readonly IGenomeExecutionService _genomeExecutionService;
    private readonly ICurrentUserService _currentUserService;

    public GeneExecutionsListHandler(
        ILogger<GeneExecutionsListHandler> logger, 
        IGenomeExecutionService genomeExecutionService,
        ICurrentUserService currentUserService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _genomeExecutionService = genomeExecutionService ?? throw new ArgumentNullException(nameof(genomeExecutionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<PaginatedResult<GeneExecutionsListResult>> Handle(GeneExecutionsListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _currentUserService.ValidateAuthentication();

            var histories = await _genomeExecutionService.GetExecutionHistoryAsync(
                "gene",
                request.GeneId,
                cancellationToken);

            var response = histories.Select(history => new GeneExecutionsListResult
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
                Logs = history.Logs.Select(l=>new GeneExecutionsLog
                {
                    Timestamp = l.Timestamp,
                    Level = l.Level,
                    Source = l.Source,
                    Message = l.Message,
                    Data = l.Data
                }).ToList(),
                Artifacts = history.Artifacts.Select(a=>new GeneExecutionsArtifact
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
                "Gene execution history list retrieved successfully for page {Page} with size {PageSize}.",
                page,
                pageSize);

            return await PaginatedResult<GeneExecutionsListResult>.SuccessAsync(
                pagedItems,
                totalCount,
                page,
                pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "FlowSynx exception caught in GenesListHandler for page {Page} with size {PageSize}.",
                request.Page,
                request.PageSize);
            return await PaginatedResult<GeneExecutionsListResult>.FailureAsync(ex.Message);
        }
    }
}
