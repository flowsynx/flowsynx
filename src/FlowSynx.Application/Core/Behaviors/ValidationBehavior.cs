//using FlowSynx.Domain.Primitives;
//using FlowSynx.PluginCore.Exceptions;
//using FluentValidation;
//using MediatR;

//namespace FlowSynx.Application.Core.Behaviors;

//public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
//    where TRequest : IRequest<TResponse>
//{
//    private readonly IEnumerable<IValidator<TRequest>> _validators;

//    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
//    {
//        ArgumentNullException.ThrowIfNull(validators);
//        _validators = validators;
//    }

//    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
//    {
//        if (!_validators.Any()) 
//            return await next(cancellationToken);

//        var context = new ValidationContext<TRequest>(request);

//        var validationResults = await Task.WhenAll(
//            _validators.Select(v =>
//                v.ValidateAsync(context, cancellationToken)));

//        var failures = validationResults
//            .Where(result => result.Errors.Any())
//            .SelectMany(result => result.Errors)
//            .ToList();

//        if (!failures.Any()) 
//            return await next(cancellationToken);

//        var errorMessages = string.Join(", ", failures.Select(x=>x.ErrorMessage));
//        throw new FlowSynxException((int)ErrorCode.InputValidation, errorMessages);
//    }
//}