namespace comroid.gamelib;

public abstract class RenderObjectBase : GameComponent, IRenderObject
{
    public IGameObject GameObject { get; }

    protected RenderObjectBase(IGameObject gameObject, ITransform transform) : base(transform)
    {
        GameObject = gameObject;
    }

    public abstract void Draw();
}
