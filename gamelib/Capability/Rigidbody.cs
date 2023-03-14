using System.Numerics;
using comroid.common;

namespace comroid.gamelib.Capability;

public abstract class Collision<TVector>
{
    public readonly IGameObject Sender;
    public readonly IGameObject CollidedWith;
    public readonly TVector CollisionPosition;
    public Collision(IGameObject sender, IGameObject collidedWith, TVector collisionPosition)
    {
        Sender = sender;
        CollidedWith = collidedWith;
        CollisionPosition = collisionPosition;
    }
}

public sealed class Collision2 : Collision<Vector2>
{
    public Collision2(IGameObject sender, IGameObject collidedWith, Vector2 collisionPosition) : base(sender, collidedWith, collisionPosition)
    {
    }
}

public sealed class Collision3 : Collision<Vector3>
{
    public Collision3(IGameObject sender, IGameObject collidedWith, Vector3 collisionPosition) : base(sender,
        collidedWith, collisionPosition)
    {
    }
}

public class Rigidbody : GameObjectComponent
{
    public float Friction { get; set; }
    public float Bounciness { get; set; }
    private readonly PhysicsEngine? physics;
    public Vector3 Velocity { get; set; }

    public event Action<Collision2>? Collide2;
    public event Action<Collision3>? Collide3;

    public Rigidbody(IGameObject gameObject, ITransform transform = null!) : base(gameObject, transform)
    {
        this.physics = gameObject.Game.FindComponent<PhysicsEngine>();
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
        Velocity *= 1 - Friction * Math.Min(Game.DeltaTime, 1);

        // apply velocity
        Transform.Position += Velocity * Game.DeltaTime;
        
        return base.EarlyUpdate();
    }

    public override bool LateUpdate()
    {
        if (Collide2 != null)
            foreach (var component in Game.AllComponents())
            {
                if (component is not ICollider outside)
                    continue;
                foreach (var any in GameObject.FindComponents<ICollider>())
                {
                    if (any == outside)
                        continue;
                    if (any.CollidesWith2D(outside, out var p))
                        Collide2(new Collision2(GameObject, outside.GameObject, p!.Value));
                }
            }
        if (Collide3 != null)
            Log<Rigidbody>.At(LogLevel.Warning, "3D Collisions are not supported");
        return base.LateUpdate();
    }
}