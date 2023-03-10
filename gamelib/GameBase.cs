using System.Numerics;
using System.Runtime.InteropServices.ComTypes;
using comroid.common;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace comroid.gamelib;

public abstract class GameBase : GameObject
{
    private readonly RenderWindow Window;
    public Color Background { get; set; } = Color.Black;
    protected readonly Text FrameInfo;
    protected readonly Circle Crosshair;
    private float frameTime, tickTime;

    protected GameBase(RenderWindow window = null!) : base(Singularity.Default())
    {
        this.Window = window ?? new RenderWindow(VideoMode.DesktopMode, GetType().Name);
        Window.Closed += (_, _) => Window.Close();
        Window.Resized += (_, e) =>
        {
            var size = new Vector2f(e.Width, e.Height);
            Window.SetView(new View(size / 2, size));
        };
        Input.CreateHandlers(Window);
        
        Add(FrameInfo = new(this) { FontSize = 12 });
        Add(Crosshair = new(this) { Radius = 5 });
    }

    public override bool Update()
    {
        // debug code
        Crosshair.Position = Input.MousePosition.To3() - Vector3.One * Crosshair.Radius;

        FrameInfo.Value = $"Frame: {frameTime:0.###}ms\n Tick: {tickTime:0.###}ms\n  UPS: {(int)(1000 / (frameTime + tickTime))}";
        var success = false;
        tickTime = (float)DebugUtil.Measure(() => success = base.Update()) / 1000;
        return success;
    }

    public override bool LateUpdate()
    {
        Input.LateUpdate();
        return base.LateUpdate();
    }

    public void Run()
    {
        // register for calling this.Dispose() after method ends
        using var _ = this;

        if (!(Load() && Enable()))
            Log<GameBase>.At(LogLevel.Fatal, $"Could not initialize {this} [{Loaded}/{Enabled}]");
        // ReSharper disable once EmptyEmbeddedStatement
        else
            while (Window.IsOpen && EarlyUpdate() && Update() && LateUpdate())
            {
                Window.DispatchEvents();
                
                Window.Clear(Background);
                frameTime = (float)DebugUtil.Measure(() =>
                {
                    Draw(Window);
                    Window.Display();
                }) / 1000;
            }
        
        Window.Close();
    }
}
