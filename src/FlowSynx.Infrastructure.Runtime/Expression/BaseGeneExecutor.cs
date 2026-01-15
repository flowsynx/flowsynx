using FlowSynx.Application.Core.Services;
using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genes;
using Microsoft.Extensions.Logging;
using System.Dynamic;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Runtime.Expression;

public abstract class BaseGeneExecutor : IGeneExecutor
{
    protected readonly ILogger _logger;

    protected BaseGeneExecutor(ILogger logger)
    {
        _logger = logger;
    }

    public abstract Task<object> ExecuteAsync(
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context);

    public abstract bool CanExecute(ExecutableComponent executable);

    protected Dictionary<string, object> PrepareContext(
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> externalContext)
    {
        var context = new Dictionary<string, object>
        {
            ["gene"] = new
            {
                id = instance.Id,
                name = gene.Metadata.Name,
                version = gene.Metadata.Version
            },
            ["parameters"] = parameters,
            ["config"] = instance.Config,
            ["blueprint"] = new
            {
                metadata = gene.Metadata,
                spec = new
                {
                    description = gene.Specification.Description,
                    expressionProfile = gene.Specification.ExpressionProfile
                }
            }
        };

        foreach (var kvp in externalContext)
        {
            context[kvp.Key] = kvp.Value;
        }

        return context;
    }
}

public class ScriptGeneExecutor : BaseGeneExecutor
{
    public ScriptGeneExecutor(ILogger<ScriptGeneExecutor> logger) : base(logger) { }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "script";
    }

    public override async Task<object> ExecuteAsync(
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var executable = gene.Specification.Executable;
        var scriptContext = PrepareContext(gene, instance, parameters, context);

        try
        {
            return executable.Language.ToLower() switch
            {
                "javascript" => await ExecuteJavaScriptAsync(executable, scriptContext),
                "python" => await ExecutePythonAsync(executable, scriptContext),
                "powershell" => await ExecutePowerShellAsync(executable, scriptContext),
                _ => throw new NotSupportedException($"Script language '{executable.Language}' is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script execution failed for gene {GeneName}", gene.Metadata.Name);
            throw new Exception($"Script execution failed: {ex.Message}", ex);
        }
    }

    private async Task<object> ExecuteJavaScriptAsync(ExecutableComponent executable, Dictionary<string, object> context)
    {
        _logger.LogInformation("Executing PowerShell script (mocked)");

        await Task.Delay(100);

        return new
        {
            executed = true,
            language = "javascript",
            parameters = context["parameters"],
            timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> ExecutePythonAsync(ExecutableComponent executable, Dictionary<string, object> context)
    {
        // In a real implementation, use IronPython or Python.NET
        // For now, we'll mock it
        _logger.LogInformation("Executing Python script (mocked)");

        await Task.Delay(100); // Simulate execution

        return new
        {
            executed = true,
            language = "python",
            parameters = context["parameters"],
            timestamp = DateTime.UtcNow
        };
    }

    private async Task<object> ExecutePowerShellAsync(ExecutableComponent executable, Dictionary<string, object> context)
    {
        // In a real implementation, use System.Management.Automation
        // For now, we'll mock it
        _logger.LogInformation("Executing PowerShell script (mocked)");

        await Task.Delay(100);

        return new
        {
            executed = true,
            language = "powershell",
            parameters = context["parameters"],
            timestamp = DateTime.UtcNow
        };
    }

    private object ConvertToScriptObject(object value)
    {
        if (value == null) return null;

        if (value is Dictionary<string, object> dict)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object>)expando;

            foreach (var kvp in dict)
            {
                expandoDict[kvp.Key] = ConvertToScriptObject(kvp.Value);
            }

            return expando;
        }
        else if (value is List<object> list)
        {
            var array = new List<object>();
            foreach (var item in list)
            {
                array.Add(ConvertToScriptObject(item));
            }
            return array.ToArray();
        }
        else
        {
            return value;
        }
    }
}

public class AssemblyGeneExecutor : BaseGeneExecutor
{
    public AssemblyGeneExecutor(ILogger<AssemblyGeneExecutor> logger) : base(logger) { }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "assembly";
    }

    public override async Task<object> ExecuteAsync(
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var executable = gene.Specification.Executable;
        var assemblyPath = executable.Assembly;

        if (string.IsNullOrEmpty(assemblyPath))
        {
            throw new Exception("Assembly path is not specified");
        }

        _logger.LogInformation("Loading assembly: {AssemblyPath}", assemblyPath);

        try
        {
            // In a real implementation, use reflection to load and execute
            // For now, we'll mock it
            await Task.Delay(200); // Simulate loading

            var result = new
            {
                assembly = assemblyPath,
                entryPoint = executable.EntryPoint,
                parameters = parameters,
                context = new
                {
                    gene = instance.Id,
                    blueprint = gene.Metadata.Name
                },
                executedAt = DateTime.UtcNow,
                success = true
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute assembly {AssemblyPath}", assemblyPath);
            throw new Exception($"Assembly execution failed: {ex.Message}", ex);
        }
    }
}

public class HttpGeneExecutor : BaseGeneExecutor
{
    private readonly System.Net.Http.HttpClient _httpClient;

    public HttpGeneExecutor(ILogger<HttpGeneExecutor> logger) : base(logger)
    {
        _httpClient = new System.Net.Http.HttpClient();
    }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "http";
    }

    public override async Task<object> ExecuteAsync(
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var http = gene.Specification.Executable.Http;
        if (http == null)
        {
            throw new Exception("HTTP configuration is missing");
        }

        var url = http.Url;
        if (string.IsNullOrEmpty(url))
        {
            throw new Exception("HTTP URL is not specified");
        }

        try
        {
            var request = new System.Net.Http.HttpRequestMessage(
                GetHttpMethod(http.Method),
                url);

            // Add headers
            if (http.Headers != null)
            {
                foreach (var header in http.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Add body
            if (parameters != null && parameters.Count > 0)
            {
                var json = JsonSerializer.Serialize(parameters);
                request.Content = new System.Net.Http.StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json");
            }

            _logger.LogInformation("Making HTTP request to {Url}", url);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // Try to parse as JSON
            try
            {
                return JsonSerializer.Deserialize<object>(responseBody);
            }
            catch
            {
                return responseBody;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP request failed to {Url}", http.Url);
            throw new Exception($"HTTP request failed: {ex.Message}", ex);
        }
    }

    private System.Net.Http.HttpMethod GetHttpMethod(string method)
    {
        return method?.ToUpper() switch
        {
            "GET" => System.Net.Http.HttpMethod.Get,
            "POST" => System.Net.Http.HttpMethod.Post,
            "PUT" => System.Net.Http.HttpMethod.Put,
            "DELETE" => System.Net.Http.HttpMethod.Delete,
            "PATCH" => System.Net.Http.HttpMethod.Patch,
            _ => System.Net.Http.HttpMethod.Post
        };
    }
}

public class ContainerGeneExecutor : BaseGeneExecutor
{
    public ContainerGeneExecutor(ILogger<ContainerGeneExecutor> logger) : base(logger) { }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "container";
    }

    public override async Task<object> ExecuteAsync(
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var container = gene.Specification.Executable.Container;
        if (container == null)
        {
            throw new Exception("Container configuration is missing");
        }

        var image = container.Image;
        if (string.IsNullOrEmpty(image))
        {
            throw new Exception("Container image is not specified");
        }

        _logger.LogInformation("Starting container: {Image}", image);

        // In a real implementation, use Docker.DotNet or Kubernetes client
        // For now, we'll mock it
        await Task.Delay(500); // Simulate container startup

        var result = new
        {
            container = new
            {
                image = image,
                command = container.Command,
                args = container.Args
            },
            parameters = parameters,
            executedAt = DateTime.UtcNow,
            success = true,
            mock = true // Indicate this is a mock execution
        };

        return result;
    }
}

public class GrpcGeneExecutor : BaseGeneExecutor
{
    public GrpcGeneExecutor(ILogger<GrpcGeneExecutor> logger) : base(logger) { }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "grpc";
    }

    public override async Task<object> ExecuteAsync(
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var grpc = gene.Specification.Executable.Grpc;
        if (grpc == null)
        {
            throw new Exception("gRPC configuration is missing");
        }

        _logger.LogInformation("Making gRPC call to {Service}.{Method}", grpc.Service, grpc.Method);

        // In a real implementation, use Grpc.Net.Client
        // For now, we'll mock it
        await Task.Delay(300);

        var result = new
        {
            grpc = new
            {
                service = grpc.Service,
                method = grpc.Method,
                address = grpc.Address
            },
            parameters = parameters,
            executedAt = DateTime.UtcNow,
            success = true
        };

        return result;
    }
}