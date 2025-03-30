using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using FluentValidation;
using MediatR;

namespace FlowSynx.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IValidator<TRequest> _validator;

    public ValidationBehavior(IValidator<TRequest> validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        _validator = validator;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join(", ", validationResult.Errors);
            throw new FlowSynxException((int)ErrorCode.InputValidation, errorMessages);
        }

        return await next();
    }
}