using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace comroid.common;

public static class LinqUtil
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> input)
    {
        var array = input.ToArray();
        var count = array.Length;
        var indices = new List<int>();
        for (var i = 0; i < count; i++)
            indices.Add(i);
        var rng = new Random();
        while (indices.Count > 0)
        {
            var indexIndex = rng.Next(indices.Count);
            var index = indices[indexIndex];
            indices.RemoveAt(indexIndex);
            yield return array[index];
        }
    }

    public static IEnumerable<R?> CastOrDefault<T, R>(this IEnumerable<T?> input, Func<R?> fallback = null!)
    {
        fallback ??= () => default;
        foreach (var it in input)
            yield return (R?)(object?)it ?? fallback();
    }

    public static IEnumerable<R> CastOrSkip<R>(this IEnumerable input)
    {
        foreach (var it in input)
            if (it is R r)
                yield return r;
    }
}