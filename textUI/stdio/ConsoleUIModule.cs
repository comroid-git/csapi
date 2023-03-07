namespace comroid.textUI.stdio;

public class ConsoleUIModule : UIModule
{
    public override string? WaitForInput(Guid? _, string? message = null)
    {
        Console.Write(message + "> ");
        return Console.ReadLine();
    }

    public override void WriteOutput(Guid? _, object message)
    {
        Console.WriteLine(message);
    }
}