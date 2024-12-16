using FlowSynx.Core.Extensions;
using FlowSynx.Core.Features.List.Query;
using FlowSynx.Extensions;
using FlowSynx.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSynx.Endpoints;

public class List : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this).MapPost(GetList);
    }
    
    public async Task<IResult> GetList([FromBody] ListRequest request, 
        [FromServices] IMediator mediator, IJobQueue jobQueue,
        CancellationToken cancellationToken)
    {
        var jobId = jobQueue.EnqueueTask(async token =>
        {
            return await mediator.List(request, cancellationToken);
        });

        var result = (Abstractions.Result<IEnumerable<object>>)await jobQueue.GetJobResultAsync(jobId);
        return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
    }
}