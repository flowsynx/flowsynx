using System.CommandLine.Binding;
using System.CommandLine;
using FlowSync.Enums;
using FlowSync.Core.Serialization;

namespace FlowSync.Commands;

public class OptionsBinder : BinderBase<CommandOptions>
{
    private readonly Option<int> _port;
    private readonly Option<string> _config;
    private readonly Option<bool> _enableLog;
    private readonly Option<AppLogLevel> _logLevel;

    public OptionsBinder(Option<int> port, Option<string> config, Option<bool> enableLog, Option<AppLogLevel> logLevel)
    {
        _port = port;
        _config = config;
        _enableLog = enableLog;
        _logLevel = logLevel;
    }

    protected override CommandOptions GetBoundValue(BindingContext bindingContext)
    {
        var serializer = bindingContext.GetService<ISerializer>();
        return new CommandOptions
        {
            Port = bindingContext.ParseResult.GetValueForOption(_port),
            Config = bindingContext.ParseResult.GetValueForOption(_config) ?? string.Empty,
            EnableLog = bindingContext.ParseResult.GetValueForOption(_enableLog),
            AppLogLevel = bindingContext.ParseResult.GetValueForOption(_logLevel)
        };
    }
}