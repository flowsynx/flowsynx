using FlowSynx.Application.AI;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Serialization;
using FlowSynx.Infrastructure.AI;
using FlowSynx.Infrastructure.Workflow;
using Moq;

namespace FlowSynx.Infrastructure.UnitTests.Workflow
{
    public class WorkflowIntentServiceTests
    {
        [Fact]
        public void Ctor_Throws_When_AiFactory_Is_Null()
        {
            // Arrange
            var deserializer = new Mock<IJsonDeserializer>().Object;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => new WorkflowIntentService(null!, deserializer));

            // Assert
            Assert.Equal("aiFactory", ex.ParamName);
        }

        [Fact]
        public void Ctor_Throws_When_Deserializer_Is_Null()
        {
            // Arrange
            var aiFactory = new Mock<IAiFactory>().Object;

            // Act
            var ex = Assert.Throws<ArgumentNullException>(() => new WorkflowIntentService(aiFactory, null!));

            // Assert
            Assert.Equal("deserializer", ex.ParamName);
        }

        [Fact]
        public async Task SynthesizeAsync_UsesPlan_FromJson_WhenPresent()
        {
            // Arrange
            var goal = "Build a CI pipeline";
            var capabilitiesJson = "{\"plugins\":[\"git\",\"build\",\"test\"]}";
            var rawJson = "{\"plan\":\"This is the generated plan.\",\"name\":\"any\",\"tasks\":[]}";
            var tokenSource = new CancellationTokenSource();

            var aiProvider = new Mock<IAiProvider>(MockBehavior.Strict);
            aiProvider
                .Setup(p => p.GenerateWorkflowJsonAsync(goal, capabilitiesJson, It.Is<CancellationToken>(ct => ct == tokenSource.Token)))
                .ReturnsAsync(rawJson);

            var aiFactory = new Mock<IAiFactory>(MockBehavior.Strict);
            aiFactory
                .Setup(f => f.GetDefaultProvider())
                .Returns(aiProvider.Object);

            var def = new WorkflowDefinition
            {
                Name = "wf",
                Tasks = new List<WorkflowTask>()
            };

            var deserializer = new Mock<IJsonDeserializer>(MockBehavior.Strict);
            deserializer
                .Setup(d => d.Deserialize<WorkflowDefinition>(rawJson))
                .Returns(def);

            var sut = new WorkflowIntentService(aiFactory.Object, deserializer.Object);

            // Act
            var (definition, raw, plan) = await sut.SynthesizeAsync(goal, capabilitiesJson, tokenSource.Token);

            // Assert
            Assert.Same(def, definition);
            Assert.Equal(rawJson, raw);
            Assert.Equal("This is the generated plan.", plan);

            aiFactory.Verify(f => f.GetDefaultProvider(), Times.Once);
            aiProvider.VerifyAll();
            deserializer.VerifyAll();
        }

        [Fact]
        public async Task SynthesizeAsync_FallsBack_ToDefaultPlan_When_PlanMissing()
        {
            // Arrange
            var goal = "Provision infrastructure";
            var capabilitiesJson = null as string;
            var rawJson = "{\"name\":\"infra\",\"tasks\":[]}";
            var token = CancellationToken.None;

            var aiProvider = new Mock<IAiProvider>();
            aiProvider
                .Setup(p => p.GenerateWorkflowJsonAsync(goal, capabilitiesJson, token))
                .ReturnsAsync(rawJson);

            var aiFactory = new Mock<IAiFactory>();
            aiFactory.Setup(f => f.GetDefaultProvider()).Returns(aiProvider.Object);

            var def = new WorkflowDefinition
            {
                Name = "infra",
                Tasks = new List<WorkflowTask>()
            };

            var deserializer = new Mock<IJsonDeserializer>();
            deserializer.Setup(d => d.Deserialize<WorkflowDefinition>(rawJson)).Returns(def);

            var sut = new WorkflowIntentService(aiFactory.Object, deserializer.Object);

            // Act
            var (_, _, plan) = await sut.SynthesizeAsync(goal, capabilitiesJson, token);

            // Assert
            Assert.Equal("Proposed workflow generated from intent.", plan);
        }

        [Fact]
        public async Task SynthesizeAsync_FallsBack_ToDefaultPlan_When_InvalidJson()
        {
            // Arrange
            var goal = "Deploy app";
            var capabilitiesJson = "{}";
            var rawJson = "not-json";
            var token = new CancellationTokenSource().Token;

            var aiProvider = new Mock<IAiProvider>();
            aiProvider
                .Setup(p => p.GenerateWorkflowJsonAsync(goal, capabilitiesJson, token))
                .ReturnsAsync(rawJson);

            var aiFactory = new Mock<IAiFactory>();
            aiFactory.Setup(f => f.GetDefaultProvider()).Returns(aiProvider.Object);

            var def = new WorkflowDefinition
            {
                Name = "deploy",
                Tasks = new List<WorkflowTask>()
            };

            var deserializer = new Mock<IJsonDeserializer>();
            deserializer.Setup(d => d.Deserialize<WorkflowDefinition>(rawJson)).Returns(def);

            var sut = new WorkflowIntentService(aiFactory.Object, deserializer.Object);

            // Act
            var (_, _, plan) = await sut.SynthesizeAsync(goal, capabilitiesJson, token);

            // Assert
            Assert.Equal("Proposed workflow generated from intent.", plan);
        }

        [Fact]
        public async Task SynthesizeAsync_Sets_DefaultName_When_Empty_Or_Whitespace()
        {
            // Arrange
            var goal = "Analyze logs";
            var capabilitiesJson = null as string;
            var rawJson = "{\"tasks\":[]}";
            var token = CancellationToken.None;

            var aiProvider = new Mock<IAiProvider>();
            aiProvider
                .Setup(p => p.GenerateWorkflowJsonAsync(goal, capabilitiesJson, token))
                .ReturnsAsync(rawJson);

            var aiFactory = new Mock<IAiFactory>();
            aiFactory.Setup(f => f.GetDefaultProvider()).Returns(aiProvider.Object);

            // Return a definition with empty name to trigger defaulting
            var def = new WorkflowDefinition
            {
                Name = "  ",
                Tasks = new List<WorkflowTask>()
            };

            var deserializer = new Mock<IJsonDeserializer>();
            deserializer.Setup(d => d.Deserialize<WorkflowDefinition>(rawJson)).Returns(def);

            var sut = new WorkflowIntentService(aiFactory.Object, deserializer.Object);

            // Act
            var (definition, raw, plan) = await sut.SynthesizeAsync(goal, capabilitiesJson, token);

            // Assert
            Assert.Equal("auto-generated-workflow", definition.Name);
            Assert.Equal(rawJson, raw);
            Assert.Equal("Proposed workflow generated from intent.", plan);
        }

        [Fact]
        public async Task SynthesizeAsync_Preserves_Name_When_Present()
        {
            // Arrange
            var goal = "ETL job";
            var capabilitiesJson = "{\"plugins\":[\"extract\",\"transform\",\"load\"]}";
            var rawJson = "{\"name\":\"etl\",\"tasks\":[]}";
            var token = CancellationToken.None;

            var aiProvider = new Mock<IAiProvider>();
            aiProvider
                .Setup(p => p.GenerateWorkflowJsonAsync(goal, capabilitiesJson, token))
                .ReturnsAsync(rawJson);

            var aiFactory = new Mock<IAiFactory>();
            aiFactory.Setup(f => f.GetDefaultProvider()).Returns(aiProvider.Object);

            var def = new WorkflowDefinition
            {
                Name = "etl",
                Tasks = new List<WorkflowTask>()
            };

            var deserializer = new Mock<IJsonDeserializer>();
            deserializer.Setup(d => d.Deserialize<WorkflowDefinition>(rawJson)).Returns(def);

            var sut = new WorkflowIntentService(aiFactory.Object, deserializer.Object);

            // Act
            var (definition, raw, plan) = await sut.SynthesizeAsync(goal, capabilitiesJson, token);

            // Assert
            Assert.Equal("etl", definition.Name);
            Assert.Equal(rawJson, raw);
            Assert.Equal("Proposed workflow generated from intent.", plan);
        }

        [Fact]
        public async Task SynthesizeAsync_Passes_CancellationToken_To_Provider()
        {
            // Arrange
            var goal = "Backup database";
            var capabilitiesJson = "{}";
            var rawJson = "{\"name\":\"backup\",\"tasks\":[]}";

            using var cts = new CancellationTokenSource();

            var aiProvider = new Mock<IAiProvider>(MockBehavior.Strict);
            aiProvider
                .Setup(p => p.GenerateWorkflowJsonAsync(goal, capabilitiesJson, It.Is<CancellationToken>(ct => ct == cts.Token)))
                .ReturnsAsync(rawJson);

            var aiFactory = new Mock<IAiFactory>(MockBehavior.Strict);
            aiFactory.Setup(f => f.GetDefaultProvider()).Returns(aiProvider.Object);

            var def = new WorkflowDefinition
            {
                Name = "backup",
                Tasks = new List<WorkflowTask>()
            };

            var deserializer = new Mock<IJsonDeserializer>(MockBehavior.Strict);
            deserializer.Setup(d => d.Deserialize<WorkflowDefinition>(rawJson)).Returns(def);

            var sut = new WorkflowIntentService(aiFactory.Object, deserializer.Object);

            // Act
            _ = await sut.SynthesizeAsync(goal, capabilitiesJson, cts.Token);

            // Assert
            aiProvider.VerifyAll();
            aiFactory.VerifyAll();
            deserializer.VerifyAll();
        }
    }
}