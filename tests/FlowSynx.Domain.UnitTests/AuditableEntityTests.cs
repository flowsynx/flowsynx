using FlowSynx.Domain.Primitives;

namespace FlowSynx.Domain.UnitTests;

public class AuditableEntityTests
{
    [Fact]
    public void AuditableEntity_ShouldSetAuditProperties()
    {
        // Arrange
        var entity = new TestAuditableEntity();
        var createdBy = "user1";
        var createdOn = DateTime.UtcNow;
        var modifiedBy = "user2";
        var modifiedOn = DateTime.UtcNow.AddHours(1);

        // Act
        entity.CreatedBy = createdBy;
        entity.CreatedOn = createdOn;
        entity.LastModifiedBy = modifiedBy;
        entity.LastModifiedOn = modifiedOn;

        // Assert
        Assert.Equal(createdBy, entity.CreatedBy);
        Assert.Equal(createdOn, entity.CreatedOn);
        Assert.Equal(modifiedBy, entity.LastModifiedBy);
        Assert.Equal(modifiedOn, entity.LastModifiedOn);
    }

    [Fact]
    public void AuditableEntityWithId_ShouldSetIdAndAuditProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entity = new TestAuditableEntityWithId { Id = id };

        // Act & Assert
        Assert.Equal(id, entity.Id);
        Assert.IsAssignableFrom<IAuditableEntity<Guid>>(entity);
    }

    [Fact]
    public void AuditableEntity_ShouldImplementIAuditableEntity()
    {
        // Arrange & Act
        var entity = new TestAuditableEntity();

        // Assert
        Assert.IsAssignableFrom<IAuditableEntity>(entity);
    }

    private class TestAuditableEntity : AuditableEntity
    {
    }

    private class TestAuditableEntityWithId : AuditableEntity<Guid>
    {
    }
}