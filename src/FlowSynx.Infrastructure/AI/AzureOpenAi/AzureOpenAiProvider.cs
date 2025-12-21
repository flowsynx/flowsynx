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
                new { role = SystemRole, content = "You are FlowSynx-WorkflowGen." +
                "Generate one valid FlowSynx WorkflowDefinition JSON object." +
                "Use only provided plugins." +
                "Do not invent fields." +
                "Ensure a valid DAG." +
                "Output JSON only." },
                new { role = "user", content = await BuildPromptAsync(goal, capabilitiesJson, pluginsInfo) }
            },
            temperature = 0.2
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
            // Support multiple registries (primary first, others as fallback/merge)
            var registries = (_pluginRegistryConfiguration.Urls?.Count > 0
                    ? _pluginRegistryConfiguration.Urls
                    : new List<string> {})
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Select(u => u.TrimEnd('/') + "/")
                .ToList();

            var allPlugins = new JArray();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // key: type@version

            foreach (var registry in registries)
            {
                var baseUrl = registry + "api/plugins?page=";

                int currentPage = 1;
                int totalPages = 1; // updated after first response

                do
                {
                    var url = baseUrl + currentPage;

                    try
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Get, url);
                        using var res = await _http.SendAsync(req, cancellationToken);

                        if (!res.IsSuccessStatusCode)
                        {
                            _logger?.LogWarning(
                                "Failed to fetch plugins from {Url}. Status: {Status}",
                                url, res.StatusCode);
                            break; // stop paging this registry; continue with next registry
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
                            {
                                var type = p?["type"]?.ToString() ?? "";
                                var version = p?["version"]?.ToString() ?? "";
                                var key = $"{type}:{version}";

                                if (seen.Add(key))
                                    allPlugins.Add(p);
                            }
                        }

                        currentPage++;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error fetching plugins from {Url}", url);
                        break;
                    }
                } while (currentPage <= totalPages);
            }

            // Convert to simplified format (optional)
            var formattedPlugins = new JArray();
            foreach (var plugin in allPlugins)
            {
                var type = plugin["type"]?.ToString() ?? "";
                var version = plugin["version"]?.ToString() ?? "";
                var pluginType = $"{type}:{version}";

                formattedPlugins.Add(new JObject
                {
                    ["type"] = pluginType,
                    ["description"] = plugin["description"]?.ToString() ?? "",
                    ["specifications"] = plugin["specifications"] ?? new JArray(),
                    ["operations"] = plugin["operations"] ?? new JArray()
                });
            }

            return formattedPlugins.ToString(Formatting.None);
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
=== ROLE ===
You are FlowSynx-WorkflowGen, an expert system that generates VALID FlowSynx WorkflowDefinition JSON objects.

You are STRICT, DETERMINISTIC, and PRECISE.
You MUST follow ALL instructions with ZERO deviation.
You MUST NOT invent, infer, normalize, rename, or assume any plugins, operations, parameters, or fields.

You are also acting as a STRICT JSON + SCHEMA VALIDATOR.
Any invalid output is considered a FAILURE.

If you cannot produce a fully valid output that satisfies ALL rules:
- Restart generation internally
- Do NOT output partial JSON
- Do NOT explain
- Output ONLY the final corrected JSON

---

=== INPUT: GOAL ===
{goal}

---

=== INPUT: PLUGINS AVAILABLE IN REGISTRY ===
Use ONLY these plugins, operations, parameters, and versions.
Copy names VERBATIM.
{pluginsInfo}

---

=== OUTPUT FORMAT (ABSOLUTELY CRITICAL) ===
- Output MUST be a SINGLE JSON OBJECT
- NO markdown
- NO comments
- NO explanation
- NO surrounding text
- NO trailing commas
- NO null values anywhere
- Empty arrays = []
- Empty objects = {{}}

---

=== AUTHORITATIVE SCHEMA (MUST MATCH EXACTLY) ===
The schema below is AUTHORITATIVE.
Field names, casing, structure, and presence MUST match exactly.

{{
  ""name"": ""string"",
  ""description"": ""string"",
  ""variables"": {{ ""key"": ""value"" }},
  ""configuration"": {{
    ""degreeOfParallelism"": 0,
    ""timeoutMilliseconds"": 0,
    ""errorHandling"": {{
      ""strategy"": ""Abort"",
      ""triggerPolicy"": {{ ""taskName"": ""string"" }},
      ""retryPolicy"": {{}}
    }}
  }},
  ""tasks"": [
    {{
      ""name"": ""string"",
      ""description"": ""string"",
      ""type"": ""string"",
      ""execution"": {{
        ""operation"": ""string"",
        ""Specification"": {{ ""key"": ""value"" }},
        ""parameters"": {{ ""key"": ""value"" }},
        ""timeoutMilliseconds"": 0,
        ""agent"": {{
          ""enabled"": true,
          ""mode"": ""execute"",
          ""instructions"": ""string"",
          ""maxIterations"": 1,
          ""temperature"": 0.0,
          ""context"": {{}}
        }}
      }},
      ""flowControl"": {{}},
      ""dependencies"": [""string""],
      ""runOnFailureOf"": [""string""],
      ""executionCondition"": {{
        ""expression"": ""string"",
        ""description"": ""string""
      }},
      ""conditionalBranches"": [
        {{
          ""expression"": ""string"",
          ""description"": ""string"",
          ""targetTaskName"": ""string""
        }}
      ],
      ""manualApproval"": {{
        ""enabled"": true,
        ""comment"": ""string""
      }},
      ""errorHandling"": {{
        ""strategy"": ""Abort"",
        ""triggerPolicy"": {{ ""taskName"": ""string"" }},
        ""retryPolicy"": {{}}
      }},
      ""position"": {{ ""x"": 0, ""y"": 0 }}
    }}
  ]
}}

---

=== HARD RULES (NON-NEGOTIABLE) ===

1. VALID JSON ONLY. No trailing commas. No null values.
2. tasks MUST NOT be empty.
3. All names MUST be unique and PascalCase.
4. Task names SHOULD use verb-noun form (e.g., FetchData).
5. Use ONLY plugins and operations from pluginsInfo.
6. Do NOT invent plugins, operations, parameters, or fields.
7. Plugins with incompatible MAJOR versions MUST NOT be mixed.
8. Ensure a valid DAG:
   - No cycles
   - All dependencies exist
   - At least one root task
   - Every task reachable from a root
9. A task MUST NOT depend on more than 10 tasks.
10. Prefer parallel execution when tasks are independent.
11. Variables:
    - Define ONLY variables that are actually used
    - Usage format MUST be: $[Variables('VariableName')]
    - Variables MUST be defined before use
12. execution.operation MUST exist in the referenced plugin.
13. All parameters MUST be consumed by the plugin operation.
14. Specification = static configuration
15. parameters = runtime or variable-based values
16. errorHandling.strategy MUST be one of:
    Retry | Skip | Abort | TriggerTask
17. retryPolicy:
    - MUST be populated ONLY when strategy == Retry
    - MUST be empty {{}} otherwise
    - maxRetries MUST be between 0 and 10
    - backoffStrategy MUST be one of:
      Fixed | Linear | Exponential | Jitter
18. If strategy == Retry:
    - execution.timeoutMilliseconds MUST be > 0
19. If strategy == TriggerTask:
    - triggerPolicy.taskName MUST match an existing task
    - MUST NOT create a cycle
    - Target SHOULD be compensating or notification task
20. If strategy != TriggerTask:
    - triggerPolicy MUST be empty {{}}
21. Task-level errorHandling OVERRIDES workflow-level errorHandling.
22. After retries exhausted:
    - Workflow MUST Abort unless TriggerTask is defined
23. runOnFailureOf MAY reference only direct or transitive dependencies.
24. conditionalBranches:
    - expressions MUST be mutually exclusive
    - targetTaskName MUST exist
    - MUST NOT reference the current task
    - One branch SHOULD act as default (e.g., expression == true)
25. executionCondition expressions:
    - MUST be deterministic
    - MUST be side-effect free
    - MUST handle missing or null variables safely
26. Manual approval tasks:
    - MUST be depended upon by at least one task
27. Agent constraints:
    - temperature MUST be between 0.0 and 0.5
    - maxIterations MUST be between 1 and 10
28. No two tasks SHOULD share the same position (x, y).
29. description fields MUST clearly describe intent.
30. Tasks SHOULD be idempotent or explicitly compensatable.
31. Irreversible tasks SHOULD define a compensating TriggerTask.
32. Workflow timeout MUST be >= sum of task timeouts.
33. Order tasks topologically (execution order).
34. Do NOT rely on implicit defaults — define all behavior explicitly.
35. The workflow MUST be fully deterministic.
36. Output EXACTLY ONE JSON OBJECT and NOTHING ELSE.

---

=== INTERNAL VALIDATION CHECKLIST (DO NOT OUTPUT) ===
- Schema matches exactly
- JSON is valid
- No cycles in DAG
- All dependencies valid
- All plugins and operations exist
- All rules satisfied

FINAL INSTRUCTION:
OUTPUT ONLY THE JSON OBJECT.
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