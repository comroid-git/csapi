namespace comroid.gamelib;

public abstract class GameObjectComponent : GameComponent
{
    public IGameObject GameObject { get; }

    protected GameObjectComponent(IGameObject gameObject, ITransform transform = null!) : base(transform ?? gameObject)
    {
        this.GameObject = gameObject;
    }
}