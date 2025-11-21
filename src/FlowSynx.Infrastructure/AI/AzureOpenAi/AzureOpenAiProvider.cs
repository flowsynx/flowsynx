using FlowSynx.Application.AI;
using FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace FlowSynx.Infrastructure.AI.AzureOpenAi;

public sealed class AzureOpenAiProvider : IAiProvider, IConfigurableAi
{
    private readonly HttpClient _http;
    private readonly ILogger<AzureOpenAiProvider>? _logger;
    private readonly AzureOpenAiConfiguration _config = new AzureOpenAiConfiguration();
    private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.None
    };

    public AzureOpenAiProvider(
        HttpClient httpClient,
        ILogger<AzureOpenAiProvider> logger)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
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
        
        var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
        req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var responseContent = await res.Content.ReadAsStringAsync(cancellationToken);
        var doc = JObject.Parse(responseContent);

        var json = doc["choices"]?[0]?["message"]?["content"]?.ToString();
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("LLM returned empty JSON.");

        return json!;
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
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = config.Temperature,
                response_format = new { type = "json_object" }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post,
                $"{_config.Endpoint}/openai/deployments/{_config.Deployment}/chat/completions?api-version=2024-08-01-preview");
            req.Headers.Add("api-key", _config.ApiKey);
            
            var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
            req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            using var res = await _http.SendAsync(req, cancellationToken);
            res.EnsureSuccessStatusCode();

            var responseContent = await res.Content.ReadAsStringAsync(cancellationToken);
            var doc = JObject.Parse(responseContent);
            var content = doc["choices"]?[0]?["message"]?["content"]?.ToString();

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
                new { role = "system", content = "You are a workflow planning assistant. Output only JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            response_format = new { type = "json_object" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{_config.Endpoint}/openai/deployments/{_config.Deployment}/chat/completions?api-version=2024-08-01-preview");
        req.Headers.Add("api-key", _config.ApiKey);
        
        var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
        req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var responseContent = await res.Content.ReadAsStringAsync(cancellationToken);
        var doc = JObject.Parse(responseContent);
        return doc["choices"]?[0]?["message"]?["content"]?.ToString() ?? "{}";
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
                new { role = "system", content = "You are a validation assistant. Output only JSON." },
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{_config.Endpoint}/openai/deployments/{_config.Deployment}/chat/completions?api-version=2024-08-01-preview");
        req.Headers.Add("api-key", _config.ApiKey);
        
        var jsonBody = JsonConvert.SerializeObject(body, _jsonSettings);
        req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, cancellationToken);
        res.EnsureSuccessStatusCode();

        var responseContent = await res.Content.ReadAsStringAsync(cancellationToken);
        var doc = JObject.Parse(responseContent);
        var content = doc["choices"]?[0]?["message"]?["content"]?.ToString();

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

    private static string BuildPrompt(string goal, string? capabilitiesJson)
    {
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
    description?: string,
    type: {{ plugin: string, version?: string }},  // plugin type and version
    parameters?: object,
    dependencies?: string[],
    timeout?: number,
    agent?: {{
        enabled: boolean,
        mode?: ""execute""|""plan""|""validate""|""assist"",
        instructions?: string,
        maxIterations?: number,
        temperature?: number,
        context?: object
    }},
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