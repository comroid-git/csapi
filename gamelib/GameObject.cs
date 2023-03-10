using SFML.Graphics;

namespace comroid.gamelib;

public class GameObject : GameComponent, IGameObject
{
    public GameObject(ITransform transform) : base(transform)
    {
    }

    public void Draw(RenderWindow win)
    {
        Everything<IRenderObject>(it =>
        {
            it.Draw(win);
            return true;
        });
    }
}
