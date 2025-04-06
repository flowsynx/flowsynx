namespace FlowSynx.Infrastructure.PluginHost;

public interface IPluginChecksumValidator
{
    bool ValidateChecksum(byte[] data, string expectedChecksum);
}