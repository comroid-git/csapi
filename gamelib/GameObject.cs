namespace comroid.gamelib;

public class GameObject : GameComponent, IGameObject
{
    public ISet<IRenderObject> RenderObjects { get; } = new HashSet<IRenderObject>();

    public GameObject(ITransform transform) : base(transform)
    {
    }

    public void Draw()
    {
        foreach (var it in RenderObjects)
            it.Draw();
    }
}
