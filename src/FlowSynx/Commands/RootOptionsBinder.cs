using System.CommandLine.Binding;
using System.CommandLine;
using FlowSynx.Logging;

namespace FlowSynx.Commands;

public class RootOptionsBinder : BinderBase<RootCommandOptions>
{
    private readonly Option<string> _configFile;
    private readonly Option<bool> _enableHealthCheck;
    private readonly Option<bool> _enableLog;
    private readonly Option<LoggingLevel> _logLevel;
    private readonly Option<string?> _logFile;
    private readonly Option<bool> _openApi;

    public RootOptionsBinder(Option<string> configFile, Option<bool> enableHealthCheck, 
        Option<bool> enableLog, Option<LoggingLevel> logLevel, Option<string?> logFile, Option<bool> openApi)
    {
        _configFile = configFile;
        _enableHealthCheck = enableHealthCheck;
        _enableLog = enableLog;
        _logLevel = logLevel;
        _logFile = logFile;
        _openApi = openApi;
    }

    protected override RootCommandOptions GetBoundValue(BindingContext bindingContext)
    {
        return new RootCommandOptions
        {
            ConfigFile = bindingContext.ParseResult.GetValueForOption(_configFile) ?? string.Empty,
            EnableHealthCheck = bindingContext.ParseResult.GetValueForOption(_enableHealthCheck),
            EnableLog = bindingContext.ParseResult.GetValueForOption(_enableLog),
            LogLevel = bindingContext.ParseResult.GetValueForOption(_logLevel),
            LogFile = bindingContext.ParseResult.GetValueForOption(_logFile) ?? string.Empty,
            OpenApi = bindingContext.ParseResult.GetValueForOption(_openApi),
        };
    }
}