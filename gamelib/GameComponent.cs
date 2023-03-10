using System.Numerics;
using comroid.common;

namespace comroid.gamelib;

public abstract class GameComponent : Container<IGameComponent>, IGameComponent
{
    public ITransform Transform { get; }
    public bool Loaded { get; private set; }
    public bool Enabled { get; private set; }
    public Vector3 Position { get; set; }
    public Vector3 Scale { get; set; }
    public Quaternion Rotation { get; set; }

    public Vector3 AbsolutePosition => Transform.AbsolutePosition + Position;
    public Vector3 AbsoluteScale => Transform.AbsoluteScale * Scale;
    public Quaternion AbsoluteRotation => Transform.AbsoluteRotation * Rotation;

    protected GameComponent(ITransform transform = null!)
    {
        Transform = transform ?? Singularity.Default();
    }

    private protected bool Everything<T>(Func<T, bool> func) => this.CastOrSkip<IDisposable, T>().All(func); 
    public virtual bool Load() => Everything<IGameComponent>(x => x.Load()) && !Loaded && (Loaded = true);
    public virtual bool Enable() => Everything<IGameComponent>(x => x.Enable()) && !Enabled && (Enabled = true);
    public virtual bool Tick() => Everything<IGameComponent>(x => x.Tick()) || true /* always tick */;
    public virtual bool Disable() => Everything<IGameComponent>(x => x.Disable()) && Enabled && !(Enabled = false);
    public virtual bool Unload() => Everything<IGameComponent>(x => x.Unload()) && Loaded && !(Loaded = false);

    public override void Dispose()
    {
        if (!(Disable() && Unload()))
            Log<GameComponent>.At(LogLevel.Warning, $"Could not dispose {this} [{Loaded}/{Enabled}]");
    }
}
