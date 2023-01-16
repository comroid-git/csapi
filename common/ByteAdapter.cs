using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace comroid.csapi.common;

public interface IStringCache : IByteContainer
{
    public int this[string str] { get; }
    public string this[int id] { get; }
}

public interface IByteContainer
{
    public byte[] Bytes
        => new[] { (byte)DataType }
            .Concat(DataType < DataType.Object ? ArraySegment<byte>.Empty : BitConverter.GetBytes(Length))
            .Concat(BitConverter.GetBytes(Header.Bytes.Length))
            .Concat(Header.Bytes)
            .Concat(BodyBytes).ToArray();
    public IByteContainer Header => Empty();
    public DataType DataType => DataType.Object;
    public int Length => DataType.GetLength(() => Bytes);
    public IEnumerable<byte> BodyBytes => BitConverter.GetBytes(Members.Count).Concat(Members.SelectMany(x => x.Bytes));
    public List<IByteContainer> Members => new();

    public static IByteContainer Empty() => new Constant();
    public static IByteContainer Concat(params IByteContainer[] arr) => new Concatenated(arr);
    public static IByteContainer Const<T>(T value, IStringCache? strings = null, Encoding? encoding = null)
        => value switch
        {
            null => Empty(),
            byte b => new Constant(DataType.Byte, b),
            sbyte sb => new Constant(DataType.SByte, System.Convert.ToByte(sb)),
            char c => new Constant(DataType.Char, BitConverter.GetBytes(c)),
            short s => new Constant(DataType.Short, BitConverter.GetBytes(s)),
            ushort us => new Constant(DataType.UShort, BitConverter.GetBytes(us)),
            int i => new Constant(DataType.Int, BitConverter.GetBytes(i)),
            uint ui => new Constant(DataType.UInt, BitConverter.GetBytes(ui)),
            long l => new Constant(DataType.Long, BitConverter.GetBytes(l)),
            ulong ul => new Constant(DataType.ULong, BitConverter.GetBytes(ul)),
            float f => new Constant(DataType.Float, BitConverter.GetBytes(f)),
            double d => new Constant(DataType.Double, BitConverter.GetBytes(d)),
            string str => strings == null
                ? new Constant(DataType.String,
                    BitConverter.GetBytes((encoding ?? Encoding.ASCII).GetBytes(str).Length)
                        .Concat((encoding ?? Encoding.ASCII).GetBytes(str)).ToArray())
                : new Constant(DataType.StringCached, BitConverter.GetBytes(strings[str])),
            IByteContainer c => c,
            _ => throw new ArgumentException("Cannot convert value", nameof(value))
        };
    public T Convert<T>(IStringCache? strings = null, Encoding? encoding = null)
        => Bytes.Read<T>(encoding, strings);

    public static Abstract FromStream<T>(Stream stream, IStringCache? strings = null, Encoding? encoding = null)
        where T : Abstract => FromStream(stream, typeof(T), strings, encoding);
    public static Abstract FromStream(Stream stream, Type type, IStringCache? strings = null, Encoding? encoding = null)
    { // todo: inspect
        var dataType = (DataType)stream.ReadByte();
        if (dataType < DataType.Object)
            return stream.ReadContainer(dataType);
        var bodyLen = stream.Read<int>();
        var headLen = stream.Read<int>();
        var headData = stream.Read(headLen);
        var memberCount = stream.Read<int>();
        var obj = (Abstract)type.GetConstructor(BindingFlags.Public, Type.EmptyTypes)?.Invoke(null)!;

        var c = 0;
        foreach (var (prop, attr) in FindAttributes(type))
        {
            var value = FromStream(stream, prop.PropertyType, strings, encoding);
            prop.SetValue(obj, value);
            c++;
        }

        if (c != memberCount)
            throw new Exception("Invalid member count was read; cannot continue");

        return obj;
    }

    public void Load(Stream stream, IStringCache? strings = null, Encoding? encoding = null)
    {
        if (!stream.CanRead)
            throw new ArgumentException("Cannot read from stream", nameof(stream));

        //BodyBytes = FromStream<object>(stream, strings, encoding);
    }

    public void Save(Stream stream, IStringCache? strings = null, Encoding? encoding = null)
    {
        if (!stream.CanWrite)
            throw new ArgumentException("Cannot write to stream", nameof(stream));
        
        stream.Write(Bytes);
        
        var type = GetType();
        foreach (var (prop, attr) in FindAttributes(type))
        {
            var obj = prop.GetValue(this);
            try
            {
                var it = Const(obj, strings, encoding);
                stream.Write(it.Bytes);
            }
            catch (ArgumentException)
            {
                Log<IByteContainer>.At(LogLevel.Debug, $"Converting value {prop.Name} / {obj} failed; skipping it");
            }
        }
        stream.Flush();
    }

    protected static IEnumerable<(PropertyInfo prop, ByteDataAttribute attr)> FindAttributes(Type type) =>
        type.GetMembers(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty)
            .Select(member => (member, member.GetCustomAttribute<ByteDataAttribute>()!))
            .Where(pair => pair.Item2 != null)
            .Select(pair => (type.GetProperty(pair.member.Name)!, pair.Item2))
            .OrderBy(pair => pair.Item2.Index);

    public abstract class Abstract : IByteContainer
    {
        public DataType DataType { get; }
        public virtual IByteContainer Header { get; init; } = Empty();
        public List<IByteContainer> Members { get; internal set; } = new();
        public virtual IEnumerable<byte> BodyBytes => Members.SelectMany(x => x.Bytes);

        protected Abstract(DataType dataType)
        {
            DataType = dataType;
        }
    }

    public sealed class Constant : Abstract
    {
        public override byte[] BodyBytes { get; }

        public Constant(DataType dataType = DataType.Empty, params byte[] bytes) : base(dataType)
        {
            BodyBytes = bytes;
        }
    }
    
    public sealed class Concatenated : Abstract
    {
        private readonly IByteContainer[] _arr;
        public override IEnumerable<byte> BodyBytes => _arr.SelectMany(x => x.Bytes);

        public Concatenated(params IByteContainer[] arr) : base(DataType.Concatenated)
        {
            _arr = arr;
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class ByteDataAttribute : Attribute
{
    public readonly int Index;

    public ByteDataAttribute(int index)
    {
        Index = index;
    }
}

public enum DataType : byte
{
    Empty = default,
    
    // managed types; size is known
    Byte = 1,
    SByte = 2,
    Char = 3,
    Short = 4,
    UShort = 5,
    Int = 6,
    UInt = 7,
    Long = 8,
    ULong = 9,
    Float = 10,
    Double = 11,
    // size is written as int32 in first 4 bytes
    String = 14,
    // size is sizeof(int)
    StringCached = 15,
    
    // unmanaged types; size is unknown
    Object = 16,
    Concatenated = 63
}

public static class ByteAdapterExtensions
{
    public static byte[] Read(this Stream stream, int n)
    {
        var buf = new byte[n];
        if (stream.Read(buf) != n)
            Log.BaseLogger.At(LogLevel.Warning, $"Read returned less bytes than expected! Expect errors");
        return buf;
    }

    public static T Read<T>(this byte[] data, Encoding? encoding = null, IStringCache? strings = null)
        => Read<T>(new MemoryStream(data), encoding, strings);

    public static IByteContainer.Constant ReadContainer(this Stream stream, DataType? type = null)
    {
        type ??= (DataType)stream.ReadByte();
        var len = type.Value.GetLength();
        if (len == -1 && type == DataType.String)
            len = stream.Read<int>();
        if (len == -1)
            throw new Exception("Invalid DataType: " + type);
        var data = stream.Read(len);
        return new IByteContainer.Constant(type.Value, data);
    }

    public static T Read<T>(this Stream stream, Encoding? encoding = null, IStringCache? strings = null) => (T)Read(stream, typeof(T), encoding, strings);
    public static object Read(this Stream stream, Type type, Encoding? encoding = null, IStringCache? strings = null)
    {
        var len = type.GetLength();
        if (len == -1 && strings == null && type == typeof(string))
            len = stream.Read<int>();
        if (len == -1)
            throw new ArgumentException("Invalid Type: " + type, nameof(type));
        var data = stream.Read(len);
        return type.Name switch
        {
            "byte" => data[0],
            "sbyte" => Convert.ToSByte(data[0]),
            "char" => BitConverter.ToChar(data),
            "short" => BitConverter.ToInt16(data),
            "ushort" => BitConverter.ToUInt16(data),
            "int" => BitConverter.ToInt32(data),
            "uint" => BitConverter.ToUInt32(data),
            "long" => BitConverter.ToInt64(data),
            "ulong" => BitConverter.ToUInt64(data),
            "float" => BitConverter.ToSingle(data),
            "double" => BitConverter.ToDouble(data),
            "string" => strings != null
                ? strings[BitConverter.ToInt32(data)]
                : encoding!.GetString(data),
            _ => throw new ArgumentException("Invalid Type: " + type, nameof(type))
        };
    }

    public static int GetLength(this Type type) => type.Name switch
    {
        "byte" => sizeof(byte),
        "sbyte" => sizeof(sbyte),
        "char" => sizeof(char),
        "short" => sizeof(short),
        "ushort" => sizeof(ushort),
        "int" => sizeof(int),
        "uint" => sizeof(uint),
        "long" => sizeof(long),
        "ulong" => sizeof(ulong),
        "float" => sizeof(float),
        "double" => sizeof(double),
        _ => -1
    };
    
    public static int GetLength(this DataType type, Func<byte[]>? Bytes = null) => type switch
    {
        DataType.Empty => 0,
        DataType.Byte => sizeof(byte),
        DataType.SByte => sizeof(sbyte),
        DataType.Char => sizeof(char),
        DataType.Short => sizeof(short),
        DataType.UShort => sizeof(ushort),
        DataType.Int => sizeof(int),
        DataType.UInt => sizeof(uint),
        DataType.Long => sizeof(long),
        DataType.ULong => sizeof(ulong),
        DataType.Float => sizeof(float),
        DataType.Double => sizeof(double),
        DataType.StringCached => sizeof(int),
        _ => Bytes?.Invoke()?.Length ?? -1
    };
}
