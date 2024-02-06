using System.Numerics;
using comroid.common;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace comroid.gamelib;

public abstract class GameBase : GameObject
{
    protected readonly Log log;
    public override GameBase Game => this;
    public readonly RenderWindow Window;
    public readonly WindowCamera MainCamera;
    protected readonly Thread UpdateThread;
    public ICamera Camera { get; set; }
    public Color Background { get; set; } = Color.Black;
    public float DeltaTime { get; private set; }
#if DEBUG
    protected readonly Text FrameInfo;
    protected readonly Circle Crosshair;
#endif
    private float frameTime, tickTime;

    protected GameBase(RenderWindow? window = null!) : base(null!, Singularity.Default())
    {
        log = new Log(GetType());
        Window = window ?? new RenderWindow(VideoMode.DesktopMode, GetType().Name);
        Camera = MainCamera = new WindowCamera(Window);
        UpdateThread = new Thread(RunUpdate);

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
        FrameInfo.Value = $"Frame: {frameTime:0.000}ms ({(int)(1000/frameTime)} FPS)\n Tick: {tickTime:0.000}ms ({(int)(1000 / tickTime)} TPS)";
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
        DeltaTime = tickTime + frameTime;
        return success;
    }

    public override bool LateUpdate()
    {
        Input.LateUpdate();
        return base.LateUpdate();
    }

    public override bool Unload()
    {
        Window.Close();
        return base.Unload();
    }

    public void Run()
    {
        // register for calling this.Dispose() after method ends
        using var _ = this;
        UpdateThread.Start();
        RunWindow();
        UpdateThread.Join();
    }

    private void RunWindow()
    {
        if (!(Load() && Enable()))
            Log<GameBase>.At(LogLevel.Fatal, $"Could not initialize {this} [{Loaded}/{Enabled}]");
        // ReSharper disable once EmptyEmbeddedStatement
        else
            while (Window.IsOpen)
            {
                Window.DispatchEvents();
                Window.Clear(Background);
                
                frameTime = (float)DebugUtil.Measure(() =>
                {
                    Draw(Window);
                    Window.Display();
                }) / 1000;
            }
    }

    private void RunUpdate()
    {
        while (Window.IsOpen)
        {
            bool u1 = false, u2 = false;
            if (!EarlyUpdate() || !(u1 = Update()) || !(u2 = LateUpdate()))
                log.At(LogLevel.Error, $"Could not finish Update normally; stopped at {(u2 ? "LateUpdate" : u1 ? "Update" : "EarlyUpdate")}");
        }
    }

    private void SetView(Vector3 camPos, Vector2f size) => Window.SetView(new View(camPos.To2f(), size));

    public bool Destroy(IGameComponent gameComponent)
    {
        return gameComponent.Disable() 
               && gameComponent.Unload() 
               && (gameComponent.Parent?.Remove(gameComponent) ?? true);
    }
}
