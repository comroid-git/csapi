using System;
using System.Collections.Generic;
using System.Linq;

namespace comroid.csapi.common;

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
            yield return array[indices[rng.Next(indices.Count)]];
    }
}