using FlowSynx.Application.Models;
using FlowSynx.Domain.Chromosomes;
using FlowSynx.Domain.Genes;
using FlowSynx.Infrastructure.Runtime.Expression;
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

public class ScriptGeneExecutor : BaseGeneExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    //private readonly IServiceProvider _serviceProvider;

    public ScriptGeneExecutor(
        ILogger<ScriptGeneExecutor> logger,
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
        GeneJson gene,
        GeneInstance instance,
        Dictionary<string, object> parameters,
        Dictionary<string, object> context)
    {
        var executable = gene.Specification.Executable;
        var scriptContext = PrepareContext(gene, instance, parameters, context);

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
            _logger.LogError(ex, "Script execution failed for gene {GeneName}", gene.Metadata.Name);
            throw new GeneExecutionException($"Script execution failed: {ex.Message}", ex);
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
            var script = $@"
            var __result = (function() {{
                {executable.Source}
            }})();
        ";

            engine.Execute(script);

            var jsResult = engine.GetValue("__result");
            return Task.FromResult(ConvertJsValue(jsResult));
        }
        catch (Jint.Runtime.JavaScriptException jsEx)
        {
            throw new GeneExecutionException(
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
            throw new GeneExecutionException($"Python syntax error: {synEx.Message}", synEx);
        }
        catch (Exception ex)
        {
            throw new GeneExecutionException($"Python execution error: {ex.Message}", ex);
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
            Parameters = context.ContainsKey("parameters") ? context["parameters"] as Dictionary<string, object> : null,
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
            throw new GeneExecutionException($"C# compilation error: {string.Join("\n", compEx.Diagnostics)}", compEx);
        }
    }

    private async Task<object> ExecutePowerShellAsync(ExecutableComponent executable, Dictionary<string, object> context)
    {
        using var runspace = RunspaceFactory.CreateRunspace();
        runspace.Open();

        using var powershell = PowerShell.Create();
        powershell.Runspace = runspace;

        // Add variables to PowerShell
        foreach (var kvp in context)
        {
            powershell.Runspace.SessionStateProxy.SetVariable(kvp.Key, kvp.Value);
        }

        // Add environment variables
        if (executable.Environment != null)
        {
            foreach (var env in executable.Environment)
            {
                powershell.Runspace.SessionStateProxy.SetVariable($"env:{env.Key}", env.Value);
            }
        }

        // Configure PowerShell
        powershell.AddScript("$ErrorActionPreference = 'Stop'");
        powershell.AddScript(executable.Source);

        // Add output collection
        var output = new PSDataCollection<PSObject>();
        output.DataAdded += (sender, e) =>
        {
            var data = output[e.Index];
            _logger.LogDebug("PowerShell output: {Output}", data.ToString());
        };

        try
        {
            var results = await Task.Factory.FromAsync(
                powershell.BeginInvoke<PSObject, PSObject>(null, output),
                powershell.EndInvoke);

            if (powershell.HadErrors)
            {
                var errors = powershell.Streams.Error.ReadAll();
                throw new AggregateException(errors.Select(e =>
                    new Exception($"PowerShell error: {e.Exception?.Message ?? e.ToString()}")));
            }

            // Convert results
            var resultList = results.ToList();
            if (resultList.Count == 0)
            {
                return null;
            }
            else if (resultList.Count == 1)
            {
                return ConvertPowerShellObject(resultList[0]);
            }
            else
            {
                return resultList.Select(ConvertPowerShellObject).ToArray();
            }
        }
        finally
        {
            powershell.Dispose();
            runspace.Close();
        }
    }

    //private JsValue ConvertToJavaScriptValue(Engine engine, object? value)
    //{
    //    if (value == null)
    //        return JsValue.Null;

    //    if (value is JsValue jsValue)
    //        return jsValue;

    //    if (value is Dictionary<string, object> dict)
    //    {
    //        var obj = engine.Object.Construct(Arguments.Empty);

    //        foreach (var kvp in dict)
    //        {
    //            obj.FastAddProperty(
    //                kvp.Key,
    //                ConvertToJavaScriptValue(engine, kvp.Value),
    //                writable: true,
    //                enumerable: true,
    //                configurable: true);
    //        }

    //        return obj;
    //    }

    //    if (value is IList<object> list)
    //    {
    //        var array = engine.Array.Construct(Arguments.Empty);

    //        for (uint i = 0; i < list.Count; i++)
    //        {
    //            array.Set(
    //                i,
    //                ConvertToJavaScriptValue(engine, list[(int)i]),
    //                throwOnError: false);
    //        }

    //        return array;
    //    }

    //    // Fallback for primitives & POCOs
    //    return JsValue.FromObject(engine, value);
    //}

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
    public Dictionary<string, object> Parameters { get; set; }
    public Dictionary<string, object> Config { get; set; }
    public Dictionary<string, string> Environment { get; set; }
}

public class GeneExecutionException : Exception
{
    public GeneExecutionException(string message) : base(message) { }
    public GeneExecutionException(string message, Exception innerException) : base(message, innerException) { }
}