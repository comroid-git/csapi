using System.Numerics;
using comroid.common;
using SFML.Graphics;
using BindingFlags = System.Reflection.BindingFlags;

namespace comroid.gamelib;

public abstract class GameComponent : Container<IGameComponent>, IGameComponent
{
    public virtual GameBase Game { get; }
    public ITransform Transform { get; }
    public bool Loaded { get; private set; }
    public bool Enabled { get; private set; }
    public Vector3 Position { get; set; } = Singularity.DefaultPosition;
    public Vector3 Scale { get; set; } = Singularity.DefaultScale;
    public Quaternion Rotation { get; set; } = Singularity.DefaultRotation;

    public Vector3 AbsolutePosition => Transform.AbsolutePosition + Position;
    public Vector3 AbsoluteScale => Transform.AbsoluteScale * Scale;
    public Quaternion AbsoluteRotation => Transform.AbsoluteRotation * Rotation;

    protected GameComponent(GameBase game, ITransform? transform = null)
    {
        Game = game;
        Transform = transform ?? Singularity.Default();
    }

    private bool Everything(Func<IGameComponent, bool> func)
        => this.OrderBy(it => it.Position.Z).CastOrSkip<IDisposable, IGameComponent>().All(func); 
    public virtual bool Load() => Everything(x => x.Loaded || x.Load()) && !Loaded && (Loaded = true);
    public virtual bool Enable() => Everything(x => x.Enabled || x.Enable()) && !Enabled && (Enabled = true);
    public virtual bool EarlyUpdate() => Everything(x => x.EarlyUpdate()) || true /* always tick */;
    public virtual bool Update() => Everything(x => x.Update()) || true /* always tick */;
    public virtual bool LateUpdate() => Everything(x => x.LateUpdate()) || true /* always tick */;
    public virtual void Draw(RenderWindow win) => Everything(x =>
    {
        x.Draw(win);
        return true;
    });
    public virtual bool Disable() => Everything(x => !x.Enabled || x.Disable()) && Enabled && !(Enabled = false);
    public virtual bool Unload() => Everything(x => !x.Loaded || x.Unload()) && Loaded && !(Loaded = false);

    public new void Add(IGameComponent component)
    {
        if ((component.Loaded || component.Load()) && component.Enable())
            base.Add(component);
        else Log<GameComponent>.At(LogLevel.Warning, $"Could not add {component} to {this} [{component.Loaded}]");
    }

    public new bool Remove(IGameComponent component)
    {
        if (Contains(component) && (!component.Enabled || component.Disable()))
            return base.Remove(component);
        else Log<GameComponent>.At(LogLevel.Warning, $"Could not remove {component} from {this}");
        return false;
    }

    public override void Dispose()
    {
        if (!(Disable() && Unload()))
            Log<GameComponent>.At(LogLevel.Warning, $"Could not dispose {this} [{Loaded}/{Enabled}]");
    }

    public R? Add<R>() where R : IGameComponent
    {
        try
        {
            var ctor = new[]
                {
                    new[] { typeof(IGameObject) },
                    new[] { typeof(IGameComponent) },
                    new[] { typeof(IGameObject), typeof(ITransform) },
                    new[] { typeof(IGameComponent), typeof(ITransform) },
                    new[] { typeof(IGameObject), GetType() },
                    new[] { typeof(IGameComponent), GetType() },
                }
                .Select(t =>
                {
                    try
                    {
                        return typeof(R).GetConstructor(t);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .FirstOrDefault(x => x != null);
            var it = (R?)ctor?.Invoke(parameters: ctor.GetParameters().Length == 2
                ? new object[] { this is not IGameObject && this is IGameObjectComponent c1 ? c1.GameObject : this, this }
                : new object[] { this is not IGameObject && this is IGameObjectComponent c2 ? c2.GameObject : this });
            if (it != null)
                Add(it);
            return it;
        }
        catch (Exception e)
        {
            Log<GameComponent>.At(LogLevel.Error, $"Could not instantiate Component of type {typeof(R)}\r\n{e}");
            return (R?)(object?)null;
        }
    }
    public R? FindComponent<R>() => FindComponents<R>().FirstOrDefault();
    public IEnumerable<R> FindComponents<R>() => this.CastOrSkip<IGameComponent, R>()
        .Concat(this.SelectMany(x => x.FindComponents<R>()));
}
