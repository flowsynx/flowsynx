using FlowSynx.Domain.Activities;

namespace FlowSynx.Infrastructure.Runtime.Execution;

public class CircuitBreakerState
{
    private readonly object _lock = new object();
    private readonly CircuitBreaker _settings;
    private int _failureCount;
    private int _successCount;
    private DateTime _lastFailureTime;
    private CircuitBreakerStatus _status = CircuitBreakerStatus.Closed;

    public bool IsOpen => _status == CircuitBreakerStatus.Open;

    public CircuitBreakerState(CircuitBreaker settings)
    {
        _settings = settings;
    }

    public void RecordFailure()
    {
        lock (_lock)
        {
            _lastFailureTime = DateTime.UtcNow;
            if (_status == CircuitBreakerStatus.Closed)
            {
                _failureCount++;
                if (_failureCount >= _settings.FailureThreshold)
                {
                    _status = CircuitBreakerStatus.Open;
                    _ = Task.Delay(_settings.TimeoutMilliseconds).ContinueWith(_ => HalfOpen());
                }
            }
            else if (_status == CircuitBreakerStatus.HalfOpen)
            {
                // Failure in half‑open immediately reopens
                _status = CircuitBreakerStatus.Open;
                _failureCount = 1; // reset? depends on design
                _ = Task.Delay(_settings.TimeoutMilliseconds).ContinueWith(_ => HalfOpen());
            }
        }
    }

    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_status == CircuitBreakerStatus.HalfOpen)
            {
                _successCount++;
                if (_successCount >= _settings.SuccessThreshold)
                {
                    // Close the circuit
                    _status = CircuitBreakerStatus.Closed;
                    _failureCount = 0;
                    _successCount = 0;
                }
            }
            else if (_status == CircuitBreakerStatus.Closed)
            {
                // Reset failure count on success in closed state
                _failureCount = 0;
            }
        }
    }

    private void HalfOpen()
    {
        lock (_lock)
        {
            if (_status == CircuitBreakerStatus.Open)
            {
                _status = CircuitBreakerStatus.HalfOpen;
                _successCount = 0;
            }
        }
    }

    private enum CircuitBreakerStatus { Closed, Open, HalfOpen }
}