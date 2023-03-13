namespace comroid.gamelib;

public abstract class GameObjectComponent : GameComponent
{
    public IGameObject GameObject { get; }

    protected GameObjectComponent(IGameObject gameObject, ITransform transform = null!) : base(gameObject.Game, transform ?? gameObject)
    {
        this.GameObject = gameObject;
    }
}