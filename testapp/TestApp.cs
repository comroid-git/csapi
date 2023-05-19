using comroid.common;

namespace testapp;

public class TestApp
{
    public static void Main(string[] args)
    {
        LogTest();
    }
    
    private static void LogTest()
    {
        Log<TestApp>.Fatal("This is a Fatal message; current LOD: " + ILog.Detail, "This is a Fatal detail");
        Log<TestApp>.Error("This is a Error message; current LOD: " + ILog.Detail, "This is a Error detail");
        Log<TestApp>.Warning("This is a Warning message; current LOD: " + ILog.Detail, "This is a Warning detail");
        Log<TestApp>.Info("This is a Info message; current LOD: " + ILog.Detail, "This is a Info detail");
        Log<TestApp>.Config("This is a Config message; current LOD: " + ILog.Detail, "This is a Config detail");
        Log<TestApp>.Debug("This is a Debug message; current LOD: " + ILog.Detail, "This is a Debug detail");
        Log<TestApp>.Trace("This is a Trace message; current LOD: " + ILog.Detail, "This is a Trace detail");
    }
}