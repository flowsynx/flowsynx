using System.Security.Cryptography;

namespace FlowSynx.Infrastructure.Workflow.BackoffStrategies;

public class JitterBackoffStrategy(int initialDelay, double backoffCoefficient = 0.5) : IBackoffStrategy
{
    public TimeSpan GetDelay(int retryCount)
    {
        var baseMs = initialDelay * Math.Pow(2, retryCount); // Exponential growth
        var jitter = GetRandomFraction() * backoffCoefficient * baseMs;
        return TimeSpan.FromMilliseconds(baseMs + jitter);
    }

    private static double GetRandomFraction()
    {
        Span<byte> buffer = stackalloc byte[8];
        RandomNumberGenerator.Fill(buffer);
        var randomValue = BitConverter.ToUInt64(buffer);
        return randomValue / (ulong.MaxValue + 1.0);
    }
}
