using System.Reflection;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Infrastructure.Workflow;

namespace FlowSynx.Infrastructure.UnitTests.Workflow;

public class WorkflowOptimizationServiceTests
{
    private static WorkflowDefinition CreateDefinition(
        IEnumerable<WorkflowTask> tasks,
        int? dop = null,
        int? workflowTimeout = null)
    {
        return new WorkflowDefinition
        {
            Name = "TestWorkflow",
            Description = "desc",
            Variables = new Dictionary<string, object?>(),
            Configuration = new WorkflowConfiguration
            {
                DegreeOfParallelism = dop,
                TimeoutMilliseconds = workflowTimeout
            },
            Tasks = tasks.ToList()
        };
    }

    [Fact]
    public async Task OptimizeAsync_SetsDegreeOfParallelism_BasedOnMaxWidth()
    {
        // Arrange: three parallel tasks => max width3
        var tasks = new List<WorkflowTask>
        {
             new WorkflowTask("A") { Name = "A" },
             new WorkflowTask("B") { Name = "B" },
             new WorkflowTask("C") { Name = "C" }
        };
        var definition = CreateDefinition(tasks, dop: null, workflowTimeout: null);
        var sut = new WorkflowOptimizationService();

        // Act
        var (optimized, explanation) = await sut.OptimizeAsync(definition, CancellationToken.None);

        // Assert
        var expectedDop = Math.Clamp(3, 2, Environment.ProcessorCount * 2);
        Assert.Equal(expectedDop, optimized.Configuration.DegreeOfParallelism);
        Assert.Contains($"Adjusted DegreeOfParallelism to {expectedDop} based on computed max DAG width 3.", explanation);
    }

    [Fact]
    public async Task OptimizeAsync_SetsDefaultWorkflowTimeout_WhenMissing()
    {
        // Arrange: one task
        var tasks = new List<WorkflowTask> { new WorkflowTask("A") { Name = "A" } };
        var definition = CreateDefinition(tasks, dop: null, workflowTimeout: null);
        var sut = new WorkflowOptimizationService();

        // Act
        var (optimized, explanation) = await sut.OptimizeAsync(definition, CancellationToken.None);

        // Assert
        Assert.Equal(30 * 60 * 1000, optimized.Configuration.TimeoutMilliseconds);
        Assert.Contains("Set workflow timeout to 30m (ms) as a conservative default.", explanation);
    }

    [Fact]
    public async Task OptimizeAsync_SetsDefaultTaskTimeouts_WhenMissing()
    {
        // Arrange: T1 missing timeout, T2 has timeout
        var tasks = new List<WorkflowTask>
        {
            new WorkflowTask("T1") { Name = "T1", TimeoutMilliseconds = null },
            new WorkflowTask("T2") { Name = "T2", TimeoutMilliseconds = 999 }
        };
        var definition = CreateDefinition(tasks);
        var sut = new WorkflowOptimizationService();

        // Act
        var (optimized, explanation) = await sut.OptimizeAsync(definition, CancellationToken.None);

        // Assert
        var t1 = optimized.Tasks.Single(t => t.Name == "T1");
        var t2 = optimized.Tasks.Single(t => t.Name == "T2");
        Assert.Equal(2 * 60 * 1000, t1.TimeoutMilliseconds);
        Assert.Equal(999, t2.TimeoutMilliseconds);
        Assert.Contains("Task 'T1': applied default timeout 2m (ms).", explanation);
        Assert.DoesNotContain("Task 'T2': applied default timeout", explanation);
    }

    [Fact]
    public async Task OptimizeAsync_NormalizesDependencies_RemovesDuplicates()
    {
        // Arrange
        var task = new WorkflowTask("T1")
        {
            Name = "T1",
            Dependencies = new List<string> { "A", "A", "B", "B" }
        };
        var definition = CreateDefinition(new[] { task,
             new WorkflowTask("A") { Name = "A" },
             new WorkflowTask("B") { Name = "B" }
        });
        var sut = new WorkflowOptimizationService();

        // Act
        var (optimized, _) = await sut.OptimizeAsync(definition, CancellationToken.None);

        // Assert
        var optimizedTask = optimized.Tasks.Single(t => t.Name == "T1");
        Assert.Equal(new[] { "A", "B" }, optimizedTask.Dependencies);
    }

    [Fact]
    public async Task OptimizeAsync_NoChanges_ReturnsNoChangesExplanation()
    {
        // Arrange: width2 (two independent tasks) -> recommended DOP =2
        var tasks = new List<WorkflowTask>
        {
            new WorkflowTask("A") { Name = "A", TimeoutMilliseconds = 100 },
            new WorkflowTask("B") { Name = "B", TimeoutMilliseconds = 200 }
        };
        var recommendedDop = Math.Clamp(2, 2, Environment.ProcessorCount * 2);
        var definition = CreateDefinition(tasks, dop: recommendedDop, workflowTimeout: 30 * 60 * 1000);
        var sut = new WorkflowOptimizationService();

        // Act
        var (_, explanation) = await sut.OptimizeAsync(definition, CancellationToken.None);

        // Assert
        Assert.Equal("No changes required; workflow already optimized.", explanation);
    }

    [Fact]
    public void ComputeLevels_BuildsLevelsAndMaxWidth_ForDAG()
    {
        // Arrange DAG:
        // Level0: A, E
        // Level1: B(dep A), C(dep A)
        // Level2: D(dep B,C)
        var A = new WorkflowTask("A") { Name = "A" };
        var B = new WorkflowTask("B") { Name = "B", Dependencies = new List<string> { "A" } };
        var C = new WorkflowTask("C") { Name = "C", Dependencies = new List<string> { "A" } };
        var D = new WorkflowTask("D") { Name = "D", Dependencies = new List<string> { "B", "C" } };
        var E = new WorkflowTask("E") { Name = "E" };
        var tasks = new List<WorkflowTask> { A, B, C, D, E };

        // Act (via reflection)
        var (levels, maxWidth) = InvokeComputeLevels(tasks);

        // Assert: max width2, and levels contain expected names (order-insensitive)
        Assert.Equal(2, maxWidth);
        Assert.Equal(3, levels.Count);
        var level0 = levels[0].Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var level1 = levels[1].Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        var level2 = levels[2].Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        Assert.True(new HashSet<string>(new[] { "A", "E" }, StringComparer.Ordinal).SetEquals(level0));
        Assert.True(new HashSet<string>(new[] { "B", "C" }, StringComparer.Ordinal).SetEquals(level1));
        Assert.True(new HashSet<string>(new[] { "D" }, StringComparer.Ordinal).SetEquals(level2));
    }

    [Fact]
    public void ComputeLevels_IgnoresUnknownDependencies_TreatsAsRoot()
    {
        // Arrange: F depends on UNKNOWN only -> treated as no deps
        var F = new WorkflowTask("F") { Name = "F", Dependencies = new List<string> { "UNKNOWN" } };
        var tasks = new List<WorkflowTask> { F };

        // Act
        var (levels, maxWidth) = InvokeComputeLevels(tasks);

        // Assert
        Assert.Equal(1, maxWidth);
        Assert.Single(levels);
        Assert.Single(levels[0]);
        Assert.Equal("F", levels[0][0].Name);
    }

    private static (List<List<WorkflowTask>> Levels, int MaxWidth) InvokeComputeLevels(List<WorkflowTask> tasks)
    {
        var type = typeof(WorkflowOptimizationService);
        var method = type.GetMethod("ComputeLevels", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, new object[] { tasks });
        Assert.NotNull(result);

        var resultType = result!.GetType();
        var item1Field = resultType.GetField("Item1");
        var item2Field = resultType.GetField("Item2");
        Assert.NotNull(item1Field);
        Assert.NotNull(item2Field);

        var levelsObj = item1Field!.GetValue(result);
        var maxWidthObj = item2Field!.GetValue(result);

        var levels = Assert.IsAssignableFrom<List<List<WorkflowTask>>>(levelsObj);
        var maxWidth = Assert.IsType<int>(maxWidthObj);
        return (levels, maxWidth);
    }
}