using EnsureThat;
using FlowSync.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSync.Core.FileSystem.Parers.Size;

internal class SizeParser : ISizeParser
{
    private const long BytesInKibiByte = 1_024;
    private const long BytesInMebiByte = 1_048_576;
    private const long BytesInGibiByte = 1_073_741_824;
    private const long BytesInTebiByte = 1_099_511_627_776;
    private const long BytesInPebiByte = 1_125_899_906_842_624;
    private readonly ILogger<SizeParser> _logger;

    public SizeParser(ILogger<SizeParser> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public long Parse(string size)
    {
        var isNumber = long.TryParse(size, out _);
        if (isNumber)
            size += 'K';

        return ParseSizeWithSuffix(size);
    }

    #region internal Methods
    protected long ParseSizeWithSuffix(string size)
    {
        if (!HasSuffix(size))
        {
            _logger.LogError($"The given size ({size}) is not valid!");
            throw new SizeParserException(FlowSyncCoreResource.FileSystemSizeParserInvalidInput);
        }

        var lastPos = 0;

        if (size.Contains("B") || size.Contains("b"))
            return ExtractValue(size, "B", ref lastPos);

        if (size.Contains("K") || size.Contains("k"))
            return ExtractValue(size, "K", ref lastPos) * BytesInKibiByte;

        if (size.Contains("M") || size.Contains("m"))
            return ExtractValue(size, "M", ref lastPos) * BytesInMebiByte;

        if (size.Contains("G") || size.Contains("g"))
            return ExtractValue(size, "G", ref lastPos) * BytesInGibiByte;

        if (size.Contains("T") || size.Contains("t"))
            return ExtractValue(size, "T", ref lastPos) * BytesInTebiByte;

        if (size.Contains("P") || size.Contains("p"))
            return ExtractValue(size, "P", ref lastPos) * BytesInPebiByte;

        return 0;
    }

    protected bool HasSuffix(string size)
    {
        return size.Contains("B") || size.Contains("b") || size.Contains("K") || size.Contains("K") ||
               size.Contains("M") || size.Contains("m") || size.Contains("G") || size.Contains("g") ||
               size.Contains("T") || size.Contains("t") || size.Contains("P") || size.Contains("p");
    }

    protected long ExtractValue(string size, string key, ref int position)
    {
        var charLocation = size.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (charLocation < 0 || charLocation < size.Length - 1)
        {
            _logger.LogWarning($"The value from ({size}) could not be extracted!");
            throw new SizeParserException(string.Format(FlowSyncCoreResource.FileSystemSizeParserCannotExtractValue, size));
        }

        var extractedValue = size.Substring(position, charLocation - position);
        var validValue = long.TryParse(extractedValue, out var val);
        position = charLocation + 1;
        return validValue ? val : 0;
    }
    #endregion
}