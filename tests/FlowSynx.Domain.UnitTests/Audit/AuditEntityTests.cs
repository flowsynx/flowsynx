using FlowSynx.Domain.Audit;

namespace FlowSynx.Domain.UnitTests.Audit;

public class AuditEntityTests
{
    [Fact]
    public void AuditEntity_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var auditEntity = new AuditEntity();

        // Assert
        Assert.Equal(Guid.Empty, auditEntity.Id);
        Assert.Null(auditEntity.UserId);
        Assert.Null(auditEntity.Type);
        Assert.Null(auditEntity.TableName);
        Assert.Equal(default(DateTime), auditEntity.DateTime);
        Assert.Null(auditEntity.OldValues);
        Assert.Null(auditEntity.NewValues);
        Assert.Null(auditEntity.AffectedColumns);
        Assert.Null(auditEntity.PrimaryKey);
    }

    [Fact]
    public void AuditEntity_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var userId = "user123";
        var type = "Create";
        var tableName = "Users";
        var dateTime = DateTime.UtcNow;
        var oldValues = "{\"Name\":\"OldName\"}";
        var newValues = "{\"Name\":\"NewName\"}";
        var affectedColumns = "Name,Email";
        var primaryKey = "123";

        // Act
        var auditEntity = new AuditEntity
        {
            Id = id,
            UserId = userId,
            Type = type,
            TableName = tableName,
            DateTime = dateTime,
            OldValues = oldValues,
            NewValues = newValues,
            AffectedColumns = affectedColumns,
            PrimaryKey = primaryKey
        };

        // Assert
        Assert.Equal(id, auditEntity.Id);
        Assert.Equal(userId, auditEntity.UserId);
        Assert.Equal(type, auditEntity.Type);
        Assert.Equal(tableName, auditEntity.TableName);
        Assert.Equal(dateTime, auditEntity.DateTime);
        Assert.Equal(oldValues, auditEntity.OldValues);
        Assert.Equal(newValues, auditEntity.NewValues);
        Assert.Equal(affectedColumns, auditEntity.AffectedColumns);
        Assert.Equal(primaryKey, auditEntity.PrimaryKey);
    }

    [Fact]
    public void AuditEntity_ShouldImplementIEntity()
    {
        // Arrange & Act
        var auditEntity = new AuditEntity { Id = Guid.NewGuid() };

        // Assert
        Assert.IsAssignableFrom<IEntity<Guid>>(auditEntity);
    }
}