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
    public abstract Vector3 CalculateCollisionOutputDirection(Collision collision, Vector3 velocity, float bounciness);
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

        public override IEnumerable<(Vector2 point, Vector2 inside)> GetBoundary2D()
        {
            const int segments = 12;
            for (var i = 1; i < segments; i++)
            {
                var off = new Vector2f(MathF.Sin(2 * MathF.PI * i / segments),
                                       MathF.Cos(2 * MathF.PI * i / segments)) * _circle.Radius;
                yield return ((_circle.Delegate.Position + off).To2(), _circle.Delegate.Position.To2());
            }
        }

        public override bool IsPointInside(Vector2 p) => Vector2.Distance(AbsolutePosition.To2(), p) < _circle.Radius;
        public override bool IsPointInside(Vector3 p) => Vector3.Distance(AbsolutePosition, p) < _circle.Radius;
        public override Vector3 CalculateCollisionOutputDirection(Collision collision, Vector3 velocity, float bounciness)
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
            Vector3 r = Vector3.Normalize(Vector3.Cross(me, at));

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

        public override IEnumerable<(Vector2 point, Vector2 inside)> GetBoundary2D()
        {
            var inside = Inverse ? -1 : 1;
            var sizeX = _rect.Size.X;
            var sizeY = _rect.Size.Y;
            var left = _rect.Delegate.Position.X;
            var right = _rect.Delegate.Position.X + sizeX;
            var top = _rect.Delegate.Position.Y;
            var bottom = _rect.Delegate.Position.Y + sizeY;
            for (var h = left; h <= right; h += sizeX * (5 / sizeX))
            {
                yield return (new Vector2(h, top),new Vector2(h,top+inside));
                yield return (new Vector2(h, bottom),new Vector2(h,bottom-inside));
            }
            for (var v = top; v <= bottom; v += sizeY * (5 / sizeY))
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
            var left = _rect.Delegate.Position.X;
            var right = _rect.Delegate.Position.X + _rect.Size.X;
            var top = _rect.Delegate.Position.Y;
            var bottom = _rect.Delegate.Position.Y + _rect.Size.Y;

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
            var left = _rect.Delegate.Position.X;
            var right = _rect.Delegate.Position.X + _rect.Size.X;
            var top = _rect.Delegate.Position.Y;
            var bottom = _rect.Delegate.Position.Y + _rect.Size.Y;
            var front = AbsolutePosition.Z;
            var back = AbsolutePosition.Z + AbsoluteScale.Z;

            // Check if the point is inside the prism
            return point.X > left && point.X < right && point.Y > bottom && point.Y < top && point.Z > back && point.Z < front;
        }

        public override Vector3 CalculateCollisionOutputDirection(Collision collision, Vector3 velocity, float bounciness)
        {
            throw new NotImplementedException();
        }
    }
}
