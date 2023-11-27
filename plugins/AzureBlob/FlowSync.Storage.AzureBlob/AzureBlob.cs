using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions.Extensions;
using FlowSync.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace FlowSync.Storage.AzureBlob;

public class AzureBlob : IFileSystemPlugin
{
    private readonly ILogger<AzureBlob> _logger;
    private AzureBlobSpecifications? _specifications;

    public AzureBlob(ILogger<AzureBlob> logger)
    {
        _logger = logger;
    }

    public Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public string Namespace => "FlowSync.FileSystem/Microsoft.AzureBlob";
    public string? Description => null;
    public void SetSpecifications(IDictionary<string, object>? specifications) => _specifications = specifications.CastToObject<AzureBlobSpecifications>();

    public Task<Usage> About(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<FileSystemEntity>> ListAsync(string path, FileSystemFilterOptions? filters = null, CancellationToken cancellationToken = default)
    {
        ICollection<FileSystemEntity> result = new List<FileSystemEntity>() {
            //new Entity("c:\\users\\", "Matrix", EntityItemKind.Directory),
            //new Entity("c:\\users\\Matrix\\", "MatrixThumbnail.jpg", EntityItemKind.File),
            //new Entity("c:\\users\\Matrix\\", "Matrix.mp4", EntityItemKind.File),
        };
        //Console.WriteLine(_specifications.AccountName);
        return Task.FromResult(result.AsEnumerable());
    }

    public Task WriteAsync(string path, FileStream dataStream, bool append = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<FileStream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(string path, FileSystemFilterOptions fileSystemFilters, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}