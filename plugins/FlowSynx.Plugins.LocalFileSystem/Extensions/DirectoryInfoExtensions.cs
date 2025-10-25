using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.LocalFileSystem.Models;
using System.Text.RegularExpressions;

namespace FlowSynx.Plugins.LocalFileSystem.Extensions;

internal static class DirectoryInfoExtensions
{
    public static IEnumerable<FileInfo> FindFiles(this DirectoryInfo directoryInfo, IPluginLogger logger, ListParameters listParameters)
    {
        ValidateDirectory(directoryInfo);      

        var searchOption = listParameters.Recurse is true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var regex = CreateRegex(listParameters);
        var files = directoryInfo.EnumerateFiles("*", searchOption);

        var result = new List<FileInfo>();       
        int resultCount = 0;

        foreach (var file in files)
        {
            if(!ShouldIncludeFile(file,regex))
                continue;
            try 
            {            
                result.Add(file);
                resultCount++;

                // Stop once we reach the maxResults
                if (listParameters.MaxResults.HasValue && resultCount >= listParameters.MaxResults)               
                    break;                
            }
            catch (Exception ex) 
            {
                logger.LogError(ex.Message);
            }
        }

        return result;
    }
    private static void ValidateDirectory(DirectoryInfo directoryInfo)
    {
        if (directoryInfo == null)
            throw new ArgumentNullException(nameof(directoryInfo), Resources.TheDirectoryCouldNotBeNull);

        if (!directoryInfo.Exists)
            throw new DirectoryNotFoundException(string.Format(Resources.TheDirectoryDoesNotExist, directoryInfo.FullName));
    }

    private static Regex? CreateRegex(ListParameters listParameters)
    {
        if (string.IsNullOrEmpty(listParameters.Filter))
            return null;           
        
        var regexOptions = listParameters.CaseSensitive == true ? 
            RegexOptions.None : 
            RegexOptions.IgnoreCase;

        return new Regex(listParameters.Filter, regexOptions);
    }
    private static bool ShouldIncludeFile(FileInfo file, Regex? regex)
    {
        if(regex ==null)
            return true;
        
        return regex.IsMatch(file.FullName);        
    }
}
