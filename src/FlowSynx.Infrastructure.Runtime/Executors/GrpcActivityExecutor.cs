using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Workflows;
using FlowSynx.Infrastructure.Runtime.Execution;
using Microsoft.Extensions.Logging;

namespace FlowSynx.Infrastructure.Runtime.Executors;

public class GrpcActivityExecutor : BaseActivityExecutor
{
    public GrpcActivityExecutor(ILogger<GrpcActivityExecutor> logger) : base(logger) { }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "grpc";
    }

    public override async Task<object> ExecuteAsync(
        ActivityJson activity,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var grpc = activity.Spec.Executable.Grpc;
        if (grpc == null)
        {
            throw new Exception("gRPC configuration is missing");
        }

        _logger.LogInformation("Making gRPC call to {Service}.{Method}", grpc.Service, grpc.Method);

        // In a real implementation, use Grpc.Net.Client
        // For now, we'll mock it
        await Task.Delay(300);

        var result = new
        {
            grpc = new
            {
                service = grpc.Service,
                method = grpc.Method,
                address = grpc.Address
            },
            parameters = parameters,
            executedAt = DateTime.UtcNow,
            success = true
        };

        return result;
    }
}