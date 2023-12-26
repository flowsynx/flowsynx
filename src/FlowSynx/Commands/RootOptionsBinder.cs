using System.CommandLine.Binding;
using System.CommandLine;

namespace FlowSynx.Commands;

public class RootOptionsBinder : BinderBase<RootCommandOptions>
{
    private readonly Option<string> _configFile;
    private readonly Option<bool> _enableHealthCheck;
    private readonly Option<bool> _enableLog;
    private readonly Option<AppLogLevel> _logLevel;
    private readonly Option<int> _retry;

    public RootOptionsBinder(Option<string> configFile, Option<bool> enableHealthCheck, 
        Option<bool> enableLog, Option<AppLogLevel> logLevel, Option<int> retry)
    {
        _configFile = configFile;
        _enableHealthCheck = enableHealthCheck;
        _enableLog = enableLog;
        _logLevel = logLevel;
        _retry = retry;
    }

    protected override RootCommandOptions GetBoundValue(BindingContext bindingContext)
    {
        return new RootCommandOptions
        {
            ConfigFile = bindingContext.ParseResult.GetValueForOption(_configFile) ?? string.Empty,
            EnableHealthCheck = bindingContext.ParseResult.GetValueForOption(_enableHealthCheck),
            EnableLog = bindingContext.ParseResult.GetValueForOption(_enableLog),
            LogLevel = bindingContext.ParseResult.GetValueForOption(_logLevel),
            Retry = bindingContext.ParseResult.GetValueForOption(_retry)
        };
    }
}