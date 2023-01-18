using comroid.csapi.common;
using NUnit.Framework;

namespace common.test;

public class ByteAdapterTest
{
    private string path;
    private TestObj obj;

    [SetUp]
    public void Setup()
    {
        path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "file.bin");
        obj = new TestObj()
        {
            Type = TestObj.ObjType.Library,
            Name = "root object",
            Inner = new TestObj()
            {
                Type = TestObj.ObjType.Component,
                Name = "inner object"
            }
        };

        Console.WriteLine("path=" + path);
    }

    [TearDown]
    public void teardown()
    {
        obj = null!;
    }

    [Test]
    public void _1_SaveObj()
    {
        using var fileStream = new FileStream(path, FileMode.Create);
        (obj as IByteContainer).Save(fileStream);
    }

    [Test]
    public void _2_LoadObj()
    {
        using var fileStream = new FileStream(path, FileMode.Open);
        obj = new TestObj();
        (obj as IByteContainer).Load(fileStream);
        Console.WriteLine("read=" + obj);
    }

    [Test]
    public void _3_LoadObj_Direct()
    {
        using var fileStream = new FileStream(path, FileMode.Open);
        obj = IByteContainer.FromStream<TestObj>(fileStream);
        Console.WriteLine("read=" + obj);
    }
}

public class TestObj : IByteContainer.Abstract
{
    public enum ObjType : byte
    {
        Unknown = default,
        Component,
        Library
    }

    [ByteData(0)]
    public ObjType Type { get; set; }
    [ByteData(1)]
    public string Name { get; set; }
    [ByteData(2)]
    public TestObj? Inner { get; set; }

    public override string ToString() => $"type={Type}; name={Name}; inner={Inner}";
}