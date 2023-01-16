using common.test;

namespace testapp;

public class Program
{
    public static void Main(string[] args)
    {
        var test = new ByteAdapterTest();
        
        test.Setup();
        test._1_SaveObj();
        test._2_LoadObj();
        test._3_LoadObj_Direct();
    }
}