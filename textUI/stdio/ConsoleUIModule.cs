namespace comroid.textUI.stdio;

public class ConsoleUIModule : UIModule
{
    public override UICapability Capabilities => base.Capabilities | UICapability.AnsiColorizable | UICapability.Unicode;

    public override Task<string?> WaitForInputAsync(Guid? _, object? message = null)
    {
        Console.Write(PreProcessMessage(message) + "> ");
        var txt = Console.ReadLine();
        var task = new TaskCompletionSource<string?>();
        task.SetResult(txt);
        return task.Task;
    }

    public override void WriteOutput(Guid? _, object message)
    {
        Console.WriteLine(PreProcessMessage(message));
    }
}