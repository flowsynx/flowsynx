using EnsureThat;

namespace FlowSync.Abstractions.Storage;

public class StorageStream: Stream
{
    private readonly Stream _origin;
    private bool _noRead = true;

    /// <summary>
    /// StorageStream
    /// </summary>
    public StorageStream(Stream origin)
    {
        EnsureArg.IsNotNull(origin, nameof(origin));
        _origin = origin;
    }

    /// <summary>
    /// No change
    /// </summary>
    public override bool CanRead => _origin.CanRead;

    /// <summary>
    /// Always true
    /// </summary>
    public override bool CanSeek => true;

    /// <summary>
    /// Always false
    /// </summary>
    public override bool CanWrite => false;

    /// <summary>
    /// No change
    /// </summary>
    public override long Length => _origin.Length;

    /// <summary>
    /// No change
    /// </summary>
    public override long Position { get => _origin.Position; set => _origin.Position = value; }

    /// <summary>
    /// No change
    /// </summary>
    public override void Flush() => _origin.Flush();

    /// <summary>
    /// see <see cref="ReadAsync(byte[], int, int, CancellationToken)"/>
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count).GetAwaiter().GetResult();

    /// <summary>
    /// No change, but remembers that read was performed
    /// </summary>
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        _noRead = false;

        return _origin.ReadAsync(buffer, offset, count, cancellationToken);
    }

    /// <summary>
    /// Only allows seeks to beginning if no reads were performed
    /// </summary>
    public override long Seek(long offset, SeekOrigin origin)
    {
        if (_noRead && offset == 0 && origin == SeekOrigin.Begin)
            return 0;

        throw new NotSupportedException();
    }

    /// <summary>
    /// Change to "not supported"
    /// </summary>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>
    /// Change to "not supported"
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <summary>
    /// No change
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(bool disposing)
    {
        _origin.Dispose();
    }
}