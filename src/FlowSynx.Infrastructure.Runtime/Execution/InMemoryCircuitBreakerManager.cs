using FlowSynx.Domain.Activities;
using System.Collections.Concurrent;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public class InMemoryCircuitBreakerManager : ICircuitBreakerManager
{
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _states = new();

    public CircuitBreakerState GetOrCreate(string key, CircuitBreaker settings)
    {
        return _states.GetOrAdd(key, _ => new CircuitBreakerState(settings));
    }
}