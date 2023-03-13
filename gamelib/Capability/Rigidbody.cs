using System.Numerics;

namespace comroid.gamelib.Capability;

public class Rigidbody : GameObjectComponent
{
    public float Friction { get; set; }
    public float Bounciness { get; set; }
    private PhysicsEngine? physics => Game.FindComponent<PhysicsEngine>();
    public Vector3 Velocity { get; set; }

    public event Action<ICollider>? Collide;

    public Rigidbody(IGameObject gameObject, ITransform transform = null!) : base(gameObject, transform)
    {
    }

    public override bool EarlyUpdate()
    {
        if (physics != null)
        {
            // todo: untested
            // adjust velocity to gravity
            var dotProduct = Vector3.Dot(Velocity, physics.Gravity);
            var magnitudeA = Velocity.Length();
            var magnitudeB = physics.Gravity.Length();
            var grade = dotProduct / (magnitudeA * magnitudeB);
            Velocity = Vector3.Lerp(Velocity, physics.Gravity, grade);
        }
        // apply friction
        Velocity *= 1 - Friction * Game.DeltaTime;

        // apply velocity
        Transform.Position += Velocity * Game.DeltaTime;
        
        return base.EarlyUpdate();
    }

    public override bool LateUpdate()
    {
        if (Collide != null)
            foreach (var colliding in Game.FindComponents<ICollider>()
                         .Where(x => !FindComponents<ICollider>().Any(x.Equals))
                         .Where(x => FindComponents<ICollider>().Any(x.CollidesWith2D)))
                Collide(colliding);
        return base.LateUpdate();
    }
}