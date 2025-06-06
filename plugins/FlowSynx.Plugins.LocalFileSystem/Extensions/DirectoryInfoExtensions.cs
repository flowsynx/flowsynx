﻿using FlowSynx.PluginCore;
using FlowSynx.PluginCore.Extensions;
using FlowSynx.Plugins.LocalFileSystem.Models;
using System.Text.RegularExpressions;

namespace FlowSynx.Plugins.LocalFileSystem.Extensions;

internal static class DirectoryInfoExtensions
{
    public static IEnumerable<FileInfo> FindFiles(this DirectoryInfo directoryInfo, IPluginLogger logger, ListParameters listParameters)
    {
        if (directoryInfo == null)
            throw new ArgumentNullException(nameof(directoryInfo), Resources.TheDirectoryCouldNotBeNull);

        if (!directoryInfo.Exists)
            throw new DirectoryNotFoundException(string.Format(Resources.TheDirectoryDoesNotExist,directoryInfo.FullName));

        var searchOption = listParameters.Recurse is true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = directoryInfo.EnumerateFiles("*", searchOption);

        var result = new List<FileInfo>();
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
                var resultCount = 0;
                var isMatched = regex != null && regex.IsMatch(file.FullName);

                if (listParameters.Filter != null && !isMatched) 
                    continue;

                result.Add(file);
                resultCount++;

                // Stop once we reach the maxResults
                if (listParameters.MaxResults.HasValue && resultCount >= listParameters.MaxResults)
                {
                    break;
                }
            }
            catch (Exception ex) 
            {
                logger.LogError(ex.Message);
            }
        }

        return result;
    }
}
