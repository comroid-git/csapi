using System;
using System.Collections.Generic;
using System.Linq;
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
            .Concat((byte)DataType < 16 ? ArraySegment<byte>.Empty : BitConverter.GetBytes(Length))
            .Concat(Header.Bytes)
            .Concat(BodyBytes).ToArray();
    public IByteContainer Header => Empty();
    public DataType DataType => DataType.Object;
    public int Length => DataType.GetLength(() => Bytes);
    public IEnumerable<byte> BodyBytes => Members.SelectMany(x => x.Bytes);
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
    public T Convert<T>(IStringCache? strings = null, Encoding? encoding = null) => (T)(object)(DataType switch
    {
        DataType.Empty => null!,
        DataType.Byte => Bytes[0],
        DataType.SByte => System.Convert.ToSByte(Bytes[0]),
        DataType.Char => BitConverter.ToChar(Bytes),
        DataType.Short => BitConverter.ToInt16(Bytes),
        DataType.UShort => BitConverter.ToUInt16(Bytes),
        DataType.Int => BitConverter.ToInt32(Bytes),
        DataType.UInt => BitConverter.ToUInt32(Bytes),
        DataType.Long => BitConverter.ToInt64(Bytes),
        DataType.ULong => BitConverter.ToUInt64(Bytes),
        DataType.Float => BitConverter.ToSingle(Bytes),
        DataType.Double => BitConverter.ToDouble(Bytes),
        DataType.String => (encoding ?? Encoding.ASCII).GetString(Bytes),
        DataType.StringCached => strings == null 
            ? throw new ArgumentException("StringCache must not be null!", nameof(strings)) 
            : strings[BitConverter.ToInt32(Bytes)],
        _ => throw new ArgumentException("Cannot convert value of type " + DataType, nameof(DataType))
    });

    public abstract class Abstract : IByteContainer
    {
        public DataType DataType { get; }
        public virtual IByteContainer Header { get; init; } = Empty();
        public List<IByteContainer> Members { get; } = new();
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

public class ByteAdapter
{
}

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
    StringCached = 15,
    
    // unmanaged types; size is unknown
    String = 32,
    Concatenated = 128,
    Object = 255
}

public static class ByteAdapterExtensions
{
    public static int GetLength(this DataType type, Func<byte[]> Bytes) => type switch
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
        _ => Bytes().Length
    };
}
