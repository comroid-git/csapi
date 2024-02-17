using System.Numerics;

namespace comroid.textUI.model;

public abstract class TextInterface
{
    public abstract Vector2 Size { get; }
    public virtual void Clear(long count = 0)
    {
        for (var c = 0; c < count; c++)
            Write("\b");
    }
    public abstract void Write(string str);
    public abstract string ReadLine();
}

public class Console : TextInterface
{
    public bool OccupyEntireArea { get; }

    public override Vector2 Size => new(System.Console.WindowWidth, System.Console.WindowHeight);

    public Console(bool occupyEntireArea)
    {
        OccupyEntireArea = occupyEntireArea;
    }

    public override void Clear(long count = 0)
    {
        if (OccupyEntireArea)
            System.Console.Clear();
        else base.Clear(count);
    }
    public override void Write(string str) => System.Console.Write(str);
    public override string ReadLine() => System.Console.ReadLine() ?? throw new Exception("Could not read line");
}
