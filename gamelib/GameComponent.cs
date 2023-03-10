using System.Numerics;
using comroid.common;

namespace comroid.gamelib;

public abstract class GameComponent : Container<IGameComponent>, IGameComponent
{
    public ITransform Transform { get; }
    public bool Loaded { get; private set; }
    public bool Enabled { get; private set; }
    public Vector3 Position
    {
        get => Transform.Position;
        set => Transform.Position = value;
    }
    public Vector3 Scale
    {
        get => Transform.Scale;
        set => Transform.Scale = value;
    }
    public Quaternion Rotation
    {
        get => Transform.Rotation;
        set => Transform.Rotation = value;
    }

    protected GameComponent(ITransform transform = null!)
    {
        Transform = transform ?? Singularity.Default();
    }

    private bool Everything(Func<GameComponent, bool> func) => this.CastOrSkip<IDisposable, GameComponent>().All(func); 
    public virtual bool Load() => Everything(x => x.Load()) && !Loaded && (Loaded = true);
    public virtual bool Enable() => Everything(x => x.Enable()) && !Enabled && (Enabled = true);
    public virtual bool Tick() => Everything(x => x.Tick()) || true;
    public virtual bool Disable() => Everything(x => x.Disable()) && Enabled && !(Enabled = false);
    public virtual bool Unload() => Everything(x => x.Unload()) && Loaded && !(Loaded = false);

    public void Run()
    {
        // call Dispose() after method ends
        using var x = this;
        
        if (!(Load() && Enable()))
            Log<GameComponent>.At(LogLevel.Fatal, $"Could not initialize {this} [{Loaded}/{Enabled}]");
        
        // ReSharper disable once EmptyEmbeddedStatement
        while (Tick());
    }

    public override void Dispose()
    {
        if (!(Disable() && Unload()))
            Log<GameComponent>.At(LogLevel.Warning, $"Could not dispose {this} [{Loaded}/{Enabled}]");
    }
}
