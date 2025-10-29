using FlowSynx.Application.Localizations;
using FlowSynx.Application.Models;
using FlowSynx.PluginCore.Exceptions;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace FlowSynx.Infrastructure.Logging;

internal static class LogTemplate
{
    /// <summary>
    /// Formats the provided template by replacing FlowSynx log tokens with <see cref="LogMessage"/> values.
    /// </summary>
    /// <param name="logMessage">The structured log payload supplying token values.</param>
    /// <param name="template">The format template containing brace-delimited tokens.</param>
    /// <returns>The fully rendered log line.</returns>
    internal static string Format(LogMessage logMessage, string template)
    {
        var sbResult = new StringBuilder(template.Length);
        var sbCurrentTerm = new StringBuilder();
        var inTerm = false;

        foreach (var character in template)
        {
            if (HandleOpeningBrace(character, ref inTerm))
            {
                continue;
            }

            if (HandleClosingBrace(character, ref inTerm, sbCurrentTerm, sbResult, logMessage))
            {
                continue;
            }

            AppendCharacter(character, inTerm, sbCurrentTerm, sbResult);
        }

        return sbResult.ToString();
    }

    /// <summary>
    /// Detects the start of a token in the template.
    /// </summary>
    /// <param name="character">Current template character.</param>
    /// <param name="inTerm">Template state indicating if we are inside a token.</param>
    /// <returns>True when the character marks the start of a token and should not be appended.</returns>
    private static bool HandleOpeningBrace(char character, ref bool inTerm)
    {
        if (character != '{')
        {
            return false;
        }

        inTerm = true;
        return true;
    }

    /// <summary>
    /// Finalises a token and writes the resolved value into the result buffer.
    /// </summary>
    /// <param name="character">Current template character.</param>
    /// <param name="inTerm">Template state indicating if we are inside a token.</param>
    /// <param name="sbCurrentTerm">Buffer accumulating token characters.</param>
    /// <param name="sbResult">Rendered template output.</param>
    /// <param name="logMessage">Message that supplies values for tokens.</param>
    /// <returns>True when the character closes a token and processing is complete.</returns>
    private static bool HandleClosingBrace(
        char character,
        ref bool inTerm,
        StringBuilder sbCurrentTerm,
        StringBuilder sbResult,
        LogMessage logMessage)
    {
        if (character != '}')
        {
            return false;
        }

        var term = sbCurrentTerm.ToString();
        var valueToAppend = GetValueForTerm(term, logMessage);

        sbResult.Append(valueToAppend);
        sbCurrentTerm.Clear();
        inTerm = false;
        return true;
    }

    /// <summary>
    /// Routes the current character into the appropriate buffer based on template state.
    /// </summary>
    private static void AppendCharacter(
        char character,
        bool inTerm,
        StringBuilder sbCurrentTerm,
        StringBuilder sbResult)
    {
        if (inTerm)
        {
            sbCurrentTerm.Append(character);
        }
        else
        {
            sbResult.Append(character);
        }
    }

    /// <summary>
    /// Resolves a token name to a value sourced from the provided <see cref="LogMessage"/>.
    /// </summary>
    /// <param name="term">The token extracted from the template.</param>
    /// <param name="logMessage">Message that supplies values for tokens.</param>
    /// <returns>The resolved value, or an empty string when the value is null.</returns>
    /// <exception cref="FlowSynxException">Thrown when the token does not map to a public log message property.</exception>
    private static string GetValueForTerm(string term, LogMessage logMessage)
    {
        if (string.Equals(term, "NewLine", StringComparison.OrdinalIgnoreCase))
        {
            return Environment.NewLine;
        }

        var propertyInfo = logMessage
            .GetType()
            .GetProperty(term, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (propertyInfo == null)
        {
            var message = Localization.Get("Logging_Invalid_Property", term);
            throw new FlowSynxException((int)ErrorCode.LoggerTemplateInvalidProperty, message);
        }

        var propertyValue = propertyInfo.GetValue(logMessage);

        if (string.Equals(propertyInfo.Name, "level", StringComparison.OrdinalIgnoreCase))
        {
            propertyValue = GetShortLogLevel((LogLevel)propertyValue!);
        }

        return propertyValue?.ToString() ?? string.Empty;
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
