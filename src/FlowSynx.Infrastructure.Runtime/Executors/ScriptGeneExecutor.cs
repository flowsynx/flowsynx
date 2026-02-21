using FlowSynx.Application.Models;
using FlowSynx.Domain.Activities;
using FlowSynx.Domain.Workflows;
using FlowSynx.Infrastructure.Runtime.Execution;
using IronPython.Hosting;
using Jint;
using Jint.Native;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text.Json;

namespace FlowSynx.Infrastructure.Runtime.Executors;

public class ScriptActivityExecutor : BaseActivityExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    //private readonly IServiceProvider _serviceProvider;

    public ScriptActivityExecutor(
        ILogger<ScriptActivityExecutor> logger,
        IHttpClientFactory httpClientFactory) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        //_serviceProvider = serviceProvider;
    }

    public override bool CanExecute(ExecutableComponent executable)
    {
        return executable.Type == "script" &&
               !string.IsNullOrEmpty(executable.Source);
    }

    public override async Task<object> ExecuteAsync(
        ActivityJson activity,
        ActivityInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var executable = activity.Spec.Executable;
        var scriptContext = PrepareContext(activity, instance, parameters, context);

        // Apply config from executable
        if (executable.Config != null)
        {
            foreach (var kvp in executable.Config)
            {
                scriptContext[$"config_{kvp.Key}"] = kvp.Value;
            }
        }

        try
        {
            return executable.Language.ToLower() switch
            {
                "javascript" => await ExecuteJavaScriptAsync(executable, scriptContext),
                "python" => await ExecutePythonAsync(executable, scriptContext),
                "csharp" => await ExecuteCSharpAsync(executable, scriptContext),
                "powershell" => await ExecutePowerShellAsync(executable, scriptContext),
                _ => throw new NotSupportedException($"Script language '{executable.Language}' is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script execution failed for activity {ActivityName}", activity.Metadata.Name);
            throw new ActivityExecutionException($"Script execution failed: {ex.Message}", ex);
        }
    }

    private Task<object?> ExecuteJavaScriptAsync(
        ExecutableComponent executable,
        Dictionary<string, object> context)
    {
        var engine = new Engine(options =>
        {
            options.AllowClr();
            options.TimeoutInterval(TimeSpan.FromSeconds(30));
        });

        // Inject all context variables as globals (params, context, env_*)
        foreach (var kvp in context)
        {
            engine.SetValue(kvp.Key, kvp.Value);
        }

        if (executable.Environment != null)
        {
            foreach (var env in executable.Environment)
            {
                engine.SetValue($"env_{env.Key}", env.Value);
            }
        }

        // Provide console and fetch utilities
        engine.SetValue("console", new
        {
            log = new Action<object>(msg => _logger.LogInformation("JavaScript: {Message}", msg)),
            error = new Action<object>(msg => _logger.LogError("JavaScript: {Message}", msg)),
            warn = new Action<object>(msg => _logger.LogWarning("JavaScript: {Message}", msg))
        });

        engine.SetValue("fetch", new Func<string, object>(url =>
        {
            using var client = _httpClientFactory.CreateClient();
            var response = client.GetAsync(url).GetAwaiter().GetResult();
            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            return new
            {
                status = (int)response.StatusCode,
                body = content,
                ok = response.IsSuccessStatusCode
            };
        }));

        try
        {
            // Wrap user code in an IIFE that receives params and context as arguments.
            // The user's code should return the desired result.
            var script = $@"
            var __result = (function(params, context) {{
                {executable.Source}
            }})(params, context);
        ";

            engine.Execute(script);

            var jsResult = engine.GetValue("__result");
            return Task.FromResult(ConvertJsValue(jsResult));
        }
        catch (Jint.Runtime.JavaScriptException jsEx)
        {
            throw new ActivityExecutionException(
                $"JavaScript error: {jsEx.Message}",
                jsEx);
        }
    }

    private static object? ConvertJsValue(JsValue value)
    {
        if (value.IsNull() || value.IsUndefined())
            return null;

        if (value.IsBoolean())
            return value.AsBoolean();

        if (value.IsNumber())
            return value.AsNumber();

        if (value.IsString())
            return value.AsString();

        if (value.IsObject())
            return value.ToObject();

        return value.ToString();
    }

    private async Task<object> ExecutePythonAsync(ExecutableComponent executable, Dictionary<string, object> context)
    {
        var engine = IronPython.Hosting.Python.CreateEngine();
        var scope = engine.CreateScope();

        // Set variables in Python scope
        foreach (var kvp in context)
        {
            scope.SetVariable(kvp.Key, kvp.Value);
        }

        // Set environment variables
        if (executable.Environment != null)
        {
            foreach (var env in executable.Environment)
            {
                scope.SetVariable($"env_{env.Key}", env.Value);
            }
        }

        // Add logging to Python
        var log = new
        {
            info = new Action<object>(msg => _logger.LogInformation("Python: {Message}", msg)),
            error = new Action<object>(msg => _logger.LogError("Python: {Message}", msg)),
            warning = new Action<object>(msg => _logger.LogWarning("Python: {Message}", msg))
        };
        scope.SetVariable("log", log);

        // Add requests library if available
        try
        {
            dynamic requests = engine.ImportModule("requests");
            scope.SetVariable("requests", requests);
        }
        catch
        {
            // requests not available
        }

        // Execute Python script
        try
        {
            var scriptSource = engine.CreateScriptSourceFromString(executable.Source);
            var result = scriptSource.Execute(scope);

            // Try to get return value from __result__ or last expression
            if (scope.ContainsVariable("__result__"))
            {
                return scope.GetVariable("__result__");
            }

            return result;
        }
        catch (Microsoft.Scripting.SyntaxErrorException synEx)
        {
            throw new ActivityExecutionException($"Python syntax error: {synEx.Message}", synEx);
        }
        catch (Exception ex)
        {
            throw new ActivityExecutionException($"Python execution error: {ex.Message}", ex);
        }
    }

    private async Task<object> ExecuteCSharpAsync(ExecutableComponent executable, Dictionary<string, object> context)
    {
        var scriptOptions = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,
                typeof(Console).Assembly,
                typeof(Task).Assembly,
                typeof(JsonSerializer).Assembly,
                typeof(Enumerable).Assembly)
            .WithImports(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Text.Json",
                "System.Threading.Tasks");

        // Prepare globals object
        var globals = new ScriptGlobals
        {
            Context = context,
            Logger = _logger,
            Params = context.ContainsKey("params") ? context["params"] as Dictionary<string, object> : null,
            Config = context.ContainsKey("config") ? context["config"] as Dictionary<string, object> : null,
            Environment = executable.Environment ?? new Dictionary<string, string>()
        };

        // Add service provider if available
        var script = CSharpScript.Create<object>(
            executable.Source,
            scriptOptions,
            typeof(ScriptGlobals));

        try
        {
            script.Compile();
            var state = await script.RunAsync(globals);

            if (state.Exception != null)
            {
                throw state.Exception;
            }

            return state.ReturnValue;
        }
        catch (CompilationErrorException compEx)
        {
            throw new ActivityExecutionException($"C# compilation error: {string.Join("\n", compEx.Diagnostics)}", compEx);
        }
    }

    private async Task<object> ExecutePowerShellAsync(ExecutableComponent executable, Dictionary<string, object> context)
    {
        using var runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();

        using var powershell = PowerShell.Create();
        powershell.Runspace = runspace;

        // Add context variables
        foreach (var kvp in context)
        {
            powershell.Runspace.SessionStateProxy.SetVariable(kvp.Key, kvp.Value);
        }

        // Add environment variables (as $env:KEY)
        if (executable.Environment != null)
        {
            foreach (var env in executable.Environment)
            {
                powershell.Runspace.SessionStateProxy.SetVariable($"env:{env.Key}", env.Value);
            }
        }

        // Configure error preference and add the main script
        powershell.AddScript("$ErrorActionPreference = 'Stop'");
        powershell.AddScript(executable.Source);

        // Collection to capture output as it arrives
        var output = new PSDataCollection<PSObject>();
        output.DataAdded += (sender, e) =>
        {
            var data = output[e.Index];
            _logger.LogDebug("PowerShell output: {Output}", data.ToString());
        };

        try
        {
            // Begin invocation with the output collection
            var asyncResult = powershell.BeginInvoke(output);

            // Wait for completion using the non‑generic EndInvoke
            var results = await Task.Factory.FromAsync(
                asyncResult,
                ar => powershell.EndInvoke(ar));

            if (powershell.HadErrors)
            {
                var errors = powershell.Streams.Error.ReadAll();
                throw new AggregateException(
                    errors.Select(e => new Exception($"PowerShell error: {e.Exception?.Message ?? e.ToString()}")));
            }

            // Unwrap PSObject to underlying .NET objects
            var resultList = results.ToList(); // results is PSDataCollection<PSObject>
            if (resultList.Count == 0)
                return null;
            if (resultList.Count == 1)
                return UnwrapPSObject(resultList[0]);
            return resultList.Select(UnwrapPSObject).ToArray();
        }
        finally
        {
            powershell.Dispose();
            // runspace is disposed automatically by the 'using' statement
        }
    }

    private object UnwrapPSObject(PSObject psObject)
    {
        return psObject?.BaseObject;
    }

    private object ConvertPowerShellObject(PSObject psObject)
    {
        var baseObject = psObject.BaseObject;

        if (baseObject == null)
            return null;

        if (baseObject is string || baseObject is int || baseObject is bool ||
            baseObject is double || baseObject is decimal || baseObject is DateTime)
        {
            return baseObject;
        }

        if (baseObject is Hashtable hashtable)
        {
            var dict = new Dictionary<string, object>();
            foreach (DictionaryEntry entry in hashtable)
            {
                dict[entry.Key.ToString()] = ConvertPowerShellObject(PSObject.AsPSObject(entry.Value));
            }
            return dict;
        }

        if (baseObject is Array array)
        {
            var list = new List<object>();
            foreach (var item in array)
            {
                list.Add(ConvertPowerShellObject(PSObject.AsPSObject(item)));
            }
            return list;
        }

        return baseObject.ToString();
    }
}

public class ScriptGlobals
{
    public Dictionary<string, object> Context { get; set; }
    public ILogger Logger { get; set; }
    public Dictionary<string, object> Params { get; set; }
    public Dictionary<string, object> Config { get; set; }
    public Dictionary<string, string> Environment { get; set; }
}

public class ActivityExecutionException : Exception
{
    public ActivityExecutionException(string message) : base(message) { }
    public ActivityExecutionException(string message, Exception innerException) : base(message, innerException) { }
}