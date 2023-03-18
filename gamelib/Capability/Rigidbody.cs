using System.Numerics;
using comroid.common;

namespace comroid.gamelib.Capability;

public class Rigidbody : GameObjectComponent
{
    public float Friction { get; set; }
    public float Bounciness { get; set; }
    public Vector3 Velocity { get; set; }
    public Vector3 MeasuredVelocity { get; set; }
    public Vector3 VelocityFactor { get; set; }
    public Vector3 PositionFactor { get; set; }
    private Vector3 PrevPos { get; set; }

    public event Action<Collision>? Collide;

    public Rigidbody(IGameObject gameObject, ITransform transform = null!) : base(gameObject, transform)
    {
    }

    public override bool EarlyUpdate()
    {
        return WithinFreeze(() =>
        {
            // measure actual velocity (e.g. for user controlled objects)
            MeasuredVelocity = PrevPos - AbsolutePosition;
            PrevPos = AbsolutePosition;
            
            // apply friction
            Velocity *= 1 - Friction * Math.Min(Game.DeltaTime, 1);

            // apply velocity
            Transform.Position += Velocity * Game.DeltaTime;

            return base.EarlyUpdate();
        });
    }

    public override bool LateUpdate()
    {
        return WithinFreeze(() =>
        {
            if (Collide != null)
                foreach (var component in Game.AllComponents())
                {
                    if (component is not ICollider other)
                        continue;
                    foreach (var any in GameObject.FindComponents<ICollider>().Where(x
                                 => (((byte)x.Channel >> 1 << 1) & ((byte)other.Channel >> 1 << 1)) != 0))
                    {
                        if (any == other)
                            continue;
                        if (any.CollidesWith2D(other, out var p, out var com))
                        {
                            var collision = new Collision(any, other, p!.Value.To3(), com!.Value.To3());
                            Collide(collision);
                            if (!collision.Cancelled)
                            {
                                Velocity = any.CalculateCollisionOutputVelocity(collision, Velocity,
                                    Bounciness * (other.GameObject.FindComponent<Rigidbody>()?.Bounciness ?? 1));
                                goto end;
                            }
                        }
                    }
                }

            end:
            return base.LateUpdate();
        });
    }

    private T WithinFreeze<T>(Func<T> action)
    {
        var pos0 = Position * PositionFactor;
        var vel0 = Velocity * VelocityFactor;

        var t = action();

        if (pos0 != default)
            Position = new Vector3(
                pos0.X == 0 ? Position.X : pos0.X,
                pos0.Y == 0 ? Position.Y : pos0.Y,
                pos0.Z == 0 ? Position.Z : pos0.Z);
        if (vel0 != default)
            Velocity = new Vector3(
                vel0.X == 0 ? Velocity.X : vel0.X,
                vel0.Y == 0 ? Velocity.Y : vel0.Y,
                vel0.Z == 0 ? Velocity.Z : vel0.Z);

        return t;
    }
}

public sealed class Collision
{
    public readonly ICollider Sender;
    public readonly ICollider CollidedWith;
    public readonly Vector3 CollisionPosition;
    public readonly Vector3 CommonPoint;
    public bool Cancelled { get; set; }
    
    public Collision(ICollider sender, ICollider collidedWith, Vector3 collisionPosition, Vector3 commonPoint)
    {
        Sender = sender;
        CollidedWith = collidedWith;
        CollisionPosition = collisionPosition;
        CommonPoint = commonPoint;
    }
}
