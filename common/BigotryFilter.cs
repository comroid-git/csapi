using System;
using System.IO;
using JetBrains.Annotations;

namespace comroid.common;

public sealed class BigotryFilter
{
    [LanguageInjection("RegExp")] public static readonly string[] Separators = new[] { "/", ",", "\\", " ", "\n", "\r\n" };
    public static readonly string[] Pronouns;

    static BigotryFilter()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "comroid", "pronouns.txt");

        string content;
        if (File.Exists(path))
            content = File.ReadAllText(path);
        else
        {
            Console.Write("Please enter your preferred pronouns: ");
            content = Console.ReadLine() ?? throw new NullReferenceException("No input detected");
            File.WriteAllText(path, content);
        }
        Pronouns = content.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        Log<BigotryFilter>.Config("Found Pronouns: " + string.Join("/", Pronouns));
    }

    public static void Init()
    {
    }
}