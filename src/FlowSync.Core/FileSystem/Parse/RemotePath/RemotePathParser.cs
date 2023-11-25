using FlowSync.Core.Exceptions;
using FlowSync.Core.Serialization;
using FlowSync.Core.Services;
using Microsoft.Extensions.Logging;

namespace FlowSync.Core.FileSystem.Parse.RemotePath;

internal class RemotePathParser : IRemotePathParser
{
    private readonly ILogger<FileSystemService> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly IDeserializer _deserializer;
    private const string ParserSeparator = "::";

    public RemotePathParser(ILogger<FileSystemService> logger, IConfigurationManager configurationManager, IDeserializer deserializer)
    {
        _logger = logger;
        _configurationManager = configurationManager;
        _deserializer = deserializer;
    }

    public RemotePathResult Parse(string path)
    {
        try
        {
            var segments = path.Split(ParserSeparator);
            if (segments.Length != 2)
            {
                return new RemotePathResult
                {
                    FileSystemName = "Local",
                    FileSystemType = "LocalFileSystem",
                    Specifications = null,
                    Path = path
                };
            }

            var fileSystemExist = _configurationManager.IsExist(segments[0]);
            if (!fileSystemExist)
                throw new RemotePathParserException($"{segments[0]} FileSystem not found!");

            var fileSystem = _configurationManager.GetSetting(segments[0]);

            var specifications = new Dictionary<string, object>();
            if (fileSystem.Specifications != null)
                specifications = _deserializer.Deserialize<Dictionary<string, object>>(fileSystem.Specifications.ToString());

            return new RemotePathResult
            {
                FileSystemName = fileSystem.Name,
                FileSystemType = fileSystem.Type,
                Specifications = specifications,
                Path = segments[1]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw;
        }
    }
}