using System.Numerics;
using comroid.common;
using SFML.Graphics;

namespace comroid.gamelib;

public abstract class GameComponent : Container<IGameComponent>, IGameComponent
{
    private Channel _channel;

    public Channel Channel
    {
        get => _channel | (Parent?.Channel ?? default);
        set => _channel = value;
    }

    public string Name { get; set; }
    public IGameComponent? Parent { get; internal set; }
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
        Name = GetType().Name;
    }

    private bool RunOnAllComponents(Func<IGameComponent, bool> func, bool enabledOnly = false, bool ordered = false)
        => ((IEnumerable<IGameComponent>)(ordered ? this.OrderBy(it => it.Position.Z) : this))
            .Where(x => !enabledOnly || x.Enabled)
            .ToArray()
            .All(func); 
    public virtual bool Load() => Loaded || RunOnAllComponents(x => x.Loaded || x.Load()) && !Loaded && (Loaded = true);
    public virtual bool Enable() => Enabled || RunOnAllComponents(x => x.Enabled || x.Enable()) && !Enabled && (Enabled = true);
    public virtual bool EarlyUpdate() => RunOnAllComponents(x => x.EarlyUpdate(), true) || true /* always tick */;
    public virtual bool Update() => RunOnAllComponents(x => x.Update(), true) || true /* always tick */;
    public virtual bool LateUpdate() => RunOnAllComponents(x => x.LateUpdate(), true) || true /* always tick */;
    public virtual void Draw(RenderWindow win) => RunOnAllComponents(x =>
    {
        if ((x.Channel & Channel.Hidden) == 0)
            x.Draw(win);
        return true;
    }, true, true);
    public virtual bool Disable() => !Enabled || RunOnAllComponents(x => !x.Enabled || x.Disable()) && Enabled && !(Enabled = false);
    public virtual bool Unload() => !Loaded || RunOnAllComponents(x => !x.Loaded || x.Unload()) && Loaded && !(Loaded = false);

    public new void Add(IGameComponent component)
    {
        if ((component.Parent == null || component.Parent.Remove(component))
            && (component.Loaded || component.Load()) && component.Enable())
        {
            base.Add(component);
            ((GameComponent)component).Parent = this;
        }
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

    public R? Add<R>(string? name = null) where R : IGameComponent
    {
        try
        {
            var it = (R?)Activator.CreateInstance(typeof(R), this is not IGameObject && this is IGameObjectComponent c0 ? c0.GameObject : this, this);
            if (it != null)
            {
                it.Name = name ?? typeof(R).Name;
                Add(it);
            }
            return it;
        }
        catch (Exception e)
        {
            Log<GameComponent>.At(LogLevel.Error, $"Could not instantiate Component of type {typeof(R)}\r\n{e}");
            return (R?)(object?)null;
        }
    }
    public R? FindComponent<R>() where R : IGameComponent => FindComponents<R>().FirstOrDefault();

    public IEnumerable<R> FindComponents<R>() where R : IGameComponent => this
        .CastOrSkip<R>()
        .Concat(this.SelectMany(x => x.FindComponents<R>()));

    public IEnumerable<IGameComponent> AllComponents() => this.Concat(this.SelectMany(x => x.AllComponents()));
    public bool Destroy() => Game.Destroy(this);
}
