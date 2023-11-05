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

    public override IEnumerable<string>? SupportedVersions => new List<string>() { "1.0", "2.0" };

    public override Task<IEnumerable<Entity>> ListAsync(string path, FilterOptions? filters = null, CancellationToken cancellationToken = default)
    {
        ICollection<Entity> result = new List<Entity>() {
            new Entity("c:\\users\\", "Matrix", EntityItemKind.Directory),
            new Entity("c:\\users\\Matrix\\", "MatrixThumbnail.jpg", EntityItemKind.File),
            new Entity("c:\\users\\Matrix\\", "Matrix.mp4", EntityItemKind.File),
        };
        Console.WriteLine(_specifications.AccountName);
        return Task.FromResult(result.AsEnumerable());
    }

    public override Task<IEnumerable<bool>> ExistsAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task WriteAsync(string path, Stream dataStream, bool append = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<Stream> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task CreateDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task DeleteAsync(IEnumerable<string> path, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}