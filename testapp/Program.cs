using common.test;

namespace testapp;

public class Program
{
    public static void Main(string[] args)
    {
        var test = new ByteAdapterTest();
        
        test.Setup();
        test._1_SaveObj();
        test.teardown();
        test.Setup();
        test._2_LoadObj();
        test.teardown();
        test.Setup();
        test._3_LoadObj_Direct();
        test.teardown();
    }
}