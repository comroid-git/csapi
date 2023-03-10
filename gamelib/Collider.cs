using System.Numerics;

namespace comroid.gamelib;

public abstract class ColliderBase : GameObjectComponent, ICollider
{
    protected ColliderBase(IGameObject gameObject, ITransform transform = null!) : base(gameObject, transform)
    {
    }

    public bool CollidesWith2D(ICollider other) => other.GetBoundary2D().Any(IsPointInside);

    public abstract IEnumerable<Vector2> GetBoundary2D();
    public abstract bool IsPointInside(Vector2 p);
    public abstract bool IsPointInside(Vector3 p);
}

public partial class Circle
{
    private class Collider : ColliderBase
    {
        private readonly Circle _circle;

        public Collider(IGameObject gameObject, Circle circle) : base(gameObject, circle)
        {
            _circle = circle;
        }

        public override IEnumerable<Vector2> GetBoundary2D()
        {
            for (var x = 0; x < 12; x++)
                yield return AbsolutePosition.To2() + new Vector2(MathF.Sin(x / 12), MathF.Cos(x / 12)) * _circle.Radius;
            yield return AbsolutePosition.To2();
        }

        public override bool IsPointInside(Vector2 p) => Vector2.Distance(AbsolutePosition.To2(), p) < _circle.Radius;
        public override bool IsPointInside(Vector3 p) => Vector3.Distance(AbsolutePosition, p) < _circle.Radius;
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
            throw new NotImplementedException();
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
    }
}
