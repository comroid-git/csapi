using System.Net.Sockets;
using System.Text;
using comroid.common;

namespace comroid.fsock;

[Obsolete] // WIP
public class FileSocket : Stream
{
    private readonly FileStream _underlying;
    private readonly bool _receive;
    private bool _active;
    private long i;
    public string Path { get; }

    public bool Receiving
    {
        get => _receive && _active;
        private set => _active = value;
    }

    public event Action<string>? LineReceived;

    public FileSocket(string path, bool receive = false)
    {
        FileStream? fs = null;
        var mode = receive ? FileMode.Truncate : FileMode.Append;
        if (!File.Exists(path) &&
            !DebugUtil.TouchFile(path, out fs, out var e, mode: mode))
            throw new Exception("Could not create socket " + path, e);
        _underlying = fs ?? File.Open(path, mode);
        _receive = _active = receive;
        Path = path;
    }

#pragma warning disable CS1998
    public async Task StartReceiving()
    {
        while (Receiving)
        {
            var line = ReadLine();
            LineReceived?.Invoke(line);
        }
    }
#pragma warning restore CS1998

    public string ReadLine()
    {
        var data = string.Empty;
        int r;
        while ((r = ReadByte()) != -1)
            if (r is '\r' or '\n')
                break;
            else data += (char)r;
        if (r is not '\r' and not '\n')
            Log<FileSocket>.At(LogLevel.Warning,
                $"Input was terminated at invalid state; must end with CR/LF; got {(char)r}");
        return data;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!_receive)
            throw new NotSupportedException("Cannot read from sender FileSocket");
        var r = _underlying.Read(buffer, offset, count);
        i += r;
        return r;
    }

    public void Write(object? it)
    {
        var data = Encoding.ASCII.GetBytes(it?.ToString() ?? string.Empty);
        i += data.Length;
        Write(data, 0, data.Length);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_receive)
            throw new NotSupportedException("Cannot write to receiver FileSocket");
        _underlying.Write(buffer, offset, count);
    }

    public override void Flush()
    {
    }

    protected override void Dispose(bool disposing)
    {
        _active = false;
        _underlying.Dispose();
        base.Dispose(disposing);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    public override bool CanRead => _receive;
    public override bool CanWrite => !CanRead;
    public override bool CanSeek => false;
    public override long Length => long.MaxValue;

    public override long Position
    {
        get => i;
        set => throw new NotSupportedException();
    }
}