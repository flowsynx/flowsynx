using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.LocalFileSystem.Models;
using System.Text.RegularExpressions;

namespace FlowSynx.Plugins.LocalFileSystem.Extensions;

internal static class DirectoryInfoExtensions
{
    public static IEnumerable<FileInfo> FindFiles(this DirectoryInfo directoryInfo, IPluginLogger logger, ListParameters listParameters)
    {
        if (directoryInfo == null)
            throw new ArgumentNullException(nameof(directoryInfo), "DirectoryInfo cannot be null.");

        if (!directoryInfo.Exists)
            throw new DirectoryNotFoundException($"The directory '{directoryInfo.FullName}' does not exist.");

        IEnumerable<FileInfo> files;
        try
        {
            var searchOption = listParameters.Recurse is true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            files = directoryInfo.EnumerateFiles("*", searchOption);
        }
        catch (Exception ex) 
        {
            throw;
        }

        List<FileInfo> result = new List<FileInfo>();
        Regex? regex = null;
        if (!string.IsNullOrEmpty(listParameters.Filter))
        {
            var regexOptions = listParameters.CaseSensitive is true ? RegexOptions.IgnoreCase : RegexOptions.None;
            regex = new Regex(listParameters.Filter, regexOptions);
        }

        foreach (var file in files)
        {
            try 
            {
                int resultCount = 0;
                var isMatched = regex != null && regex.IsMatch(file.FullName);
                if (listParameters.Filter == null || isMatched)
                {
                    result.Add(file);
                    resultCount++;

                    // Stop once we reach the maxResults
                    if (listParameters.MaxResults.HasValue && resultCount >= listParameters.MaxResults)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex) 
            {
                logger.LogError(ex.Message);
                continue; 
            }
        }

        return result;
    }
}
