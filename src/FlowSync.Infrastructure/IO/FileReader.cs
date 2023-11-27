using EnsureThat;
using Microsoft.Extensions.Logging;

namespace FlowSync.Infrastructure.IO;

public class FileReader : IFileReader
{
    private readonly ILogger<FileReader> _logger;

    public FileReader(ILogger<FileReader> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public string Read(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in read data from path '{path}'. Message: {ex.Message}");
            throw;
        }
    }
}
