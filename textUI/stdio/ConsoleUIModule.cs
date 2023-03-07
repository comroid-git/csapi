namespace comroid.textUI.stdio;

public class ConsoleUIModule : UIModule
{
    public override UICapability Capabilities => base.Capabilities | UICapability.AnsiColorizable | UICapability.Unicode;

    public override Task<string?> WaitForInputAsync(Guid? _, object? message = null)
    {
        Console.Write(PreProcessMessage(message) + "> ");
        return new Task<string?>(Console.ReadLine);
    }

    public override void WriteOutput(Guid? _, object message)
    {
        Console.WriteLine(PreProcessMessage(message));
    }
}