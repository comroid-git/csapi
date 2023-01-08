using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace comroid.csapi.common;

public static class Extensions
{
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
    
    public static string CreateMd5ForFolder(this DirectoryInfo dir)
    {
        // https://stackoverflow.com/questions/3625658/how-do-you-create-the-hash-of-a-folder-in-c
        // assuming you want to include nested folders
        var files = Directory.GetFiles(dir.FullName, "*.*", SearchOption.AllDirectories)
            .OrderBy(p => p).ToList();

        var md5 = MD5.Create();

        for(int i = 0; i < files.Count; i++)
        {
            var file = files[i];

            // hash path
            var relativePath = file.Substring(dir.FullName.Length + 1);
            var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
            md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

            // hash contents
            var contentBytes = File.ReadAllBytes(file);
            if (i == files.Count - 1)
                md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
            else md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
        }

        return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
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