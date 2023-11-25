using FlowSync.Abstractions;
using FlowSync.Core.Extensions;
using FlowSync.Core.Features.List;
using FlowSync.Core.Utilities;
using FluentValidation;
using MediatR;

namespace FlowSync.Endpoints.List;

public static class Endpoint
{
    public static IEndpointRouteBuilder MapList(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/list", async (ListRequest request, 
            IMediator mediator, 
            IValidator<ListRequest> validator,
        CancellationToken cancellationToken) =>
        {
            try
            {
                var validationResult = await validator.ValidateAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                    return Results.ValidationProblem(validationResult.ToDictionary());
                
                var result = await mediator.List(request, cancellationToken);
                var outputType = EnumUtils.GetEnumValueOrDefault<OutputType>(request.Output) ?? OutputType.Json;
                return result.Succeeded ? Results.Ok(result) : Results.NotFound(result);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).WithName("GetList");
        return endpoints;
    }
}