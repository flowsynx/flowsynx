using Asp.Versioning;
using Asp.Versioning.Builder;
using FlowSync.Core.Extensions;
using FlowSync.Core.Features.List;
using FlowSync.Validator;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FlowSync.Endpoints.List;

public static class Endpoint
{
    public static IVersionedEndpointRouteBuilder MapList(this IVersionedEndpointRouteBuilder app)
    {
        app.MapPost("v{version:apiVersion}/list", async ([FromBody] ListRequest request, 
            [FromServices] IMediator mediator, 
            IValidator<ListRequest> validator, 
            CancellationToken cancellationToken) =>
        {
            try
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);

                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }

                var result = await mediator.List(request, cancellationToken);
                return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).WithName("GetList");
        return app;
    }
}