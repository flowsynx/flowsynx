using FlowSynx.Domain;
using FlowSynx.PluginCore.Exceptions;

namespace FlowSynx.Application.PluginHost.Parser;

public static class PluginTypeParser
{
    private const char Separator = ':';
    private const string UpdateSeparator = "->";
    private const string LatestKeyword = "latest";

    public static ParsedPluginType Parse(string input)
    {
        if (!TryParse(input, out var result, out var error))
        {
            var errorMessage = new ErrorMessage((int)ErrorCode.PluginTypeInvalidInput, error ?? "Invalid plugin type input.");
            throw new FlowSynxException(errorMessage);
        }

        return result!;
    }

    public static bool TryParse(string? input, out ParsedPluginType? result, out string? error)
    {
        result = null;
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Plugin type input cannot be null or empty.";
            return false;
        }

        input = input.Trim();

        if (input.Contains(UpdateSeparator, StringComparison.Ordinal))
            return ParseUpdateFormat(input, out result, out error);

        return ParseSimpleFormat(input, out result, out error);
    }

    private static bool ParseUpdateFormat(string input, out ParsedPluginType? result, out string? error)
    {
        result = null;
        error = null;

        var updateSplit = input.Split(UpdateSeparator, StringSplitOptions.TrimEntries);

        if (updateSplit.Length != 2)
        {
            error = "Update format must be <type>:<currentVersion>-><targetVersion>.";
            return false;
        }

        var left = updateSplit[0];
        // Do not normalize empty target to "latest" in update mode; treat empty as error (generic malformed message)
        var targetRaw = updateSplit[1];
        if (string.IsNullOrWhiteSpace(targetRaw))
        {
            error = "Update format must be <type>:<currentVersion>-><targetVersion>.";
            return false;
        }
        var targetVersion = targetRaw.Trim().ToLowerInvariant();

        var typeParts = left.Split(Separator, StringSplitOptions.TrimEntries);

        if (typeParts.Length != 2)
        {
            error = "Update format must be <type>:<currentVersion>-><targetVersion>.";
            return false;
        }

        var type = typeParts[0];
        var currentVersion = NormalizeVersion(typeParts[1]);

        if (string.IsNullOrWhiteSpace(type))
        {
            error = "Plugin type cannot be empty.";
            return false;
        }

        if (!IsValidVersion(currentVersion))
        {
            error = "Current version cannot be empty.";
            return false;
        }

        if (string.Equals(currentVersion, LatestKeyword, StringComparison.OrdinalIgnoreCase))
        {
            error = "\"latest\" is not allowed as current version in update mode.";
            return false;
        }

        result = new ParsedPluginType(type, currentVersion, targetVersion);
        return true;
    }

    private static bool ParseSimpleFormat(string input, out ParsedPluginType? result, out string? error)
    {
        result = null;
        error = null;

        var parts = input.Split(Separator, StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            var type = parts[0];
            if (string.IsNullOrWhiteSpace(type))
            {
                error = "Plugin type cannot be empty.";
                return false;
            }

            result = new ParsedPluginType(type, LatestKeyword, null);
            return true;
        }

        if (parts.Length == 2)
        {
            var type = parts[0];
            var version = NormalizeVersion(parts[1]);

            if (string.IsNullOrWhiteSpace(type))
            {
                error = "Plugin type cannot be empty.";
                return false;
            }

            if (!IsValidVersion(version))
            {
                error = "Version cannot be empty when using <type>:<version> format.";
                return false;
            }

            result = new ParsedPluginType(type, version);
            return true;
        }

        error = "Plugin type format must be <type> or <type>:<version> or <type>:<current>-><target>.";
        return false;
    }

    private static bool IsValidVersion(string? version)
        => !string.IsNullOrWhiteSpace(version);

    private static string NormalizeVersion(string? version)
        => string.IsNullOrWhiteSpace(version) ? LatestKeyword : version.Trim().ToLowerInvariant();
}