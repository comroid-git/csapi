using System;
using System.Runtime.InteropServices;

namespace comroid.common;

public static class InteropUtil
{
    public static T ToStruct<T>(this byte[] data) where T : unmanaged
    {
        var str = Activator.CreateInstance<T>();
        var size = Marshal.SizeOf(str);
        var ptr = IntPtr.Zero;

        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, ptr, size);
            str = (T)Marshal.PtrToStructure(ptr, str!.GetType())!;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return str;
    }

    public static byte[] ToBytes<T>(this T msg) where T : unmanaged
    {
        var size = Marshal.SizeOf(msg);
        var arr = new byte[size];
        var ptr = IntPtr.Zero;

        try
        {
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(msg, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }

        return arr;
    }
}