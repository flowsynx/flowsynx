using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.LocalFileSystem.Services;

internal interface ILocalFileManager
{
    Task Create(PluginParameters parameters);
    Task Delete(PluginParameters parameters);
    Task<bool> Exist(PluginParameters parameters);
    Task<IEnumerable<PluginContextData>> List(PluginParameters parameters);
    Task Purge(PluginParameters parameters);
    Task<PluginContextData> Read(PluginParameters parameters);
    Task Rename(PluginParameters parameters);
    Task Write(PluginParameters parameters);
}