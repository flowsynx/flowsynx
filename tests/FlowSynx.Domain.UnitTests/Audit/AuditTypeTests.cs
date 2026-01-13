using FlowSynx.Domain.Enums;

namespace FlowSynx.Domain.UnitTests.Audit;

public class AuditTypeTests
{
    [Fact]
    public void AuditType_ShouldHaveCorrectValues()
    {
        // Assert
        Assert.Equal((byte)0, (byte)AuditType.None);
        Assert.Equal((byte)1, (byte)AuditType.Create);
        Assert.Equal((byte)2, (byte)AuditType.Update);
        Assert.Equal((byte)3, (byte)AuditType.Delete);
    }

    [Fact]
    public void AuditType_ShouldContainAllExpectedValues()
    {
        // Arrange
        var expectedValues = new[] { AuditType.None, AuditType.Create, AuditType.Update, AuditType.Delete };

        // Act
        var actualValues = Enum.GetValues<AuditType>();

        // Assert
        Assert.Equal(expectedValues.Length, actualValues.Length);
        foreach (var expected in expectedValues)
        {
            Assert.Contains(expected, actualValues);
        }
    }
}