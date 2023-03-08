namespace NjuCsCmsHelper.Server.Utils;

class TempFile
{
    readonly FileInfo fileInfo;

    public TempFile()
    {
        var path = Path.GetTempFileName();
        fileInfo = new FileInfo(path);
        fileInfo.Attributes |= FileAttributes.Temporary;
    }

    public Stream OpenWrite()
    {
        return fileInfo.OpenWrite();
    }

    public Stream OpenRead(bool deleteOnClose = false)
    {
        if (deleteOnClose)
        {
            return new DeleteOnCloseStream(fileInfo.OpenRead(), fileInfo.FullName);
        }
        else
        {
            return fileInfo.OpenRead();
        }
    }

    private class DeleteOnCloseStream : StreamWrapper
    {
        private readonly string path;

        public DeleteOnCloseStream(Stream stream, string path) : base(stream)
        {
            this.path = path;
        }

        public override void Close()
        {
            base.Close();
            File.Delete(path);
        }
    }

}

public class StreamWrapper : Stream
{
    private readonly Stream stream;

    public StreamWrapper(Stream stream)
    {
        this.stream = stream;
    }

    public override bool CanRead => stream.CanRead;
    public override bool CanSeek => stream.CanSeek;
    public override bool CanWrite => stream.CanWrite;

    public override long Length => stream.Length;

    public override long Position
    {
        get => stream.Position;
        set => stream.Position = value;
    }

    public override void Flush() => stream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => stream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => stream.Seek(offset, origin);
    public override void Write(byte[] buffer, int offset, int count) => stream.Write(buffer, offset, count);

    public override void SetLength(long value) => stream.SetLength(value);
}
