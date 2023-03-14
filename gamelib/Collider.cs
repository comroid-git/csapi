using System.Numerics;
using comroid.gamelib.Capability;

namespace comroid.gamelib;

public abstract class ColliderBase : GameObjectComponent, ICollider
{
    protected ColliderBase(IGameObject gameObject, ITransform transform = null!) : base(gameObject, transform)
    {
    }

    public bool CollidesWith2D(ICollider other, out Vector2? point)
    {
        point = other.GetBoundary2D().Where(IsPointInside)
            .Cast<Vector2?>()
            .FirstOrDefault(defaultValue: null);
        return point != null;
    }

    public abstract IEnumerable<Vector2> GetBoundary2D();
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

        public override IEnumerable<Vector2> GetBoundary2D()
        {
            const int segments = 12;
            for (var i = 1; i < segments; i++)
                yield return AbsolutePosition.To2() +
                             new Vector2(MathF.Sin(2 * MathF.PI * i / segments),
                                 MathF.Cos(2 * MathF.PI * i / segments)) * _circle.Radius;
            yield return AbsolutePosition.To2();
        }

        public override bool IsPointInside(Vector2 p) => Vector2.Distance(AbsolutePosition.To2(), p) < _circle.Radius;
        public override bool IsPointInside(Vector3 p) => Vector3.Distance(AbsolutePosition, p) < _circle.Radius;
        public override Vector3 CalculateCollisionOutputDirection(Collision collision, Vector3 velocity, float bounciness)
        {
            var velocityMagnitude = velocity.Length();
            if (velocityMagnitude == 0)
                return velocity;

            var me = collision.Sender.AbsolutePosition;
            var other = collision.CollidedWith.AbsolutePosition;
            var at = collision.CollisionPosition;
            var rel = other - me;
        
            // this part brought to you by ChatGPT
// Get the cross product of A and B
            Vector3 r = Vector3.Cross(me, at);

// Get the magnitude of r
            float rMagnitude = r.Length();

// Get the angle between A and B
            float theta = MathF.Acos(Vector3.Dot(Vector3.Normalize(velocity), Vector3.Normalize(rel)));

// Calculate the quaternion
            Quaternion rotation = new Quaternion(
                MathF.Cos(theta),
                MathF.Sin(theta) * (r.X / rMagnitude),
                MathF.Sin(theta) * (r.Y / rMagnitude),
                MathF.Sin(theta) * (r.Z / rMagnitude)
            );
            return Vector3.Transform(velocity, rotation) * bounciness;
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
        
        public override IEnumerable<Vector2> GetBoundary2D()
        {
            var left = _rect.Delegate.Position.X;
            var right = _rect.Delegate.Position.X + _rect.Size.X;
            var top = _rect.Delegate.Position.Y;
            var bottom = _rect.Delegate.Position.Y + _rect.Size.Y;
            foreach (var h in new[]{left,right})
            foreach (var v in new[]{top, bottom})
                yield return new Vector2(h, v);
            yield return AbsolutePosition.To2();
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
            return velocity;
        }
    }
}
