using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace comroid.common;

public class ConfigBase : IEnumerable<KeyValuePair<string, object?>>
{
    public readonly ConcurrentDictionary<string, ConfigEntry> Entries = new();

    public object? this[string key]
    {
        get => Get<object>(key);
        set => Entries[key].Value = value;
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => ToDict().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add<T>(string key,
        Func<string, T?>? inputConverter = null,
        Func<T?>? defaultValue = null,
        Func<T?, string>? outputConverter = null)
    {
        Entries[key] = new ConfigEntry<T>(key, defaultValue, inputConverter, outputConverter);
    }

    public void Set(string key, string value)
    {
        Entries[key]?.Set(value);
    }

    public string Get(string key) => Entries[key].ConvertOutput();

    public T? Get<T>(string key) => (T?)Entries[key].Value;

    public Dictionary<string, object?> ToDict()
    {
        return Entries.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.Value);
    }

    public override string ToString()
    {
        return Entries.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.ConvertOutput())
            .Aggregate("{\r\n", (str, entry) => str + $"\t{{{entry.Key}: {entry.Value} }}") + "\r\n}";
    }

    public void Save(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(ToDict(), typeof(Dictionary<string, object>)));
    }

    public void Load(string path)
    {
        foreach (var (key, value) in
                 JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(path))!)
            Entries[key].Set(value.ToString());
    }
}

public abstract class ConfigEntry
{
    public string Name;
    public Type Type;

    public ConfigEntry(string name, Type type)
    {
        Type = type;
        Name = name;
    }

    public abstract object? Value { get; set; }
    public abstract object? DefaultValue { get; }

    public void Set(string str)
    {
        Value = ConvertInput(str);
    }

    public abstract object? ConvertInput(string str);
    public abstract string ConvertOutput();
}

public class ConfigEntry<T> : ConfigEntry
{
    public readonly Func<T?> defaultValue;
    public readonly Func<string, T?> inputConverter;
    public readonly Func<T?, string> outputConverter;
    public T? value;

    internal ConfigEntry(string name, Func<T?>? defaultValue, Func<string, T?>? inputConverter,
        Func<T?, string>? outputConverter) : base(name, typeof(T))
    {
        defaultValue ??= () => default;
        inputConverter ??= it => (T)(object)it!;
        outputConverter ??= arg => arg?.ToString() ?? throw new Exception(
            $"Could not convert {arg} to target type {typeof(T)}");
        value = defaultValue();
        this.defaultValue = defaultValue;
        this.inputConverter = inputConverter;
        this.outputConverter = outputConverter;
    }

    public override object? Value
    {
        get => value ?? defaultValue();
        set => this.value = (T?)value;
    }

    public override object? DefaultValue
    {
        get => defaultValue();
    }

    public override object? ConvertInput(string str) => inputConverter(str);

    public override string ConvertOutput() => Type.IsArrayOf<string>()
        ? string.Join(", ", (string[])Value!)
        : outputConverter(value ?? defaultValue());
}