using EnsureThat;
using FlowSync.Core.Configuration;
using FlowSync.Core.Exceptions;
using FlowSync.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace FlowSync.Core.FileSystem.Parers.RemotePath;

internal class RemotePathParser : IRemotePathParser
{
    private readonly ILogger<RemotePathParser> _logger;
    private readonly IConfigurationManager _configurationManager;
    private readonly IDeserializer _deserializer;
    private const string ParserSeparator = "::";

    public RemotePathParser(ILogger<RemotePathParser> logger, IConfigurationManager configurationManager, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(configurationManager, nameof(configurationManager));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));
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
                    FileSystemType = "FlowSync.FileSystem/Local",
                    Specifications = null,
                    Path = path
                };
            }

            var fileSystemExist = _configurationManager.IsExist(segments[0]);
            if (!fileSystemExist)
                throw new RemotePathParserException(string.Format(FlowSyncCoreResource.FileSystemRemotePathParserFileSystemNotFoumd, segments[0]));

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