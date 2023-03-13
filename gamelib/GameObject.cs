namespace comroid.gamelib;

public class GameObject : GameComponent, IGameObject
{
    public GameObject(ITransform? transform = null) : base(transform)
    {
    }
}
