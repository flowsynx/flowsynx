namespace FlowSynx.Plugin.Storage.Memory;

public class MemoryFileStream : Stream
{
    private readonly MemoryEntity _entity;

    public byte[] Content
    {
        get => _entity.Content;
        set => _entity.Content = value;
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => _entity.Content.Length;

    public override long Position { get; set; }

    public MemoryFileStream(MemoryEntity entity)
    {
        _entity = entity;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (origin == SeekOrigin.Begin)
            return Position = offset;
        if (origin == SeekOrigin.Current)
            return Position += offset;
        return Position = Length - offset;
    }

    public override void SetLength(long value)
    {
        int newLength = (int)value;
        byte[] newContent = new byte[newLength];
        Buffer.BlockCopy(Content, 0, newContent, 0, Math.Min(newLength, (int)Length));
        Content = newContent;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int mincount = Math.Min(count, Math.Abs((int)(Length - Position)));
        Buffer.BlockCopy(Content, (int)Position, buffer, offset, mincount);
        Position += mincount;
        return mincount;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (Length - Position < count)
            SetLength(Position + count);
        Buffer.BlockCopy(buffer, offset, Content, (int)Position, count);
        Position += count;
    }
}