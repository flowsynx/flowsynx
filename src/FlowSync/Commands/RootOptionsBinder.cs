using System.CommandLine.Binding;
using System.CommandLine;
using FlowSync.Enums;
using FlowSync.Core.Serialization;

namespace FlowSync.Commands;

public class RootOptionsBinder : BinderBase<RootCommandOptions>
{
    private readonly Option<int> _port;
    private readonly Option<string> _config;
    private readonly Option<bool> _enableHealthCheck;
    private readonly Option<bool> _enableLog;
    private readonly Option<AppLogLevel> _logLevel;
    private readonly Option<int> _retry;

    public RootOptionsBinder(Option<int> port, Option<string> config, Option<bool> enableHealthCheck, 
        Option<bool> enableLog, Option<AppLogLevel> logLevel, Option<int> retry)
    {
        _port = port;
        _config = config;
        _enableHealthCheck = enableHealthCheck;
        _enableLog = enableLog;
        _logLevel = logLevel;
        _retry = retry;
    }

    protected override RootCommandOptions GetBoundValue(BindingContext bindingContext)
    {
        var serializer = bindingContext.GetService<ISerializer>();
        return new RootCommandOptions
        {
            Port = bindingContext.ParseResult.GetValueForOption(_port),
            Config = bindingContext.ParseResult.GetValueForOption(_config) ?? string.Empty,
            EnableHealthCheck = bindingContext.ParseResult.GetValueForOption(_enableHealthCheck),
            EnableLog = bindingContext.ParseResult.GetValueForOption(_enableLog),
            AppLogLevel = bindingContext.ParseResult.GetValueForOption(_logLevel),
            Retry = bindingContext.ParseResult.GetValueForOption(_retry)
        };
    }
}