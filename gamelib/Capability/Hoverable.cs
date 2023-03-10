namespace comroid.gamelib.Capability;

public class Hoverable : GameObjectComponent
{
    public bool Hovering { get; private set; }
    public event Action<IGameObject>? HoverBegin;
    public event Action<IGameObject>? HoverEnd;
    
    public Hoverable(IGameObject gameObject, ITransform transform = null!) : base(gameObject, transform)
    {
    }

    public override bool Enable()
    {
        HoverEnd?.Invoke(GameObject);
        return base.Enable();
    }

    public override bool Tick()
    {
        var pre = Hovering;
        Hovering = GameObject.FindChildren<ICollider>().Any(it => it.IsPointInside(Input.MousePosition));
        ((pre, Hovering) switch
        {
            (false, true) => HoverBegin,
            (true, false) => HoverEnd,
            _ => null
        })?.Invoke(GameObject);
        return base.Tick();
    }
}