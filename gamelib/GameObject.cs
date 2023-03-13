namespace comroid.gamelib;

public class GameObject : GameComponent, IGameObject
{
    public GameObject(GameBase game, ITransform? transform = null) : base(game, transform)
    {
    }
}
