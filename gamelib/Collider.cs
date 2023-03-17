using System.Numerics;
using comroid.gamelib.Capability;
using SFML.System;

namespace comroid.gamelib;

public abstract class ColliderBase : GameObjectComponent, ICollider
{
    public bool Inverse { get; set; }
    
    protected ColliderBase(IGameObject gameObject, ITransform transform = null!) : base(gameObject, transform)
    {
    }

    public bool CollidesWith2D(ICollider other, out Vector2? point, out Vector2? commonPoint)
    {
        var inv = Inverse || other.Inverse;
        var res = (inv
                ? ArraySegment<((Vector2, Vector2), ICollider, ICollider)>.Empty
                : GetBoundary2D().Select(p => (p, it:(ICollider)this, vs: other)))
            .Concat(other.GetBoundary2D().Select(p => (p, it:other, vs: (ICollider)this)))
            .SelectMany(ProjectPath)
            .Where(any =>
            {
                if (inv)
                    if (any.vs.IsPointInside(any.p.point))
                        return true;
                    else return false;
                else
                    return any.vs.IsPointInside(any.p.point);
            })
            .Select(x => (ValueTuple<Vector2, Vector2>?)x.p)
            .FirstOrDefault(defaultValue: null);
        (point, commonPoint) = (res?.Item1, res?.Item2);
        return res != null;
    }

    private IEnumerable<((Vector2 point, Vector2 inside) p, ICollider vs)> ProjectPath(
        ((Vector2 point, Vector2 inside) p, ICollider it, ICollider vs) arg)
    {
        yield return ((arg.p.point, arg.p.inside), arg.vs);
        if (arg.it.FindComponent<Rigidbody>() is { } rigidbody)
        {
            var velocity = rigidbody.Velocity;
            var iterations = (int)Math.Ceiling(velocity.Length());
            for (var i = 1; i < iterations; i++)
                yield return ((arg.p.point + velocity.To2() * Game.DeltaTime * (i / 2), arg.p.inside), arg.vs);
        }
    }

    public abstract IEnumerable<(Vector2 point, Vector2 inside)> GetBoundary2D();
    public abstract bool IsPointInside(Vector2 p);
    public abstract bool IsPointInside(Vector3 p);
    public abstract Vector3 CalculateCollisionOutputVelocity(Collision collision, Vector3 velocity, float bounciness);
}

public partial class Circle
{
    public class Collider : ColliderBase
    {
        private readonly Circle _circle;

        public Collider(IGameObject gameObject, Circle circle) : base(gameObject, circle)
        {
            _circle = circle;
        }

        private float radius => _circle.Radius * Scale.X;

        public override IEnumerable<(Vector2 point, Vector2 inside)> GetBoundary2D()
        {
            const int segments = 12;
            for (var i = 1; i < segments; i++)
            {
                var off = new Vector2f(MathF.Sin(2 * MathF.PI * i / segments),
                                       MathF.Cos(2 * MathF.PI * i / segments)) * radius;
                yield return (_circle.GetDelegateTransformData_Position().To2() + off.To2(), _circle.GetDelegateTransformData_Position().To2());
            }
        }

        public override bool IsPointInside(Vector2 p) => Vector2.Distance(_circle.GetDelegateTransformData_Position().To2(), p) < radius;
        public override bool IsPointInside(Vector3 p) => Vector3.Distance(_circle.GetDelegateTransformData_Position().To3(), p) < radius;
        public override Vector3 CalculateCollisionOutputVelocity(Collision collision, Vector3 velocity, float bounciness)
        {
            var velocityMagnitude = velocity.Length();
            if (velocityMagnitude == 0)
                return velocity;
            velocity = Vector3.Normalize(velocity);

            var me = collision.Sender.AbsolutePosition;
            var other = collision.CollidedWith.AbsolutePosition;
            var at = collision.CollisionPosition;
            var com = collision.CommonPoint;
            var rel = at-com;
        
            // this part brought to you by ChatGPT
// Get the cross product of A and B
            Vector3 r = Vector3.Cross(velocity, rel);

// Get the angle between A and B
            float theta = MathF.Acos(Vector3.Dot(Vector3.Normalize(velocity), Vector3.Normalize(rel)));
            //float delta = MathF.Acos(Vector3.Dot(Vector3.Normalize(at), Vector3.Normalize(com)));
            //theta -= delta;

// Calculate the quaternion
            Quaternion rotation = new Quaternion(
                MathF.Sin(theta) * r.X,
                MathF.Sin(theta) * r.Y,
                MathF.Sin(theta) * r.Z,
                MathF.Cos(theta)
            );
            return Vector3.Transform(velocity, rotation) * bounciness * velocityMagnitude;
        }
    }
}

public partial class Rect
{
    public class Collider : ColliderBase
    {
        private readonly Rect _rect;

        public Collider(IGameObject gameObject, Rect rect) : base(gameObject, rect)
        {
            _rect = rect;
        }

        private Vector2 size => _rect.Size.To2() * _rect.GetDelegateTransformData_Scale().To2();

        public override IEnumerable<(Vector2 point, Vector2 inside)> GetBoundary2D()
        {
            var inside = Inverse ? -1 : 1;
            var left = _rect.GetDelegateTransformData_Position().X;
            var right = _rect.GetDelegateTransformData_Position().X + size.X;
            var top = _rect.GetDelegateTransformData_Position().Y;
            var bottom = _rect.GetDelegateTransformData_Position().Y + size.Y;
            for (var h = left; h <= right; h += size.X * (5 / size.X))
            {
                yield return (new Vector2(h, top),new Vector2(h,top+inside));
                yield return (new Vector2(h, bottom),new Vector2(h,bottom-inside));
            }
            for (var v = top; v <= bottom; v += size.Y * (5 / size.Y))
            {
                yield return (new Vector2(left, v),new Vector2(left+inside,v));
                yield return (new Vector2(right, v),new Vector2(right-inside,v));
            }
        }

        // this method brought to you by ChatGPT
        public override bool IsPointInside(Vector2 point)
        {
            // Calculate the half width and half height of the rectangle
            //var halfWidth = _rect.Size.X / 2f;
            //var halfHeight = _rect.Size.Y / 2f;

            // Calculate the left, right, top, and bottom edges of the rectangle
            var left = _rect.GetDelegateTransformData_Position().X;
            var right = _rect.GetDelegateTransformData_Position().X + size.X;
            var top = _rect.GetDelegateTransformData_Position().Y;
            var bottom = _rect.GetDelegateTransformData_Position().Y + size.Y;

            // Check if the point is inside the rectangle
            return point.X > left && point.X < right && point.Y > top && point.Y < bottom;
        }

        // this method brought to you by ChatGPT
        public override bool IsPointInside(Vector3 point)
        {
            // Calculate the half width, half height, and half depth of the prism
            //var halfWidth = _rect.Size.X / 2f;
            //var halfHeight = _rect.Size.Y / 2f;
            //var halfDepth = AbsoluteScale.Z / 2f;

            // Calculate the left, right, top, bottom, front, and back planes of the prism
            var left = _rect.GetDelegateTransformData_Position().X;
            var right = _rect.GetDelegateTransformData_Position().X + size.X;
            var top = _rect.GetDelegateTransformData_Position().Y;
            var bottom = _rect.GetDelegateTransformData_Position().Y + size.Y;
            var front = _rect.AbsolutePosition.Z;
            var back = _rect.AbsolutePosition.Z + AbsoluteScale.Z - 1;

            // Check if the point is inside the prism
            return point.X > left && point.X < right && point.Y > bottom && point.Y < top && point.Z > back && point.Z < front;
        }

        public override Vector3 CalculateCollisionOutputVelocity(Collision collision, Vector3 velocity, float bounciness)
        {
            throw new NotImplementedException();
        }
    }
}
