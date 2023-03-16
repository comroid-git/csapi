using System.Numerics;
using comroid.gamelib.Capability;

namespace comroid.gamelib;

public class PhysicsEngine : GameObject
{
    public Vector3 Gravity { get; set; }

    public PhysicsEngine(IGameObject gameObject) : base(gameObject.Game, gameObject)
    {
    }

    public override bool EarlyUpdate()
    {
        foreach (var rigidbody in Game.FindComponents<Rigidbody>().Where(x=>x.Enabled))
        {
            var velocity = rigidbody.Velocity;

            // adjust velocity to gravity
            var dotProduct = Vector3.Dot(velocity, Gravity);
            var magnitudeA = velocity.Length();
            var magnitudeB = Gravity.Length();
            var grade = dotProduct / (magnitudeA * magnitudeB);
            rigidbody.Velocity = Vector3.Lerp(velocity, Gravity, grade);
        }

        return base.EarlyUpdate();
    }
}