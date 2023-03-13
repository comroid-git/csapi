using System.Numerics;
using comroid.common;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace comroid.gamelib;

public abstract class GameBase : GameObject
{
    public override GameBase Game => this;
    public readonly RenderWindow Window;
    public readonly WindowCamera MainCamera;
    public ICamera Camera { get; set; }
    public Color Background { get; set; } = Color.Black;
#if DEBUG
    protected readonly Text FrameInfo;
    protected readonly Circle Crosshair;
#endif
    private float frameTime, tickTime;

    protected GameBase(RenderWindow window = null!) : base(null!, Singularity.Default())
    {

        this.Window = window ?? new RenderWindow(VideoMode.DesktopMode, GetType().Name);
        this.Camera = MainCamera = new WindowCamera(Window);

        Window.Closed += (_, _) => Window.Close();
        Window.Resized += (_, e) => SetView(Camera.Position, new Vector2f(e.Width, e.Height));
        Input.Initialize(this, Window);

#if DEBUG
        Add(Crosshair = new(this) { Radius = 5, Delegate = { OutlineColor = Color.Black, OutlineThickness = 3}});
        Add(FrameInfo = new(this) { FontSize = 12 });
#endif
    }

    public override bool Update()
    {
        var view = Window.GetView();
        SetView(Camera.Position, view.Size);
        
#if DEBUG
        FrameInfo.Position = (view.Center - view.Size / 2).To3(float.MaxValue);
        FrameInfo.Value = $"Frame: {frameTime:0.000}ms\n Tick: {tickTime:0.000}ms\n  UPS: {(int)(1000 / (frameTime + tickTime))}";
        Crosshair.Position = Input.MousePosition.To3(float.MaxValue);
        if (Input.MouseButton.Values.Any(x => x.Down))
        {
            Crosshair.Color = Color.Black;
            Crosshair.Delegate.OutlineColor = Color.White;
        }
        else
        {
            Crosshair.Color = Color.White;
            Crosshair.Delegate.OutlineColor = Color.Black;
        }
#endif
        
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
            while (Window.IsOpen)
            {
                if (!EarlyUpdate())
                    break;
                
                Window.DispatchEvents();
                
                if (!Update())
                    break;
                
                Window.Clear(Background);
                frameTime = (float)DebugUtil.Measure(() =>
                {
                    Draw(Window);
                    Window.Display();
                }) / 1000;
                
                if (!LateUpdate())
                    break;
            }
        
        Window.Close();
    }

    private void SetView(Vector3 camPos, Vector2f size) => Window.SetView(new View(camPos.To2f(), size));
}
