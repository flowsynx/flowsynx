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
        return executableComponent.Type.ToLowerInvariant() switch
        {
            "script" => new ScriptActivityExecutor(_loggerFactory.CreateLogger<ScriptActivityExecutor>(), _httpClientFactory),
            "assembly" => new AssemblyActivityExecutor(_loggerFactory.CreateLogger<AssemblyActivityExecutor>()),
            "http" => new HttpActivityExecutor(_loggerFactory.CreateLogger<HttpActivityExecutor>()),
            "container" => new ContainerActivityExecutor(_loggerFactory.CreateLogger<ContainerActivityExecutor>()),
            "grpc" => new GrpcActivityExecutor(_loggerFactory.CreateLogger<GrpcActivityExecutor>()),
            _ => throw new NotSupportedException($"Executor type '{executableComponent.Type}' is not supported")
        };
    }
}