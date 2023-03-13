using System.Numerics;

namespace comroid.gamelib;

public partial class Circle
{
    private class Collider : GameObjectComponent, ICollider
    {
        private readonly Circle _circle;

        public Collider(IGameObject gameObject, Circle circle) : base(gameObject, circle)
        {
            _circle = circle;
        }

        public bool IsPointInside(Vector2 p) => Vector2.Distance(AbsolutePosition.To2(), p) < _circle.Radius;
        public bool IsPointInside(Vector3 p) => Vector3.Distance(AbsolutePosition, p) < _circle.Radius;
    }
}

public partial class Rect
{
    public class Collider : GameObjectComponent, ICollider
    {
        private readonly Rect _rect;

        public Collider(IGameObject gameObject, Rect rect) : base(gameObject, rect)
        {
            _rect = rect;
        }

        // this method brought to you by ChatGPT
        public bool IsPointInside(Vector2 point)
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
        public bool IsPointInside(Vector3 point)
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
