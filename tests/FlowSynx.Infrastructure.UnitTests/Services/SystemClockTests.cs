using FlowSynx.Infrastructure.Services;

namespace FlowSynx.Infrastructure.UnitTests.Services;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        var systemClock = new SystemClock();

        // Act
        DateTime beforeCall = DateTime.UtcNow;
        DateTime utcNow = systemClock.UtcNow;
        DateTime afterCall = DateTime.UtcNow;

        // Assert
        Assert.InRange(utcNow, beforeCall, afterCall);
    }

    [Fact]
    public void Now_ShouldReturnCurrentLocalTime()
    {
        // Arrange
        var systemClock = new SystemClock();

        // Act
        DateTime beforeCall = DateTime.Now;
        DateTime now = systemClock.Now;
        DateTime afterCall = DateTime.Now;

        // Assert
        Assert.InRange(now, beforeCall, afterCall);
    }
}