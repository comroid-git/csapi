using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace comroid.common;


public static class Extensions
{
    public static bool CanCast<T>(this object it) => it.CanCast(typeof(T));
    public static bool CanCast(this object it, Type type) => type.IsInstanceOfType(it);

    public static T? As<T>(this object it) => it.CanCast<T>()/**/ ? (T)it : (T?)(object?)null;//&& Nullable.GetUnderlyingType(typeof(T)) == null ? (T)it : Nullable.GetUnderlyingType(typeof(T)) != null ? (T)(object?)null! : throw new InvalidCastException($"Cannot cast {it} to non-nullable type {typeof(T)}");

    public static int CopyTo(this DirectoryInfo source, string targetPath)
    {
        var sourcePath = source.FullName;
        var c = 0;

        //https://stackoverflow.com/a/3822913
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            c += 1;
        }

        return c;
    }

    public static int Redirect(this Stream input, Stream output, int bufferSize = 64)
    {
        int r, c = 0;
        var buf = new byte[bufferSize];
        while ((r = input.Read(buf, 0, bufferSize)) != -1)
        {
            output.Write(buf, 0, r);
            c += r;
        }

        return c;
    }

    public static void UpdateMd5(this FileSystemInfo path, Func<FileSystemInfo, string> md5path)
    {
        var md5 = md5path(path);
        new DirectoryInfo(Path.GetDirectoryName(md5)!).Create();
        File.WriteAllText(md5, path.CreateMd5());
    }

    public static bool IsUpToDate(this FileSystemInfo path, Func<FileSystemInfo, string> md5path) =>
        new FileInfo(md5path(path)) is { Exists: true } file &&
        File.ReadAllText(file.FullName) == path.CreateMd5();

    public static string CreateMd5(this FileSystemInfo path)
    {
        // https://stackoverflow.com/questions/3625658/how-do-you-create-the-hash-of-a-folder-in-c
        // assuming you want to include nested folders
        var files = path is FileInfo f
            ? new[] { f.FullName }
            : Directory.GetFiles(path.FullName, "*.*", SearchOption.AllDirectories).OrderBy(p => p).ToArray();

        var md5 = MD5.Create();

        for (int i = 0; i < files.Length; i++)
        {
            var file = files[i];

            // hash path
            var relativePath = path is FileInfo ? path.Name : file.Substring(path.FullName.Length + 1);
            var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
            md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

            // hash contents
            var contentBytes = File.ReadAllBytes(file);
            if (i == files.Length - 1)
                md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
            else md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
        }

        return BitConverter.ToString(md5.Hash!).Replace("-", "").ToLower();
    }

    public static T Await<T>(this Task<T> task)
    {
        task.Wait();
        if (task.Exception != null)
            throw task.Exception;
        return task.Result;
    }

    public static StreamWriter ToStreamWriter(this TextWriter writer) => new(new TextWriterStream(writer));
}

internal class TextWriterStream : Stream
{
    private readonly TextWriter writer;
    public TextWriterStream(TextWriter writer) => this.writer = writer;
    public override void Flush() => writer.Flush();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) =>
        writer.Write(buffer.Select(x => (char)x).ToArray(), offset, count);
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => -1;
    public override long Position
    {
        get => -1;
        set => throw new NotSupportedException();
    }
}

public class TempFile : FileSystemInfo, IDisposable
{
    private readonly FileInfo _file;
    public TempFile(string? path = null) => _file = new FileInfo(path ?? Path.GetTempFileName());
    public override bool Exists => _file.Exists;
    public override string Name => _file.Name;
    public override string FullName => _file.FullName;
    public override void Delete() => _file.Delete();
    public void Dispose() => Delete();
}