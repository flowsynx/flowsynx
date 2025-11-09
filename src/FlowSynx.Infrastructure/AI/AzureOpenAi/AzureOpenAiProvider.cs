using FlowSynx.Application.AI;
using FlowSynx.Infrastructure.Secrets.Infisical;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlowSynx.Infrastructure.AI.AzureOpenAi;

public sealed class AzureOpenAiProvider : IAiProvider, IConfigurableAi
{
    private readonly HttpClient _http;
    private readonly ILogger<InfisicalSecretProvider>? _logger;
    private readonly AzureOpenAiConfiguration _config = new AzureOpenAiConfiguration();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public AzureOpenAiProvider(
        HttpClient httpClient,
        ILogger<AzureOpenAiProvider> logger)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string Name { get; set; } = "AzureOpenAI";

    public void Configure(Dictionary<string, string> configuration)
    {
        _config.Endpoint = configuration.GetValueOrDefault("Endpoint", string.Empty);
        _config.ApiKey = configuration.GetValueOrDefault("ApiKey", string.Empty);
        _config.Deployment = configuration.GetValueOrDefault("Deployment", "gpt-4o-mini");
    }

    public async Task<string> GenerateWorkflowJsonAsync(string goal, string? capabilitiesJson, CancellationToken cancellationToken)
    {
        // Minimal chat body; expect JSON-only response
        var body = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a system that outputs only JSON that matches FlowSynx WorkflowDefinition schema. No prose." },
                new { role = "user", content = BuildPrompt(goal, capabilitiesJson) }
            },
            temperature = 0.2,
            response_format = new { type = "json_object" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_config.Endpoint}/openai/deployments/{_config.Deployment}/chat/completions?api-version=2024-08-01-preview");
        req.Headers.Add("api-key", _config.ApiKey);
        req.Content = JsonContent.Create(body, options: _jsonOptions);

        using var res = await _http.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var doc = await res.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken) 
                  ?? throw new InvalidOperationException("Empty LLM response.");

        var json = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("LLM returned empty JSON.");

        return json!;
    }

    private static string BuildPrompt(string goal, string? capabilitiesJson)
    {
        // Keep prompt compact; instruct model about output fields that FlowSynx expects.
        return $@"
Goal:
{goal}

Capabilities (plugins and what they do). Use only these capabilities if provided:
{capabilitiesJson ?? "<none provided>"}

Output strictly a JSON object with:
- name: string
- description: string
- configuration: {{ degreeOfParallelism?: number, timeout?: number }}
- variables?: object
- tasks: array of {{
    name: string,
    type: string,          // plugin type id
    description?: string,
    parameters?: object,
    dependencies?: string[],
    timeout?: number,
    manualApproval?: {{
        enabled: boolean,
        approvers?: string[],
        instructions?: string
    }},
    errorHandling?: object,
    runOnFailureOf?: string[]
}}

Ensure DAG is valid: no cycles; dependencies refer to defined task names; use plugins that match capabilities.
Prefer parallelizable structure when tasks are independent.
";
    }
}