using comroid.common;

namespace testapp;

public class Program
{
    public static void Main(string[] args)
    {
        LogTest(DetailLevel.None);
    }
    
    private static void LogTest(DetailLevel none)
    {
        Log<Program>.Fatal("");
    }
}