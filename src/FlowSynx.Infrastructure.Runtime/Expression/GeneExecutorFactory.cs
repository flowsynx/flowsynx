using FlowSynx.Application.Core.Services;
using FlowSynx.Domain.Genes;
using FlowSynx.Infrastructure.Runtime.Executors;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public class GeneExecutorFactory : IGeneExecutorFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public GeneExecutorFactory(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    public IGeneExecutor CreateExecutor(ExecutableComponent executableComponent)
    {
        return executableComponent.Type.ToLowerInvariant() switch
        {
            "script" => new ScriptGeneExecutor(_loggerFactory.CreateLogger<ScriptGeneExecutor>(), _httpClientFactory),
            "assembly" => new AssemblyGeneExecutor(_loggerFactory.CreateLogger<AssemblyGeneExecutor>()),
            "http" => new HttpGeneExecutor(_loggerFactory.CreateLogger<HttpGeneExecutor>()),
            "container" => new ContainerGeneExecutor(_loggerFactory.CreateLogger<ContainerGeneExecutor>()),
            "grpc" => new GrpcGeneExecutor(_loggerFactory.CreateLogger<GrpcGeneExecutor>()),
            _ => throw new NotSupportedException($"Executor type '{executableComponent.Type}' is not supported")
        };
    }
}