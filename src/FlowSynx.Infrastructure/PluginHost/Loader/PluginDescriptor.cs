using System.Reflection;

namespace FlowSynx.Infrastructure.PluginHost.Loader;

public sealed class PluginDescriptor
{
    public string Id { get; }
    public string Version { get; }
    public string EntryDllPath { get; }
    public string DirectoryPath { get; }

    public PluginDescriptor(string entryDllPath)
    {
        if (string.IsNullOrWhiteSpace(entryDllPath))
            throw new ArgumentNullException(nameof(entryDllPath));

        if (!File.Exists(entryDllPath))
            throw new FileNotFoundException("Plugin dll not found.", entryDllPath);

        EntryDllPath = Path.GetFullPath(entryDllPath);
        DirectoryPath = Path.GetDirectoryName(EntryDllPath)!;

        // Basic descriptor data (can be enhanced by reading plugin manifest)
        Id = Path.GetFileNameWithoutExtension(EntryDllPath);

        try
        {
            var an = AssemblyName.GetAssemblyName(EntryDllPath);
            Version = an.Version?.ToString() ?? "0.0.0.0";
        }
        catch
        {
            Version = "0.0.0.0";
        }
    }
}