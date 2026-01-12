using FlowSynx.Application.Core.Services;
using FlowSynx.Domain.GeneBlueprints;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public class GeneExecutorFactory : IGeneExecutorFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public GeneExecutorFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IGeneExecutor CreateExecutor(ExecutableComponent executableComponent)
    {
        return executableComponent.Type.ToLowerInvariant() switch
        {
            "script" => new ScriptGeneExecutor(_loggerFactory.CreateLogger<ScriptGeneExecutor>()),
            "assembly" => new AssemblyGeneExecutor(_loggerFactory.CreateLogger<AssemblyGeneExecutor>()),
            "http" => new HttpGeneExecutor(_loggerFactory.CreateLogger<HttpGeneExecutor>()),
            "container" => new ContainerGeneExecutor(_loggerFactory.CreateLogger<ContainerGeneExecutor>()),
            "grpc" => new GrpcGeneExecutor(_loggerFactory.CreateLogger<GrpcGeneExecutor>()),
            _ => throw new NotSupportedException($"Executor type '{executableComponent.Type}' is not supported")
        };
    }
}