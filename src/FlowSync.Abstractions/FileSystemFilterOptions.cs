using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions.Helpers;

namespace FlowSync.Abstractions;

public class FileSystemFilterOptions
{
    private string _directoryPath = PathHelper.RootDirectoryPath;

    public string DirectoryPath
    {
        get => _directoryPath;
        set => _directoryPath = PathHelper.Normalize(value);
    }

    public FilterItemKind Kind { get; set; } = FilterItemKind.FileAndDirectory;
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? MinimumAge { get; set; }
    public string? MaximumAge { get; set; }
    public string? MinimumSize { get; set; }
    public string? MaximumSize { get; set; }
    public string? Sorting { get; set; }
    public bool CaseSensitive { get; set; } = false;
    public bool Recurse { get; set; } = false;
    public int MaxResults { get; set; } = 10;
    
    public FileSystemFilterOptions Clone()
    {
        return (FileSystemFilterOptions)MemberwiseClone();
    }
}