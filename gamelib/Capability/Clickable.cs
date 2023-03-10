using SFML.Window;

namespace comroid.gamelib.Capability;

public class Clickable : GameObjectComponent
{
    private readonly IGameObjectComponent _target;
    public bool Clicking { get; private set; }
    public event Action<IGameObjectComponent>? Click;

    public Clickable(IGameObjectComponent target, ITransform transform = null!) : base(target.GameObject, transform)
    {
        _target = target;
    }

    public override bool EarlyUpdate()
    {
        var state = Input.GetKey(Mouse.Button.Left);
        Clicking = state && _target.FindChildren<ICollider>().Any(it => it.IsPointInside(Input.MousePosition));
        if (state.Pressed)
            Click?.Invoke(_target);
        return base.EarlyUpdate();
    }
}