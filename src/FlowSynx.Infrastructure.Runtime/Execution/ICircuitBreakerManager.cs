using FlowSynx.Domain.Activities;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public interface ICircuitBreakerManager
{
    CircuitBreakerState GetOrCreate(string key, CircuitBreaker settings);
}