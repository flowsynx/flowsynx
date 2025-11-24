using FlowSynx.Application.AI;
using FlowSynx.Application.Configuration.Integrations.PluginRegistry;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace FlowSynx.Infrastructure.AI.AzureOpenAi;

public sealed class AzureOpenAiProvider : IAiProvider, IConfigurableAi
{
    private const string SystemRole = "system";
    private const string JsonObjectResponseFormat = "json_object";
    private const string ApiKeyHeaderName = "api-key";
    private const string ApplicationJsonMediaType = "application/json";
    private const string ChoicesJsonPath = "choices";
    private const string MessageJsonPath = "message";
    private const string ContentJsonPath = "content";

    private readonly HttpClient _http;
    private readonly ILogger<AzureOpenAiProvider>? _logger;
    private readonly PluginRegistryConfiguration _pluginRegistryConfiguration;
    private readonly AzureOpenAiConfiguration _config = new AzureOpenAiConfiguration();
    private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None
    };

    public AzureOpenAiProvider(
        HttpClient httpClient,
        ILogger<AzureOpenAiProvider> logger,
        PluginRegistryConfiguration pluginRegistryConfiguration)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
        _pluginRegistryConfiguration = pluginRegistryConfiguration ?? throw new ArgumentNullException(nameof(pluginRegistryConfiguration));
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
        var pluginsInfo = await FetchPluginsAsync(cancellationToken);

        // Minimal chat body; expect JSON-only response
        var body = new
        {
            messages = new[]
            {
                new { role = SystemRole, content = "You are a system that outputs only JSON that matches FlowSynx WorkflowDefinition schema. No prose." },
                new { role = "user", content = await BuildPromptAsync(goal, capabilitiesJson, pluginsInfo) }
            },
            temperature = 0.2,
            response_format = new { type = JsonObjectResponseFormat }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_config.Endpoint}/openai/deployments/{_config.Deployment}/chat/completions?api-version=2024-08-01-preview");
        req.Headers.Add(ApiKeyHeaderName, _config.ApiKey);

        var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
        req.Content = new StringContent(jsonBody, Encoding.UTF8, ApplicationJsonMediaType);

        using var res = await _http.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var responseContent = await res.Content.ReadAsStringAsync(cancellationToken);
        var doc = JObject.Parse(responseContent);

        var json = doc[ChoicesJsonPath]?[0]?[MessageJsonPath]?[ContentJsonPath]?.ToString();
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("LLM returned empty JSON.");

        return json!;
    }

    private async Task<string> FetchPluginsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _pluginRegistryConfiguration.Url + "api/plugins?page=";

            var allPlugins = new JArray();

            int currentPage = 1;
            int totalPages = 1; // temporary until we read the first response

            do
            {
                var url = baseUrl + currentPage;

                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var res = await _http.SendAsync(req, cancellationToken);

                if (!res.IsSuccessStatusCode)
                {
                    _logger?.LogWarning(
                        "Failed to fetch plugins from {Url}. Status: {Status}",
                        url, res.StatusCode);
                    break;
                }

                var content = await res.Content.ReadAsStringAsync(cancellationToken);
                var root = JObject.Parse(content);

                // Extract paging values
                currentPage = root["currentPage"]?.Value<int>() ?? currentPage;
                totalPages = root["totalPages"]?.Value<int>() ?? totalPages;

                // Extract plugins in this page
                var pagePlugins = root["data"] as JArray;
                if (pagePlugins != null)
                {
                    foreach (var p in pagePlugins)
                        allPlugins.Add(p);
                }

                currentPage++;

            } while (currentPage <= totalPages);

            // Convert to simplified format (optional)
            var formattedPlugins = new JArray();
            foreach (var plugin in allPlugins)
            {
                formattedPlugins.Add(new JObject
                {
                    ["type"] = plugin["type"]?.ToString() ?? "",
                    ["version"] = plugin["version"]?.ToString() ?? "",
                    ["description"] = plugin["description"]?.ToString() ?? ""
                });
            }

            return formattedPlugins.ToString(Formatting.Indented);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching plugins from registry");
            return "[]";
        }
    }

    private static async Task<string> BuildPromptAsync(
        string goal,
        string? capabilitiesJson,
        string pluginsInfo)
    {
        return $@"
You are FlowSynx-WorkflowGen, an expert system that generates valid FlowSynx WorkflowDefinition JSON objects.

You MUST follow ALL instructions with zero deviation.
Be deterministic, accurate, and strict. Do NOT invent any plugins, parameters, properties, or fields.

---
GOAL:
{goal}

---
PLUGINS AVAILABLE IN REGISTRY:
Use ONLY these plugins.
{pluginsInfo}

---
OUTPUT FORMAT (IMPORTANT):
You MUST output ONLY a JSON object (no markdown, no comments, no explanation).
Your output MUST strictly follow the FlowSynx WorkflowDefinition schema.

Schema to follow:

{{
  ""name"": ""string"",                
  ""description"": ""string"",
  ""variables"": {{ ""key"": ""value"" }},
  ""configuration"": {{
    ""degreeOfParallelism"": 0,
    ""timeout"": 0,
    ""errorHandling"": {{
      ""strategy"": ""Abort"",
      ""triggerPolicy"": {{ ""taskName"": ""string"" }},
      ""retryPolicy"": {{
        ""maxRetries"": 3,
        ""backoffStrategy"": ""Exponential"",
        ""initialDelay"": 1000,
        ""maxDelay"": 60000,
        ""backoffCoefficient"": 2.0
      }}
    }}
  }},
  ""tasks"": [
    {{
      ""name"": ""string"",
      ""description"": ""string"",
      ""type"": {{
        ""plugin"": ""string"",
        ""version"": ""string""
      }},
      ""parameters"": {{ ""key"": ""value"" }},
      ""dependencies"": [""string""],
      ""timeout"": 0,
      ""agent"": {{
        ""enabled"": true,
        ""mode"": ""execute"",
        ""instructions"": ""string"",
        ""maxIterations"": 0,
        ""temperature"": 0.0,
        ""context"": {{}}
      }},
      ""manualApproval"": {{
        ""enabled"": true,
        ""comment"": ""string""
      }},
      ""errorHandling"": {{
        ""strategy"": ""Abort"",
        ""triggerPolicy"": {{ ""taskName"": ""string"" }},
        ""retryPolicy"": {{
          ""maxRetries"": 3,
          ""backoffStrategy"": ""Exponential"",
          ""initialDelay"": 1000,
          ""maxDelay"": 60000,
          ""backoffCoefficient"": 2.0
        }}
      }},
      ""runOnFailureOf"": [""string""]
    }}
  ]
}}

---
REQUIREMENTS & RULES (CRITICAL):

1. VALID JSON ONLY — no comments, markdown, or explanation.
2. Tasks array MUST NOT be empty.
3. All names MUST be unique and use PascalCase.
4. Plugin names MUST match: ""FlowSynx.<Category>.<Name>"".
5. Plugin versions MUST use full semantic versioning: ""X.Y.Z"".
6. Use ONLY plugins available in:
   - pluginsInfo
7. Do NOT invent plugins or capabilities.
8. Ensure a valid DAG:
   - No cycles.
   - All dependencies refer to existing tasks.
9. Prefer parallel tasks where they are independent.
10. Variables:
    - Only define variables that tasks actually use.
    - When used in parameters, the format MUST be:
      $[Variables('VariableName')]
11. Include all fields shown in the schema. Empty arrays = [] and empty objects = {{}}.

---
Before producing the final JSON, validate in your reasoning:
- All dependencies exist and no cycles are present.
- No undefined plugin or attribute is used.
- Output matches schema exactly.
- Plugin names + versions match the provided registry.

Finally: OUTPUT ONLY THE JSON OBJECT.
";
    }

    public async Task<AgentExecutionResult> ExecuteAgenticTaskAsync(
        AgentExecutionContext context,
        AgentConfiguration config,
        CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Agent executing task '{TaskName}' in mode '{Mode}'", context.TaskName, config.Mode);

        var systemPrompt = BuildAgentSystemPrompt(config.Mode);
        var userPrompt = BuildAgentUserPrompt(context, config);

        var result = new AgentExecutionResult { Steps = new List<string>() };

        for (int iteration = 0; iteration < config.MaxIterations; iteration++)
        {
            result.Steps.Add($"Iteration {iteration + 1}: Reasoning...");

            var body = new
            {
                messages = new object[]
                {
                    new { role = SystemRole, content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = config.Temperature,
                response_format = new { type = JsonObjectResponseFormat }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post,
                $"{_config.Endpoint}/openai/deployments/{_config.Deployment}/chat/completions?api-version=2024-08-01-preview");
            req.Headers.Add(ApiKeyHeaderName, _config.ApiKey);

            var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
            req.Content = new StringContent(jsonBody, Encoding.UTF8, ApplicationJsonMediaType);

            using var res = await _http.SendAsync(req, cancellationToken);
            res.EnsureSuccessStatusCode();

            var responseContent = await res.Content.ReadAsStringAsync(cancellationToken);
            var doc = JObject.Parse(responseContent);
            var content = doc[ChoicesJsonPath]?[0]?[MessageJsonPath]?[ContentJsonPath]?.ToString();

            if (string.IsNullOrWhiteSpace(content))
            {
                result.ErrorMessage = "Agent returned empty response";
                return result;
            }

            // Parse agent response
            var agentResponse = JsonConvert.DeserializeObject<AgentResponse>(content, _jsonSettings);
            if (agentResponse == null)
            {
                result.ErrorMessage = "Failed to parse agent response";
                return result;
            }

            result.Reasoning = agentResponse.Reasoning;
            result.Steps.Add($"Reasoning: {agentResponse.Reasoning}");

            // Check if agent completed the task
            if (agentResponse.Status == "completed")
            {
                result.Success = true;
                result.Output = agentResponse.Output;
                result.Metadata = agentResponse.Metadata;
                result.Steps.Add("Task completed successfully");
                break;
            }

            // If agent needs more iterations, update context with new information
            if (agentResponse.Status == "continue" && iteration < config.MaxIterations - 1)
            {
                userPrompt = AppendIterationContext(userPrompt, agentResponse);
                result.Steps.Add($"Continuing with updated context: {agentResponse.NextAction}");
            }
            else
            {
                result.ErrorMessage = $"Max iterations reached without completion";
                break;
            }
        }

        return result;
    }

    public async Task<string> PlanTaskExecutionAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken)
    {
        var prompt = $@"
Task: {context.TaskName}
Task Type: {context.TaskType}
Description: {context.TaskDescription ?? "N/A"}
Parameters: {JsonConvert.SerializeObject(context.TaskParameters, _jsonSettings)}

Create a detailed execution plan with steps, considerations, and potential issues.
Output as JSON with structure: {{ ""steps"": [...], ""considerations"": [...], ""risks"": [...] }}
";

        var body = new
        {
            messages = new[]
            {
                new { role = SystemRole, content = "You are a workflow planning assistant. Output only JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            response_format = new { type = JsonObjectResponseFormat }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{_config.Endpoint}/openai/deployments/{_config.Deployment}/chat/completions?api-version=2024-08-01-preview");
        req.Headers.Add(ApiKeyHeaderName, _config.ApiKey);

        var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
        req.Content = new StringContent(jsonBody, Encoding.UTF8, ApplicationJsonMediaType);

        using var res = await _http.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var responseContent = await res.Content.ReadAsStringAsync(cancellationToken);
        var doc = JObject.Parse(responseContent);
        return doc[ChoicesJsonPath]?[0]?[MessageJsonPath]?[ContentJsonPath]?.ToString() ?? "{}";
    }

    public async Task<(bool IsValid, string? ValidationMessage)> ValidateTaskAsync(
        AgentExecutionContext context,
        object? output,
        CancellationToken cancellationToken)
    {
        var prompt = $@"
Task: {context.TaskName}
Expected behavior: {context.UserInstructions ?? context.TaskDescription ?? "N/A"}
Parameters: {JsonConvert.SerializeObject(context.TaskParameters, _jsonSettings)}
Actual output: {JsonConvert.SerializeObject(output, _jsonSettings)}
Expectations : {JsonConvert.SerializeObject(context.AdditionalContext, _jsonSettings)}

Validate if the output matches expectations and task requirements.
Return JSON: {{ ""isValid"": true/false, ""message"": ""explanation"" }}
";

        var body = new
        {
            messages = new[]
            {
                new { role = SystemRole, content = "You are a validation assistant. Output only JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            response_format = new { type = JsonObjectResponseFormat }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{_config.Endpoint}/openai/deployments/{_config.Deployment}/chat/completions?api-version=2024-08-01-preview");
        req.Headers.Add(ApiKeyHeaderName, _config.ApiKey);

        var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
        req.Content = new StringContent(jsonBody, Encoding.UTF8, ApplicationJsonMediaType);

        using var res = await _http.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var responseContent = await res.Content.ReadAsStringAsync(cancellationToken);
        var doc = JObject.Parse(responseContent);
        var content = doc[ChoicesJsonPath]?[0]?[MessageJsonPath]?[ContentJsonPath]?.ToString();

        if (string.IsNullOrWhiteSpace(content))
            return (false, "Validation failed: empty response");

        var validation = JsonConvert.DeserializeObject<ValidationResponse>(content, _jsonSettings);
        return (validation?.IsValid ?? false, validation?.Message);
    }

    private static string BuildAgentSystemPrompt(string mode)
    {
        return mode switch
        {
            "execute" => @"You are an AI agent executing workflow tasks. 
Analyze the task, reason about the best approach, and generate appropriate output.
Always respond in JSON format with: { ""status"": ""completed""|""continue"", ""reasoning"": ""..."", ""output"": {...}, ""nextAction"": ""..."", ""metadata"": {...} }",

            "plan" => @"You are a planning agent. Break down tasks into actionable steps.
Output JSON: { ""status"": ""completed"", ""reasoning"": ""..."", ""output"": { ""steps"": [...] }, ""metadata"": {...} }",

            "validate" => @"You are a validation agent. Check if task parameters and outputs are correct.
Output JSON: { ""status"": ""completed"", ""reasoning"": ""..."", ""output"": { ""isValid"": true/false, ""issues"": [...] }, ""metadata"": {...} }",

            "assist" => @"You are an assistant agent. Provide guidance and suggestions for task execution.
Output JSON: { ""status"": ""completed"", ""reasoning"": ""..."", ""output"": { ""suggestions"": [...] }, ""metadata"": {...} }",

            _ => "You are an AI agent. Respond in JSON format."
        };
    }

    private static string BuildAgentUserPrompt(AgentExecutionContext context, AgentConfiguration config)
    {
        return $@"
Task Name: {context.TaskName}
Task Type: {context.TaskType}
Description: {context.TaskDescription ?? "N/A"}
Parameters: {JsonConvert.SerializeObject(context.TaskParameters)}

{(context.WorkflowVariables?.Any() == true ? $"Workflow Variables: {JsonConvert.SerializeObject(context.WorkflowVariables)}" : "")}
{(context.PreviousTaskOutputs?.Any() == true ? $"Previous Task Outputs: {JsonConvert.SerializeObject(context.PreviousTaskOutputs)}" : "")}
{(!string.IsNullOrWhiteSpace(config.Instructions) ? $"Special Instructions: {config.Instructions}" : "")}
{(context.AdditionalContext?.Any() == true ? $"Additional Context: {JsonConvert.SerializeObject(context.AdditionalContext)}" : "")}

Execute this task using mode: {config.Mode}
";
    }

    private static string AppendIterationContext(string originalPrompt, AgentResponse response)
    {
        return $@"{originalPrompt}

Previous iteration reasoning: {response.Reasoning}
Next action: {response.NextAction}
Continue reasoning and execution.
";
    }
}

public class AgentResponse
{
    public string Status { get; set; } = "continue";
    public string? Reasoning { get; set; }
    public object? Output { get; set; }
    public string? NextAction { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class ValidationResponse
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
}