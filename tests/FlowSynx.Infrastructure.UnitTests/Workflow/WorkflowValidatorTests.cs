using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.Infrastructure.Workflow;
using FlowSynx.Infrastructure.Workflow.Parsers;
using FlowSynx.PluginCore.Exceptions;
using Moq;

namespace FlowSynx.Tests.Infrastructure.Workflow
{
    public class WorkflowValidatorTests
    {
        private readonly Mock<ILocalization> _localizationMock;
        private readonly Mock<IExpressionParserFactory> _parserFactoryMock;
        private readonly Mock<IExpressionParser> _parserMock;
        private readonly Mock<IPlaceholderReplacer> _placeholderReplacerMock;
        private readonly WorkflowValidator _validator;

        public WorkflowValidatorTests()
        {
            _localizationMock = new Mock<ILocalization>();
            _parserFactoryMock = new Mock<IExpressionParserFactory>();
            _parserMock = new Mock<IExpressionParser>();
            _placeholderReplacerMock = new Mock<IPlaceholderReplacer>();

            _localizationMock
                .Setup(l => l.Get(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns((string key, object[] args) => key);

            _parserFactoryMock
                .Setup(f => f.CreateParser(It.IsAny<Dictionary<string, object?>>(), It.IsAny<Dictionary<string, object?>>()))
                .Returns(_parserMock.Object);

            _placeholderReplacerMock
                .Setup(r => r.ReplacePlaceholders(It.IsAny<string>(), It.IsAny<IExpressionParser>()))
                .Returns((string val, IExpressionParser p) => val);

            _validator = new WorkflowValidator(
                _localizationMock.Object,
                _parserFactoryMock.Object,
                _placeholderReplacerMock.Object);
        }

        [Fact]
        public void Validate_Should_Pass_When_No_Cycle()
        {
            var tasks = new List<WorkflowTask>
            {
                new("TaskA") { Dependencies = new() },
                new("TaskB") { Dependencies = new() { "TaskA" } },
                new("TaskC") { Dependencies = new() { "TaskB" } }
            };

            var definition = new WorkflowDefinition
            {
                Name = "Workflow With no cycle in Dependencies",
                Tasks = tasks
            };

            var exception = Record.Exception(() => _validator.Validate(definition));

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_Should_Throw_When_Cycle_In_Dependencies()
        {
            var tasks = new List<WorkflowTask>
            {
                new("TaskA") { Dependencies = new() { "TaskC" } },
                new("TaskB") { Dependencies = new() { "TaskA" } },
                new("TaskC") { Dependencies = new() { "TaskB" } }
            };

            var definition = new WorkflowDefinition
            {
                Name = "Workflow With cycle in Dependencies",
                Tasks = tasks
            };

            var ex = Assert.Throws<FlowSynxException>(() => _validator.Validate(definition));
            Assert.Equal((int)ErrorCode.WorkflowCyclicDependencies, ex.ErrorCode);
        }

        [Fact]
        public void Validate_Should_Throw_When_Cycle_In_ConditionalBranches()
        {
            var tasks = new List<WorkflowTask>
            {
                new("TaskA")
                {
                    ConditionalBranches = new()
                    {
                        new ConditionalBranch { Expression = "true", TargetTaskName = "TaskB" }
                    }
                },
                new("TaskB")
                {
                    ConditionalBranches = new()
                    {
                        new ConditionalBranch { Expression = "true", TargetTaskName = "TaskA" }
                    }
                }
            };

            var definition = new WorkflowDefinition
            {
                Name = "Workflow With cycle in Conditional Branches",
                Tasks = tasks
            };

            var ex = Assert.Throws<FlowSynxException>(() => _validator.Validate(definition));
            Assert.Equal((int)ErrorCode.WorkflowCyclicDependencies, ex.ErrorCode);
        }

        [Fact]
        public void Validate_Should_Throw_When_Missing_Branch_Target()
        {
            var tasks = new List<WorkflowTask>
            {
                new("TaskA")
                {
                    ConditionalBranches = new()
                    {
                        new ConditionalBranch { Expression = "true", TargetTaskName = "NonExistentTask" }
                    }
                }
            };

            var definition = new WorkflowDefinition
            {
                Name = "Workflow With Missing Target",
                Tasks = tasks
            };

            var ex = Assert.Throws<FlowSynxException>(() => _validator.Validate(definition));
            Assert.Equal((int)ErrorCode.WorkflowMissingDependencies, ex.ErrorCode);
        }

        [Fact]
        public void Validate_Should_Pass_When_ConditionalBranches_Are_Valid_And_No_Cycle()
        {
            var tasks = new List<WorkflowTask>
            {
                new("TaskA")
                {
                    ConditionalBranches = new()
                    {
                        new ConditionalBranch { Expression = "true", TargetTaskName = "TaskB" }
                    }
                },
                new("TaskB")
            };

            var definition = new WorkflowDefinition { 
                Name= "Workflow With Conditional Branches", 
                Tasks = tasks 
            };

            var exception = Record.Exception(() => _validator.Validate(definition));

            Assert.Null(exception);
        }
    }
}
