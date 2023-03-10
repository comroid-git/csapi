using System.Numerics;
using comroid.common;
using SFML.Graphics;

namespace comroid.gamelib;

public abstract class GameComponent : Container<IGameComponent>, IGameComponent
{
    public ITransform Transform { get; }
    public bool Loaded { get; private set; }
    public bool Enabled { get; private set; }
    public Vector3 Position { get; set; } = Singularity.DefaultPosition;
    public Vector3 Scale { get; set; } = Singularity.DefaultScale;
    public Quaternion Rotation { get; set; } = Singularity.DefaultRotation;

    public Vector3 AbsolutePosition => Transform.AbsolutePosition + Position;
    public Vector3 AbsoluteScale => Transform.AbsoluteScale * Scale;
    public Quaternion AbsoluteRotation => Transform.AbsoluteRotation * Rotation;

    protected GameComponent(ITransform transform = null!)
    {
        Transform = transform ?? Singularity.Default();
    }

    private protected bool Everything(Func<IGameComponent, bool> func) => this.CastOrSkip<IDisposable, IGameComponent>().All(func); 
    public virtual bool Load() => Everything(x => x.Load()) && !Loaded && (Loaded = true);
    public virtual bool Enable() => Everything(x => x.Enable()) && !Enabled && (Enabled = true);
    public virtual bool Tick() => Everything(x => x.Tick()) || true /* always tick */;
    public virtual void Draw(RenderWindow win) => Everything(x =>
    {
        x.Draw(win);
        return true;
    });
    public virtual bool Disable() => Everything(x => x.Disable()) && Enabled && !(Enabled = false);
    public virtual bool Unload() => Everything(x => x.Unload()) && Loaded && !(Loaded = false);

    public override void Dispose()
    {
        if (!(Disable() && Unload()))
            Log<GameComponent>.At(LogLevel.Warning, $"Could not dispose {this} [{Loaded}/{Enabled}]");
    }
}
