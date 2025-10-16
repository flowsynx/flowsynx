using FlowSynx.Infrastructure.Workflow.BackoffStrategies;

namespace FlowSynx.Infrastructure.UnitTests.Workflow.BackoffStrategies;

public class JitterBackoffStrategyTests
{
    [Fact]
    public void GetDelay_ReturnsBaseDelay_WhenCoefficientIsZero()
    {
        var strategy = new JitterBackoffStrategy(initialDelay: 100, backoffCoefficient: 0);
        var retryCount = 3;
        var expected = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount));

        var delay = strategy.GetDelay(retryCount);

        Assert.Equal(expected, delay);
    }

    [Fact]
    public void GetDelay_RemainsWithinExpectedRange()
    {
        var initialDelay = 50;
        var backoffCoefficient = 0.5;
        var strategy = new JitterBackoffStrategy(initialDelay, backoffCoefficient);
        var retryCount = 4;

        var baseMs = initialDelay * Math.Pow(2, retryCount);
        var maxMs = baseMs * (1 + backoffCoefficient);

        for (var i = 0; i < 10; i++)
        {
            var delay = strategy.GetDelay(retryCount);
            Assert.InRange(delay.TotalMilliseconds, baseMs, maxMs);
        }
    }
}
