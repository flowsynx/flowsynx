using FlowSynx.PluginCore;

namespace FlowSynx.Plugins.LocalFileSystem.Services;

public interface ILocalFileManager
{
    Task<object> About(PluginParameters parameters);

    Task Create(PluginParameters parameters);

    Task Write(PluginParameters parameters);

    Task<object> Read(PluginParameters parameters);

    Task Rename(PluginParameters parameters);

    Task Delete(PluginParameters parameters);

    Task<bool> Exist(PluginParameters parameters);

    Task<object> List(PluginParameters parameters);

    //Task Transfer(Context context, CancellationToken cancellationToken);

    //Task ProcessTransfer(Context context, TransferData transferData, TransferKind transferKind, 
    //    CancellationToken cancellationToken);

    //Task<IEnumerable<CompressEntry>> Compress(Context context, CancellationToken cancellationToken);
}