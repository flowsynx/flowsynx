using FlowSync.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSync.Core.FileSystem.Parse.Size;

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
            throw new SizeParserException("The given size is not valid!");
        }

        var lastPos = 0;

        if (size.Contains("B"))
            return ExtractValue(size, "B", ref lastPos);

        if (size.Contains("K"))
            return ExtractValue(size, "K", ref lastPos) * BytesInKibiByte;

        if (size.Contains("M"))
            return ExtractValue(size, "M", ref lastPos) * BytesInMebiByte;

        if (size.Contains("G"))
            return ExtractValue(size, "G", ref lastPos) * BytesInGibiByte;

        if (size.Contains("T"))
            return ExtractValue(size, "T", ref lastPos) * BytesInTebiByte;

        if (size.Contains("P"))
            return ExtractValue(size, "P", ref lastPos) * BytesInPebiByte;

        return 0;
    }

    protected bool HasSuffix(string size)
    {
        return size.Contains("B") || size.Contains("K") ||
               size.Contains("M") || size.Contains("G") ||
               size.Contains("T") || size.Contains("P");
    }

    protected long ExtractValue(string size, string key, ref int position)
    {
        var charLocation = size.IndexOf(key, StringComparison.Ordinal);
        if (charLocation < 0 || charLocation < size.Length - 1)
        {
            _logger.LogWarning($"The value from ({size}) could not be extracted!");
            throw new SizeParserException($"The value from ({size}) could not be extracted!");
        }

        var extractedValue = size.Substring(position, charLocation - position);
        var validValue = long.TryParse(extractedValue, out var val);
        position = charLocation + 1;
        return validValue ? val : 0;
    }
    #endregion
}