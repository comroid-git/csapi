using System;

namespace comroid.csapi.common;

public static class ReflectionUtil
{
    public static bool IsArrayOf<T>(this Type type) => type.IsArray && type.GetElementType()!.IsAssignableTo(typeof(T));
}