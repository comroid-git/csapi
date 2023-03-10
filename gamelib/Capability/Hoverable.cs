namespace comroid.gamelib.Capability;

public class Hoverable : GameObjectComponent
{
    private readonly IGameObjectComponent _target;
    public bool Hovering { get; private set; }
    public event Action<IGameObjectComponent>? HoverBegin;
    public event Action<IGameObjectComponent>? HoverEnd;
    
    public Hoverable(IGameObjectComponent target, ITransform transform = null!) : base(target.GameObject, transform)
    {
        _target = target;
    }

    public override bool Enable()
    {
        HoverEnd?.Invoke(_target);
        return base.Enable();
    }

    public override bool EarlyUpdate()
    {
        var pre = Hovering;
        Hovering = _target.FindChildren<ICollider>().Any(it => it.IsPointInside(Input.MousePosition));
        ((pre, Hovering) switch
        {
            (false, true) => HoverBegin,
            (true, false) => HoverEnd,
            _ => null
        })?.Invoke(_target);
        return base.EarlyUpdate();
    }
}