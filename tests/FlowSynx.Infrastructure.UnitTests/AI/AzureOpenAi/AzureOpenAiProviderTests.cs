using FlowSynx.Application.Configuration.Integrations.PluginRegistry;
using FlowSynx.Infrastructure.AI.AzureOpenAi;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace FlowSynx.Infrastructure.UnitTests.AI.AzureOpenAi;

public sealed class AzureOpenAiProviderTests
{
    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responder;
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public TestHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                var ms = new MemoryStream();
                request.Content.CopyToAsync(ms).GetAwaiter().GetResult();
                LastRequestBody = Encoding.UTF8.GetString(ms.ToArray());
            }
            return Task.FromResult(_responder(request, cancellationToken));
        }
    }

    private static AzureOpenAiProvider CreateProvider(TestHandler handler)
    {
        var http = new HttpClient(handler);
        var logger = Mock.Of<ILogger<AzureOpenAiProvider>>();
        var pluginRegistryConfig = Mock.Of<PluginRegistryConfiguration>();
        return new AzureOpenAiProvider(http, logger, pluginRegistryConfig);
    }

    [Fact]
    public void Ctor_Throws_On_Null_HttpClient()
    {
        var logger = Mock.Of<ILogger<AzureOpenAiProvider>>();
        var pluginRegistryConfig = Mock.Of<PluginRegistryConfiguration>();
        Assert.Throws<ArgumentNullException>(() => new AzureOpenAiProvider(null!, logger, pluginRegistryConfig));
    }

    [Fact]
    public void Name_Default_And_CanBeChanged()
    {
        var handler = new TestHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        var provider = CreateProvider(handler);

        Assert.Equal("AzureOpenAI", provider.Name);
        provider.Name = "Custom";
        Assert.Equal("Custom", provider.Name);
    }

    [Fact]
    public async Task Configure_Values_Are_Used_In_Request()
    {
        // Arrange
        var responseJson = JsonDocument.Parse("{\"choices\": [{\"message\": {\"content\": \"{\\\"ok\\\":true}\"}}]}");
        var handler = new TestHandler((_, _) =>
        {
            var msg = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(responseJson)
            };
            return msg;
        });
        var provider = CreateProvider(handler);
        provider.Configure(new()
        {
            ["Endpoint"] = "https://example",
            ["ApiKey"] = "k123",
            ["Deployment"] = "my-deploy"
        });

        // Act
        var result = await provider.GenerateWorkflowJsonAsync("do X", "{}", CancellationToken.None);

        // Assert
        Assert.Equal("{\"ok\":true}", result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://example/openai/deployments/my-deploy/chat/completions?api-version=2024-08-01-preview", handler.LastRequest!.RequestUri!.ToString());
        Assert.True(handler.LastRequest!.Headers.TryGetValues("api-key", out var values));
        Assert.Equal("k123", Assert.Single(values!));

        // Verify body contains goal and capabilities via BuildPromptAsync
        var bodyJson = JsonDocument.Parse(handler.LastRequestBody!).RootElement;
        var messages = bodyJson.GetProperty("messages").EnumerateArray().ToArray();
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        var userContent = messages[1].GetProperty("content").GetString();
        Assert.Contains("do X", userContent);
        Assert.Contains("You MUST output ONLY a JSON object", userContent);
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_Throws_On_NonSuccess_Status()
    {
        var handler = new TestHandler((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var provider = CreateProvider(handler);
        provider.Configure(new() { ["Endpoint"] = "https://example" });

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        await provider.GenerateWorkflowJsonAsync("g", null, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_Throws_When_Choices_Missing()
    {
        // Response lacks 'choices' array; accessing it should throw KeyNotFoundException
        var handler = new TestHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        });
        var provider = CreateProvider(handler);
        provider.Configure(new() { ["Endpoint"] = "https://example" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GenerateWorkflowJsonAsync("g", null, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_Throws_On_Empty_Message_Content()
    {
        using var json = JsonDocument.Parse("{\"choices\": [{\"message\": {\"content\": \" \"}}]}");
        var handler = new TestHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(json)
        });
        var provider = CreateProvider(handler);
        provider.Configure(new() { ["Endpoint"] = "https://example" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        await provider.GenerateWorkflowJsonAsync("g", null, CancellationToken.None));
        Assert.Equal("LLM returned empty JSON.", ex.Message);
    }

    [Fact]
    public async Task GenerateWorkflowJsonAsync_Returns_Message_Content_On_Success()
    {
        using var json = JsonDocument.Parse("{\"choices\": [{\"message\": {\"content\": \"{\\\"id\\\":1}\"}}]}");
        var handler = new TestHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(json)
        });
        var provider = CreateProvider(handler);
        provider.Configure(new()
        {
            ["Endpoint"] = "https://example",
            ["Deployment"] = "dep"
        });

        var result = await provider.GenerateWorkflowJsonAsync("build", "{}", CancellationToken.None);
        Assert.Equal("{\"id\":1}", result);
    }
}