using comroid.common;
using SFML.Graphics;
using SFML.Window;

namespace comroid.gamelib;

public abstract class GameBase : GameObject
{
    public Color Background { get; set; } = Color.Black;
    protected readonly Text FrameInfo;
    private float frameTime, tickTime;

    protected GameBase() : base(Singularity.Default())
    {
        Add(FrameInfo = new(this) { FontSize = 12 });
    }

    public override bool Tick()
    {
        FrameInfo.Value = $"Frame: {frameTime:0.###}ms\n Tick: {tickTime:0.###}ms\n  UPS: {(int)(1000 / (frameTime + tickTime))}";
        var success = false;
        tickTime = (float)DebugUtil.Measure(() => success = base.Tick()) / 1000;
        return success;
    }

    public void Run(RenderWindow win = null!)
    {
        win ??= new RenderWindow(VideoMode.DesktopMode, GetType().Name);

        win.Closed += (_, _) => win.Close();

        // register for calling this.Dispose() after method ends
        using var _ = this;

        if (!(Load() && Enable()))
            Log<GameBase>.At(LogLevel.Fatal, $"Could not initialize {this} [{Loaded}/{Enabled}]");
        // ReSharper disable once EmptyEmbeddedStatement
        else
            while (win.IsOpen && Tick())
            {
                win.DispatchEvents();
                
                win.Clear(Background);
                frameTime = (float)DebugUtil.Measure(() =>
                {
                    Draw(win);
                    win.Display();
                }) / 1000;
            }
        
        win.Close();
    }
}
