using MediatR;
using Microsoft.Extensions.Logging;
using EnsureThat;
using FlowSynx.Abstractions;
using FlowSynx.IO.Serialization;
using FlowSynx.Core.Services;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace FlowSynx.Core.Features.Workflow.Query;

internal class WorkflowHandler : IRequestHandler<WorkflowRequest, Result<object?>>
{
    private readonly ILogger<WorkflowHandler> _logger;
    private readonly IDeserializer _deserializer;
    private readonly IWorkflowExecutor _workflowExecutor;

    public WorkflowHandler(ILogger<WorkflowHandler> logger, IDeserializer deserializer, 
        IWorkflowExecutor workflowExecutor)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
        _deserializer = deserializer;
        _workflowExecutor = workflowExecutor;
    }

    public async Task<Result<object?>> Handle(WorkflowRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var variablesJson = ExtractJson(request.WorkflowTemplate, "variables", JsonObjectType.JsonObject);
            var workflowVariables = _deserializer.Deserialize<WorkflowVariables>(variablesJson);

            var variablesJObject = JObject.Parse(variablesJson);
            ConvertBooleansToLowercase(variablesJObject);

            var pipelines = ExtractJson(request.WorkflowTemplate, "pipelines", JsonObjectType.JsonArray);
            var templateEngine = new TemplateEngine(variablesJObject);

            templateEngine.RegisterFunction("uppercase", new StringTransformationFunction((value, _) => value.ToString().ToUpper()));

            templateEngine.RegisterFunction("add", new MathTransformationFunction((value, args) =>
            {
                var num = Convert.ToDouble(value);
                var addValue = Convert.ToDouble(args[0]);
                return num + addValue;
            }, 1));

            templateEngine.RegisterFunction("concat", new StringTransformationFunction((value, args) => value + string.Join("", args), 1, int.MaxValue));

            var rendered = templateEngine.Render(pipelines);

            var workflowPipelines = _deserializer.Deserialize<WorkflowPipelines>(rendered);
            var workflowValidator = new WorkflowDagValidator(workflowPipelines);

            var missingDependencies = workflowValidator.AllDependenciesExist();

            if (missingDependencies.Any())
            {
                var sb = new StringBuilder();
                sb.AppendLine("Invalid workflow: missing dependencies.. There are list of missing dependencies:");
                sb.AppendLine(string.Join(",", missingDependencies));

                throw new Exception(sb.ToString());
            }

            var validation = workflowValidator.Check();
            if (validation.Cyclic)
            {
                var sb = new StringBuilder();
                sb.AppendLine("The workflow has cyclic dependencies. Please resolve them and try again!. There are Cyclic:");
                sb.AppendLine(string.Join("->", validation.CyclicNodes));

                throw new Exception(sb.ToString());
            }

            var executionDefinition = new WorkflowExecutionDefinition
            {
                WorkflowPipelines = workflowPipelines,
                WorkflowVariables = workflowVariables,
                DegreeOfParallelism = 3
            };

            var result = await _workflowExecutor.ExecuteAsync(executionDefinition, cancellationToken);
            templateEngine.RegisterResults(result);

            var outputsJson = ExtractJson(request.WorkflowTemplate, "outputs", JsonObjectType.JsonObject);

            var outputRendered = templateEngine.Render(outputsJson);

            var jObject = JsonConvert.DeserializeObject<JToken>(outputRendered);
            var workflowOutput = DeserializeRecursive(jObject);

            return await Result<object?>.SuccessAsync(workflowOutput);
        }
        catch (Exception ex)
        {
            return await Result<object?>.FailAsync(new List<string> { ex.Message });
        }
    }

    private object? DeserializeRecursive(JToken? token)
    {
        if (token == null)
            return token;

        switch (token)
        {
            case JObject jObject:
            {
                var dictionary = new Dictionary<string, object?>();
                foreach (var property in jObject.Properties())
                {
                    dictionary[property.Name] = DeserializeRecursive(property.Value);
                }

                return dictionary;
            }
            case JArray jArray:
            {
                return jArray.Select(DeserializeRecursive).ToList();
            }
            default:
                return token.ToObject<object>();
        }
    }

    public class JsonObjectType
    {
        public char StartBracket { get; set; }
        public char EndBracket { get; set; }

        public static JsonObjectType JsonObject => new JsonObjectType()
        {
            StartBracket = '{',
            EndBracket = '}'
        };

        public static JsonObjectType JsonArray => new JsonObjectType()
        {
            StartBracket = '[',
            EndBracket = ']'
        };
    }

    private string ExtractJson(string json, string key, JsonObjectType type)
    {
        // Find the start index of the key (e.g., "address":)
        var startIndex = json.IndexOf(key, StringComparison.Ordinal);
        if (startIndex == -1)
            return string.Empty; // Key not found

        // Find the start of the nested object (the opening brace)
        startIndex = json.IndexOf(type.StartBracket, startIndex);

        // Find the closing brace of the nested object
        int endIndex = FindClosingBracket(json, startIndex, type);

        // Extract and return the nested JSON substring
        return json.Substring(startIndex, endIndex - startIndex + 1);
    }

    private int FindClosingBracket(string json, int startIndex, JsonObjectType type)
    {
        var braceCount = 0;
        for (var i = startIndex; i < json.Length; i++)
        {
            if (json[i] == type.StartBracket) braceCount++;
            else if (json[i] == type.EndBracket) braceCount--;

            // If braceCount reaches 0, we found the matching closing brace
            if (braceCount == 0)
                return i;
        }
        return -1; // Closing brace not found
    }

    private void ConvertBooleansToLowercase(JObject jObject)
    {
        foreach (var property in jObject.Properties().ToList())
        {
            if (property.Value.Type == JTokenType.Boolean)
            {
                // Convert the boolean value to lowercase "true" or "false"
                property.Value = property.Value.ToString().ToLower();
            }
            else if (property.Value.Type == JTokenType.Object)
            {
                // Recursively handle nested objects
                ConvertBooleansToLowercase((JObject)property.Value);
            }
            else if (property.Value.Type == JTokenType.Array)
            {
                // Handle nested arrays if needed
                foreach (var item in property.Value)
                {
                    if (item.Type == JTokenType.Boolean)
                    {
                        item.Replace(item.ToString().ToLower());
                    }
                }
            }
        }
    }
}

public class StringTransformationFunction : TransformationFunction
{
    private readonly Func<object?, List<object>, object> _func;
    private readonly int _minArgs;
    private readonly int _maxArgs;

    public StringTransformationFunction(Func<object?, List<object>, object> func, int minArgs = 0, int maxArgs = 0)
    {
        _func = func;
        _minArgs = minArgs;
        _maxArgs = maxArgs;
    }

    public override void ValidateArguments(List<object> arguments)
    {
        if (arguments.Count < _minArgs || arguments.Count > _maxArgs)
        {
            throw new ArgumentException($"This transformation requires between {_minArgs} and {_maxArgs} arguments.");
        }
    }

    public override object Transform(object? value, List<object> arguments)
    {
        return _func(value, arguments);
    }
}

public class MathTransformationFunction : TransformationFunction
{
    private readonly Func<object?, List<object>, object> _func;
    private readonly int _expectedArgs;

    public MathTransformationFunction(Func<object?, List<object>, object> func, int expectedArgs)
    {
        _func = func;
        _expectedArgs = expectedArgs;
    }

    public override void ValidateArguments(List<object> arguments)
    {
        if (arguments.Count != _expectedArgs)
        {
            throw new ArgumentException($"This transformation requires exactly {_expectedArgs} arguments.");
        }
    }

    public override object Transform(object? value, List<object> arguments)
    {
        return _func(value, arguments);
    }
}