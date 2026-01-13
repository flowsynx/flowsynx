using FlowSynx.Application.Core.Dispatcher;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace FlowSynx.Infrastructure.Messaging;

public class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, Type> _handlerTypes = new();
    private static readonly ConcurrentDictionary<Type, Delegate> _handlerDelegates = new();

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TAction> Dispatch<TAction>(
        IAction<TAction> action, 
        CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var requestType = action.GetType();

        // Validate request if validator exists
        await ValidateRequestAsync(action, requestType, cancellationToken);

        // Get or create handler delegate
        var handlerDelegate = GetOrCreateHandlerDelegate<TAction>(requestType);

        // Execute handler
        return await handlerDelegate(action, _serviceProvider, cancellationToken);
    }

    private async Task ValidateRequestAsync<TAction>(
        IAction<TAction> action,
        Type actionType,
        CancellationToken cancellationToken)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(actionType);
        var validator = _serviceProvider.GetService(validatorType) as IValidator;

        if (validator != null)
        {
            var validationContext = typeof(ValidationContext<>)
                .MakeGenericType(actionType)
                .GetConstructor(new[] { actionType })
                .Invoke(new object[] { action });

            var validateMethod = validatorType.GetMethod("ValidateAsync", new[]
            {
                    typeof(IValidationContext),
                    typeof(CancellationToken)
                });

            var validationResult = await (Task<ValidationResult>)validateMethod.Invoke(
                validator,
                new object[] { validationContext, cancellationToken });

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }
    }

    private Func<IAction<TAction>, IServiceProvider, CancellationToken, Task<TAction>>
        GetOrCreateHandlerDelegate<TAction>(Type requestType)
    {
        return (Func<IAction<TAction>, IServiceProvider, CancellationToken, Task<TAction>>)
            _handlerDelegates.GetOrAdd(requestType, type =>
            {
                // Create compiled expression for maximum performance
                var handlerType = typeof(IActionHandler<,>).MakeGenericType(type, typeof(TAction));
                var handleMethod = handlerType.GetMethod("Handle");

                // Build expression: (request, sp, ct) => sp.GetService<THandler>().Handle(request, ct)
                var requestParam = Expression.Parameter(typeof(IAction<TAction>), "request");
                var serviceProviderParam = Expression.Parameter(typeof(IServiceProvider), "sp");
                var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "ct");

                // Call GetRequiredService<THandler>()
                var getServiceCall = Expression.Call(
                    typeof(ServiceProviderServiceExtensions),
                    "GetRequiredService",
                    new[] { handlerType },
                    serviceProviderParam);

                // Convert request to specific type
                var convertedRequest = Expression.Convert(requestParam, type);

                // Call Handle method
                var handleCall = Expression.Call(
                    getServiceCall,
                    handleMethod,
                    convertedRequest,
                    cancellationTokenParam);

                // Create lambda
                var lambda = Expression.Lambda(
                    handleCall,
                    requestParam,
                    serviceProviderParam,
                    cancellationTokenParam);

                return lambda.Compile();
            });
    }
}