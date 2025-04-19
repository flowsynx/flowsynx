using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace FlowSynx.Infrastructure.Logging;

internal static class LogTemplate
{
    internal static string Format(LogMessage logMessage, string template)
    {
        var sbResult = new StringBuilder(template.Length);
        var sbCurrentTerm = new StringBuilder();
        var formatChars = template.ToCharArray();
        var inTerm = false;

        for (var i = 0; i < template.Length; i++)
        {
            if (formatChars[i] == '{')
                inTerm = true;
            else if (formatChars[i] == '}')
            {
                string? valueToAppend;
                if (sbCurrentTerm.ToString() == "NewLine")
                {
                    valueToAppend = System.Environment.NewLine;
                }
                else
                {
                    var propertyInfo = logMessage
                        .GetType()
                        .GetProperty(sbCurrentTerm.ToString(), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    
                    if (propertyInfo == null)
                    {
                        var message = string.Format(Resources.Logging_Invalid_Property, sbCurrentTerm.ToString());
                        throw new FlowSynxException((int)ErrorCode.LoggerTemplateInvalidProperty, message);
                    }

                    var propertyValue = propertyInfo.GetValue(logMessage);

                    if (string.Equals(propertyInfo.Name, "level", StringComparison.OrdinalIgnoreCase))
                        propertyValue = GetShortLogLevel((LogLevel) propertyValue!);

                    valueToAppend = propertyValue is null ? string.Empty : propertyValue.ToString();
                }

                sbResult.Append(valueToAppend);
                sbCurrentTerm.Clear();
                inTerm = false;
            }
            else if (inTerm)
            {
                sbCurrentTerm.Append(formatChars[i]);
            }
            else
                sbResult.Append(formatChars[i]);
        }
        return sbResult.ToString();
    }

    internal static string GetShortLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "FAIL",
            LogLevel.Critical => "CRIT",
            _ => logLevel.ToString().ToUpper()
        };
    }
}