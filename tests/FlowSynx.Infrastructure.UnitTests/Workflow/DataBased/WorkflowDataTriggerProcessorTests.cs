using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Services;
using FlowSynx.Application.Workflow;
using FlowSynx.Domain.Trigger;
using FlowSynx.Domain.Workflow;
using FlowSynx.Infrastructure.Workflow.Triggers.DataBased;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FlowSynx.Infrastructure.UnitTests.Workflow.DataBased;

public class WorkflowDataTriggerProcessorTests
{
    [Fact]
    public async Task ProcessTriggersAsync_WithChanges_QueuesExecutionWithTriggerPayload()
    {
        // Arrange
        var triggerEntity = CreateTriggerEntity();
        var dataChange = new DataChangeEvent(
            source: "TESTPROVIDER",
            table: "Orders",
            operation: DataChangeOperation.Update,
            primaryKey: 42,
            currentValues: new Dictionary<string, object?> { ["amount"] = 110 },
            previousValues: new Dictionary<string, object?> { ["amount"] = 100 },
            timestamp: DateTimeOffset.UtcNow,
            cursor: "orders-42");

        var triggerServiceMock = new Mock<IWorkflowTriggerService>();
        triggerServiceMock
            .Setup(s => s.GetActiveTriggersByTypeAsync(WorkflowTriggerType.DataBased, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowTriggerEntity> { triggerEntity });

        var executionEntity = new WorkflowExecutionEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = triggerEntity.WorkflowId,
            UserId = triggerEntity.UserId,
            WorkflowDefinition = "{}",
            ExecutionStart = DateTime.UtcNow,
            Status = WorkflowExecutionStatus.Pending,
            TaskExecutions = new List<WorkflowTaskExecutionEntity>()
        };

        var orchestratorMock = new Mock<IWorkflowOrchestrator>();
        orchestratorMock
            .Setup(o => o.CreateWorkflowExecutionAsync(triggerEntity.UserId, triggerEntity.WorkflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(executionEntity);

        var queuedRequests = new List<ExecutionQueueRequest>();
        var queueMock = new Mock<IWorkflowExecutionQueue>();
        queueMock
            .Setup(q => q.QueueExecutionAsync(It.IsAny<ExecutionQueueRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ExecutionQueueRequest, CancellationToken>((request, _) => queuedRequests.Add(request))
            .Returns(ValueTask.CompletedTask);

        var providerMock = new Mock<IDataChangeProvider>();
        providerMock
            .Setup(p => p.GetChangesAsync(It.IsAny<DataTriggerConfiguration>(), It.IsAny<DataTriggerState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataChangeEvent> { dataChange });

        var providerFactoryMock = new Mock<IDataChangeProviderFactory>();
        providerFactoryMock
            .Setup(f => f.Resolve("TESTPROVIDER"))
            .Returns(providerMock.Object);

        var clockMock = new Mock<ISystemClock>();
        clockMock.SetupGet(c => c.UtcNow).Returns(DateTime.UtcNow);

        var localizationMock = CreateLocalizationMock();
        var loggerMock = new Mock<ILogger<WorkflowDataTriggerProcessor>>();

        var processor = new WorkflowDataTriggerProcessor(
            loggerMock.Object,
            triggerServiceMock.Object,
            orchestratorMock.Object,
            queueMock.Object,
            providerFactoryMock.Object,
            clockMock.Object,
            localizationMock.Object);

        // Act
        await processor.ProcessTriggersAsync(CancellationToken.None);

        // Assert
        Assert.Single(queuedRequests);
        var request = queuedRequests.Single();
        Assert.NotNull(request.Trigger);
        Assert.Equal(WorkflowTriggerType.DataBased, request.Trigger!.Type);
        Assert.True(request.Trigger.Properties.TryGetValue("provider", out var providerValue));
        Assert.Equal("TESTPROVIDER", providerValue);

        var eventPayload = Assert.IsType<Dictionary<string, object?>>(request.Trigger.Properties["event"]);
        Assert.Equal("Orders", eventPayload["table"]);
        Assert.Equal(DataChangeOperation.Update.ToString(), Assert.IsType<string>(eventPayload["operation"]));
        Assert.True(eventPayload.ContainsKey("diff"));

        providerMock.Verify(p => p.GetChangesAsync(It.IsAny<DataTriggerConfiguration>(), It.IsAny<DataTriggerState>(), It.IsAny<CancellationToken>()), Times.Once);
        orchestratorMock.Verify(o => o.CreateWorkflowExecutionAsync(triggerEntity.UserId, triggerEntity.WorkflowId, It.IsAny<CancellationToken>()), Times.Once);
        queueMock.Verify(q => q.QueueExecutionAsync(It.IsAny<ExecutionQueueRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessTriggersAsync_RespectsPollInterval()
    {
        // Arrange
        var triggerEntity = CreateTriggerEntity();

        var triggerServiceMock = new Mock<IWorkflowTriggerService>();
        triggerServiceMock
            .Setup(s => s.GetActiveTriggersByTypeAsync(WorkflowTriggerType.DataBased, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowTriggerEntity> { triggerEntity });

        var orchestratorMock = new Mock<IWorkflowOrchestrator>();
        orchestratorMock
            .Setup(o => o.CreateWorkflowExecutionAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowExecutionEntity
            {
                Id = Guid.NewGuid(),
                WorkflowId = triggerEntity.WorkflowId,
                UserId = triggerEntity.UserId,
                WorkflowDefinition = "{}",
                ExecutionStart = DateTime.UtcNow,
                Status = WorkflowExecutionStatus.Pending,
                TaskExecutions = new List<WorkflowTaskExecutionEntity>()
            });

        var queueMock = new Mock<IWorkflowExecutionQueue>();
        queueMock
            .Setup(q => q.QueueExecutionAsync(It.IsAny<ExecutionQueueRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var providerMock = new Mock<IDataChangeProvider>();
        providerMock
            .Setup(p => p.GetChangesAsync(It.IsAny<DataTriggerConfiguration>(), It.IsAny<DataTriggerState>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataChangeEvent>());

        var providerFactoryMock = new Mock<IDataChangeProviderFactory>();
        providerFactoryMock
            .Setup(f => f.Resolve("TESTPROVIDER"))
            .Returns(providerMock.Object);

        var now = DateTime.UtcNow;
        var clockMock = new Mock<ISystemClock>();
        clockMock.SetupSequence(c => c.UtcNow)
            .Returns(now)
            .Returns(now.AddSeconds(1))
            .Returns(now.AddSeconds(10));

        var localizationMock = CreateLocalizationMock();
        var loggerMock = new Mock<ILogger<WorkflowDataTriggerProcessor>>();

        var processor = new WorkflowDataTriggerProcessor(
            loggerMock.Object,
            triggerServiceMock.Object,
            orchestratorMock.Object,
            queueMock.Object,
            providerFactoryMock.Object,
            clockMock.Object,
            localizationMock.Object);

        // Act
        await processor.ProcessTriggersAsync(CancellationToken.None); // should poll
        await processor.ProcessTriggersAsync(CancellationToken.None); // should skip (interval not elapsed)
        await processor.ProcessTriggersAsync(CancellationToken.None); // interval elapsed -> poll again

        // Assert
        providerMock.Verify(p => p.GetChangesAsync(It.IsAny<DataTriggerConfiguration>(), It.IsAny<DataTriggerState>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static WorkflowTriggerEntity CreateTriggerEntity()
    {
        return new WorkflowTriggerEntity
        {
            Id = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            UserId = "user-1",
            Type = WorkflowTriggerType.DataBased,
            Status = WorkflowTriggerStatus.Active,
            Properties = new Dictionary<string, object>
            {
                ["provider"] = "TESTPROVIDER",
                ["tables"] = new[] { "Orders" },
                ["events"] = new[] { "UPDATE" },
                ["pollIntervalSec"] = 5,
                ["settings"] = new Dictionary<string, object>()
            }
        };
    }

    private static Mock<ILocalization> CreateLocalizationMock()
    {
        var localizationMock = new Mock<ILocalization>();
        localizationMock
            .Setup(l => l.Get(It.IsAny<string>()))
            .Returns<string>(key => key);
        localizationMock
            .Setup(l => l.Get(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns<string, object[]>((key, _) => key);
        return localizationMock;
    }
}
