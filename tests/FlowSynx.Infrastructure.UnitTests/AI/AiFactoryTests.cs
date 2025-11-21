using FlowSynx.Application.AI;
using FlowSynx.Application.Configuration.Core.AI;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using FlowSynx.Infrastructure.AI;

namespace FlowSynx.Infrastructure.UnitTests.AI;

public class AiFactoryTests
{
    private sealed class TestConfigurableProvider : IAiProvider, IConfigurableAi
    {
        public string Name { get; set; } = "AzureOpenAI";
        public Dictionary<string, string>? ReceivedConfig { get; private set; }
        public void Configure(Dictionary<string, string> configuration) => ReceivedConfig = configuration;

        public Task<AgentExecutionResult> ExecuteAgenticTaskAsync(AgentExecutionContext context, AgentConfiguration config, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GenerateWorkflowJsonAsync(string goal, string? capabilitiesJson, CancellationToken cancellationToken) => Task.FromResult("{}");

        public Task<string> PlanTaskExecutionAsync(AgentExecutionContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<(bool IsValid, string? ValidationMessage)> ValidateTaskAsync(AgentExecutionContext context, object? output, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class TestNonConfigurableProvider : IAiProvider
    {
        public string Name { get; set; } = "AzureOpenAI";

        public Task<AgentExecutionResult> ExecuteAgenticTaskAsync(AgentExecutionContext context, AgentConfiguration config, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GenerateWorkflowJsonAsync(string goal, string? capabilitiesJson, CancellationToken cancellationToken) => Task.FromResult("{}");

        public Task<string> PlanTaskExecutionAsync(AgentExecutionContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<(bool IsValid, string? ValidationMessage)> ValidateTaskAsync(AgentExecutionContext context, object? output, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void GetDefaultProvider_Throws_WhenDisabled()
    {
        var config = new AiConfiguration
        {
            Enabled = false,
            DefaultProvider = "AzureOpenAI",
            Providers = new() { ["AzureOpenAI"] = new AiProviderConfiguration() }
        };
        var factory = new AiFactory(config, new[] { new TestConfigurableProvider() });
        var ex = Assert.Throws<InvalidOperationException>(() => factory.GetDefaultProvider());
        Assert.Equal("AI configuration is not enabled.", ex.Message);
    }

    [Fact]
    public void GetDefaultProvider_Throws_WhenProviderConfigMissing()
    {
        var config = new AiConfiguration
        {
            Enabled = true,
            DefaultProvider = "OpenAI",
            Providers = new() // No matching key
        };
        var factory = new AiFactory(config, Array.Empty<IAiProvider>());
        var ex = Assert.Throws<InvalidOperationException>(() => factory.GetDefaultProvider());
        Assert.Equal("No AI configuration found for 'OpenAI'.", ex.Message);
    }

    [Fact]
    public void GetDefaultProvider_Throws_WhenImplementationMissing()
    {
        var providerConfig = new AiProviderConfiguration { ["Endpoint"] = "https://example" };
        var config = new AiConfiguration
        {
            Enabled = true,
            DefaultProvider = "AzureOpenAI",
            Providers = new() { ["AzureOpenAI"] = providerConfig }
        };
        var factory = new AiFactory(config, Array.Empty<IAiProvider>());
        var ex = Assert.Throws<InvalidOperationException>(() => factory.GetDefaultProvider());
        Assert.Contains("No AI provider implementation found", ex.Message);
    }

    [Fact]
    public void GetDefaultProvider_Configures_IfConfigurable()
    {
        var providerConfig = new AiProviderConfiguration { ["Endpoint"] = "https://example" };
        var config = new AiConfiguration
        {
            Enabled = true,
            DefaultProvider = "AzureOpenAI",
            Providers = new() { ["AzureOpenAI"] = providerConfig }
        };
        var provider = new TestConfigurableProvider();
        var factory = new AiFactory(config, new[] { provider });

        var result = factory.GetDefaultProvider();

        Assert.Same(provider, result);
        Assert.NotNull(provider.ReceivedConfig);
        Assert.Equal(providerConfig, provider.ReceivedConfig);
    }

    [Fact]
    public void GetDefaultProvider_DoesNotConfigure_WhenNotConfigurable()
    {
        var providerConfig = new AiProviderConfiguration { ["Endpoint"] = "https://example" };
        var config = new AiConfiguration
        {
            Enabled = true,
            DefaultProvider = "AzureOpenAI",
            Providers = new() { ["AzureOpenAI"] = providerConfig }
        };
        var provider = new TestNonConfigurableProvider();
        var factory = new AiFactory(config, new[] { provider });

        var result = factory.GetDefaultProvider();

        Assert.Same(provider, result);
        // Nothing to assert about configuration invocation since provider is not configurable
    }

    [Fact]
    public void GetDefaultProvider_MatchesProviderName_IgnoringCase()
    {
        var providerConfig = new AiProviderConfiguration();
        var config = new AiConfiguration
        {
            Enabled = true,
            DefaultProvider = "AzureOpenAI",
            Providers = new() { ["AzureOpenAI"] = providerConfig }
        };
        var provider = new TestNonConfigurableProvider { Name = "azureopenai" }; // different casing
        var factory = new AiFactory(config, new[] { provider });

        var result = factory.GetDefaultProvider();

        Assert.Same(provider, result);
    }
}