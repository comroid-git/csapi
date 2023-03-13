using System.Numerics;

namespace comroid.gamelib;

public class PhysicsEngine : GameObject
{
    public Vector3 Gravity { get; set; }

    public PhysicsEngine(IGameObject gameObject) : base(gameObject.Game, gameObject)
    {
    }
}