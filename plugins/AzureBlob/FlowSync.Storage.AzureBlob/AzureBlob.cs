using FlowSync.Abstractions;
using FlowSync.Abstractions.Entities;
using FlowSync.Abstractions.Extensions;

namespace FlowSync.Storage.AzureBlob;

public class AzureBlob : Plugin
{
    private readonly AzureBlobSpecifications? _specifications;

    public AzureBlob(IDictionary<string, object>? specifications) : base(specifications)
    {
        _specifications = specifications.CastToObject<AzureBlobSpecifications>();
    }

    public override Guid Id => Guid.Parse("7f21ba04-ea2a-4c78-a2f9-051fa05391c8");
    public override string Name => "AzureBlob";
    public override string? Description => null;

    public override Task<Usage> About(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<IEnumerable<FileSystemEntity>> ListAsync(string path, FileSystemFilterOptions? filters = null, CancellationToken cancellationToken = default)
    {
        ICollection<FileSystemEntity> result = new List<FileSystemEntity>() {
            //new Entity("c:\\users\\", "Matrix", EntityItemKind.Directory),
            //new Entity("c:\\users\\Matrix\\", "MatrixThumbnail.jpg", EntityItemKind.File),
            //new Entity("c:\\users\\Matrix\\", "Matrix.mp4", EntityItemKind.File),
        };
        //Console.WriteLine(_specifications.AccountName);
        return Task.FromResult(result.AsEnumerable());
    }

    public override Task WriteAsync(string path, FileStream dataStream, bool append = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<FileStream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteFileAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}