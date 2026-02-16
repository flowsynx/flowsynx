using FlowSynx.Application.Core.Services;
using FlowSynx.Domain.Activities;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public interface IActivityExecutorFactory
{
    IActivityExecutor CreateExecutor(ExecutableComponent executableComponent);
}