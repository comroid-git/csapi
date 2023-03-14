﻿using System.Numerics;
using comroid.common;

namespace comroid.gamelib.Capability;

public class Rigidbody : GameObjectComponent
{
    public float Friction { get; set; }
    public float Bounciness { get; set; }
    private readonly PhysicsEngine? physics;
    public Vector3 Velocity { get; set; }

    public event Action<Collision>? Collide;

    public Rigidbody(IGameObject gameObject, ITransform transform = null!) : base(gameObject, transform)
    {
        this.physics = gameObject.Game.FindComponent<PhysicsEngine?>();
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
        if (Collide != null)
            foreach (var component in Game.AllComponents())
            {
                if (component is not ICollider outside)
                    continue;
                foreach (var any in GameObject.FindComponents<ICollider>())
                {
                    if (any == outside)
                        continue;
                    if (any.CollidesWith2D(outside, out var p))
                    {
                        var collision = new Collision(any, outside, p!.Value.To3());
                        Collide(collision);
                        if (!collision.Cancelled)
                        {
                            Velocity = any.CalculateCollisionOutputDirection(collision, Velocity, Bounciness);
                            goto end;
                        }
                    }
                }
            }
        end:
        return base.LateUpdate();
    }
}

public sealed class Collision
{
    public readonly ICollider Sender;
    public readonly ICollider CollidedWith;
    public readonly Vector3 CollisionPosition;
    public bool Cancelled { get; set; }
    
    public Collision(ICollider sender, ICollider collidedWith, Vector3 collisionPosition)
    {
        Sender = sender;
        CollidedWith = collidedWith;
        CollisionPosition = collisionPosition;
    }
}
