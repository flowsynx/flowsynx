using EnsureThat;
using FlowSynx.IO;
using FlowSynx.Net;
using FlowSynx.Plugin.Abstractions;
using FlowSynx.Reflections;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.RegularExpressions;

namespace FlowSynx.Plugin.Storage.Http;

public class HttpStorage : IStoragePlugin
{
    private readonly ILogger<HttpStorage> _logger;
    private readonly IStorageFilter _storageFilter;
    private readonly IHttpRequestService _httpRequestService;
    private Dictionary<string, object?>? _specifications;
    private HttpSpecifications? _httpSpecifications;

    public HttpStorage(ILogger<HttpStorage> logger, IStorageFilter storageFilter, 
        IHttpRequestService httpRequestService)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(storageFilter, nameof(storageFilter));
        _logger = logger;
        _storageFilter = storageFilter;
        _httpRequestService = httpRequestService;
    }

    public Guid Id => Guid.Parse("dff77655-a61e-492c-bd82-dee00d159bfc");

    public string Name => "Http";

    public PluginNamespace Namespace => PluginNamespace.Storage;

    public string? Description => "This is a read only storage for reading files of a web server. The web server should provide file listings which flowsynx will read it.";

    public Dictionary<string, object?>? Specifications
    {
        get => _specifications;
        set
        {
            _specifications = value;
            _httpSpecifications = value.DictionaryToObject<HttpSpecifications>();
        }
    }

    public Type SpecificationsType => typeof(HttpSpecifications);

    private const string Message = "Http storage is read only";

    private Uri CreateRootUri()
    {
        if (_httpSpecifications == null)
            throw new StorageException("The specifications for http storage is null. This storage required specifications.");
        
        if (string.IsNullOrEmpty(_httpSpecifications.Url))
            throw new StorageException("The Url value in Http specifications should be not empty.");

        return new Uri(_httpSpecifications.Url);
    }

    public Task<StorageUsage> About(CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public async Task<IEnumerable<StorageEntity>> ListAsync(string path, StorageSearchOptions searchOptions,
        StorageListOptions listOptions, StorageHashOptions hashOptions, CancellationToken cancellationToken = default)
    {
        var result = new List<StorageEntity>();

        var rootUri = CreateRootUri();
        var url = CombineUri(rootUri.ToString(), path);
        var remaining = new Queue<Uri>();
        remaining.Enqueue(url);
        while (remaining.Count > 0)
        {
            var dequeueUri = remaining.Dequeue();
            //var baseUri = new Uri(dequeueUri.ToString().TrimEnd('/'));
            //var rootUrl = dequeueUri.GetLeftPart(UriPartial.Authority);

            var regexFile = new Regex("[0-9] <a href=\"(http:|https:)?(?<file>.*?)\"", RegexOptions.IgnoreCase);
            var regexDir = new Regex("dir.*?<a href=\"(http:|https:)?(?<dir>.*?)\"", RegexOptions.IgnoreCase);

            var html = await ReadHtmlContentFromUrl(dequeueUri.ToString(), cancellationToken);

            var matchesFile = regexFile.Matches(html);
            if (matchesFile.Count != 0)
            {
                foreach (Match match in matchesFile)
                {
                    if (match.Success && listOptions.Kind is StorageFilterItemKind.File or StorageFilterItemKind.FileAndDirectory)
                    {
                        result.Add(HttpConverter.ToEntity(rootUri, match.Groups, StorageEntityItemKind.File));
                    }
                }
            }

            var matchesDir = regexDir.Matches(html);
            if (matchesDir.Count != 0)
            {
                foreach (Match match in matchesDir)
                {
                    if (match.Success)
                    {
                        if (listOptions.Kind is StorageFilterItemKind.Directory or StorageFilterItemKind.FileAndDirectory)
                        {
                            result.Add(HttpConverter.ToEntity(rootUri, match.Groups, StorageEntityItemKind.Directory));
                        }

                        if (!searchOptions.Recurse) continue;

                        remaining.Enqueue(CombineUri(rootUri.ToString(), match.Groups["dir"].ToString()));
                    }
                }
            }
        }

        var filteredResult = _storageFilter.FilterEntitiesList(result, searchOptions, listOptions);

        if (listOptions.MaxResult is > 0)
            filteredResult = filteredResult.Take(listOptions.MaxResult.Value);

        return filteredResult;
    }

    public Task WriteAsync(string path, StorageStream storageStream, StorageWriteOptions writeOptions,
        CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public Task<StorageRead> ReadAsync(string path, StorageHashOptions hashOptions,
        CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public Task<bool> FileExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public Task DeleteAsync(string path, StorageSearchOptions storageSearches, CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public Task MakeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public Task PurgeDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public Task<bool> DirectoryExistAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new StorageException(Message);
    }

    public void Dispose() { }

    private Uri CombineUri(string baseUri, string relativeOrAbsoluteUri)
    {
        return new Uri(new Uri(baseUri), relativeOrAbsoluteUri);
    }

    private async Task<string> ReadHtmlContentFromUrl(string url, CancellationToken cancellationToken)
    {
        var request = new Request
        {
            Uri = url,
            HttpMethod = HttpMethod.Get
        };
        var stream = await _httpRequestService.SendRequestAsync(request, cancellationToken);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}