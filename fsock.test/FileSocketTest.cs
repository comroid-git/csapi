using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using comroid.common;
using comroid.fsock;
using NUnit.Framework;

namespace fsock.test;

[NonParallelizable]
public class Tests
{
    private string Path;
    private FileSocket receiver, sender;

    [SetUp]
    public void Setup()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        receiver = new FileSocket(Path, true);
        sender = new FileSocket(Path);
    }

    [TearDown]
    public void TearDown()
    {
        receiver.Dispose();
        sender.Dispose();
    }

    [Test(TestOf = typeof(FileSocket))]
    public void Test_Seq_1()
    {
        var testString = Guid.NewGuid().ToString();
        
        sender.Write(testString);
        var read = receiver.ReadLine();
        
        Assert.AreEqual(testString, read, "invalid data received");
    }

    [Test(TestOf = typeof(FileSocket))]
    public void Test_Par_1()
    {
        var testString = Guid.NewGuid().ToString();
        var read = string.Empty;

        void HandleData(string data) => read = data;

        receiver.LineReceived += HandleData;
        using var rcv = receiver.StartReceiving();
        
        sender.Write(testString);
        
        Thread.Sleep(50);
        Assert.AreEqual(testString, read, "invalid data received");
    }
}