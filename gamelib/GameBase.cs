using comroid.common;
using SFML.Graphics;
using SFML.Window;

namespace comroid.gamelib;

public abstract class GameBase : GameObject
{
    public Color Background { get; set; } = Color.Black;

    protected GameBase() : base(Singularity.Default())
    {
    }

    public void Run(RenderWindow win = null!)
    {
        win ??= new RenderWindow(VideoMode.DesktopMode, GetType().Name);

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

                Draw(win);
                win.Display();
            }
    }
}
