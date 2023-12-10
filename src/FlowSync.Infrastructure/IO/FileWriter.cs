using EnsureThat;
using FlowSync.Infrastructure.Exceptions;
using Microsoft.Extensions.Logging;

namespace FlowSync.Infrastructure.IO;

public class FileWriter : IFileWriter
{
    private readonly ILogger<FileWriter> _logger;

    public FileWriter(ILogger<FileWriter> logger)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        _logger = logger;
    }

    public bool Write(string path, string contents)
    {
        try
        {
            File.WriteAllTextAsync(path, contents);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error in write data to path '{path}'. Message: {ex.Message}");
            throw new FileWriterException(ex.Message);
        }
    }
}
