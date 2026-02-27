using FlowSynx.Application.Core.Services;
using FlowSynx.Domain.Activities;
using FlowSynx.Infrastructure.Runtime.Executors;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public class ActivityExecutorFactory : IActivityExecutorFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public ActivityExecutorFactory(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    public IActivityExecutor CreateExecutor(ExecutableComponent executableComponent)
    {
        return executableComponent.Type switch
        {
            ExecutableComponentType.Script => new ScriptActivityExecutor(_loggerFactory.CreateLogger<ScriptActivityExecutor>(), _httpClientFactory),
            ExecutableComponentType.Assembly => new AssemblyActivityExecutor(_loggerFactory.CreateLogger<AssemblyActivityExecutor>()),
            ExecutableComponentType.Http => new HttpActivityExecutor(_loggerFactory.CreateLogger<HttpActivityExecutor>()),
            ExecutableComponentType.Container => new ContainerActivityExecutor(_loggerFactory.CreateLogger<ContainerActivityExecutor>()),
            ExecutableComponentType.Grpc => new GrpcActivityExecutor(_loggerFactory.CreateLogger<GrpcActivityExecutor>()),
            _ => throw new NotSupportedException($"Executor type '{executableComponent.Type}' is not supported")
        };
    }
}