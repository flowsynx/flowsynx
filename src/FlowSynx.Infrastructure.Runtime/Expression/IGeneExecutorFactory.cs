using FlowSynx.Application.Core.Services;
using FlowSynx.Domain.Genes;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public interface IGeneExecutorFactory
{
    IGeneExecutor CreateExecutor(ExecutableComponent executableComponent);
}