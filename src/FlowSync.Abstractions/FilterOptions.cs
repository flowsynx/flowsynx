using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions.Extensions;
using FlowSync.Abstractions.Helpers;

namespace FlowSync.Abstractions;

public class FilterOptions
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
    
    public bool Add(ICollection<Entity> dest, ICollection<Entity> src)
    {
        if (dest.Count + src.Count < MaxResults)
        {
            dest.AddRange(src);
            return false;
        }

        dest.AddRange(src.Take(MaxResults - dest.Count));
        return true;
    }
    
    public FilterOptions Clone()
    {
        return (FilterOptions)MemberwiseClone();
    }
}