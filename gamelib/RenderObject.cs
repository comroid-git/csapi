using System.Data;
using System.Numerics;
using SFML.Graphics;
using SFML.System;

namespace comroid.gamelib;

public abstract class RenderObjectBase<TDelegate> : GameObjectComponent, IRenderObject where TDelegate : Transformable, Drawable
{
    public readonly TDelegate Delegate;

    public event Action<TDelegate>? ApplyExtraData;

    protected RenderObjectBase(TDelegate @delegate, IGameObject gameObject, ITransform transform = null!)
        : base(gameObject, transform)
    {
        this.Delegate = @delegate;
    }

    protected virtual void UpdateDelegateTransformData()
    {
        UpdateDelegateTransformData_Position();
        UpdateDelegateTransformData_Scale();
        UpdateDelegateTransformData_Rotation();
    }
    
    protected virtual void UpdateDelegateTransformData_Position() => Delegate.Position = AbsolutePosition.To2f(); 
    protected virtual void UpdateDelegateTransformData_Scale() => Delegate.Scale = AbsoluteScale.To2f(); 
    protected virtual void UpdateDelegateTransformData_Rotation() => Delegate.Rotation = AbsoluteRotation.Euler().Z; 

    public override void Draw(RenderWindow win)
    {
        UpdateDelegateTransformData();
        ApplyExtraData?.Invoke(Delegate);
        win.Draw(Delegate);
        base.Draw(win);
    }
}

public class Circle : RenderObjectBase<CircleShape>
{
    public float Radius
    {
        get => Delegate.Radius;
        set => Delegate.Radius = value;
    }
    
    public Circle(IGameObject gameObject, ITransform transform = null!)
        : base(new CircleShape(), gameObject, transform)
    {
        Add(new Collider(GameObject, this));
    }
    
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

public class Rect : RenderObjectBase<RectangleShape>
{
    public Rect(IGameObject gameObject, ITransform transform = null!)
        : base(new RectangleShape(new Vector2f(1,1)), gameObject, transform)
    {
        Add(new Collider(GameObject, this));
    }

    protected override void UpdateDelegateTransformData_Scale()
    {
        Delegate.Size = AbsoluteScale.To2f();
    }

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
            // Calculate the left, right, top, and bottom edges of the rectangle
            var left = AbsolutePosition.X;
            var right = AbsolutePosition.X + AbsoluteScale.X;
            var top = AbsolutePosition.Y;
            var bottom = AbsolutePosition.Y + AbsoluteScale.Y;

            // Check if the point is inside the rectangle
            return point.X > left && point.X < right && point.Y > top && point.Y < bottom;
        }

        // this method brought to you by ChatGPT
        public bool IsPointInside(Vector3 point)
        {
            // Calculate the left, right, top, bottom, front, and back planes of the cuboid
            var left = AbsolutePosition.X;
            var right = AbsolutePosition.X + AbsoluteScale.X;
            var top = AbsolutePosition.Y;
            var bottom = AbsolutePosition.Y + AbsoluteScale.Y;
            var front = AbsolutePosition.Z;
            var back = AbsolutePosition.Z + AbsoluteScale.Z;

            // Check if the point is inside the cuboid
            return point.X > left && point.X < right && point.Y > top && point.Y < bottom && point.Z > back && point.Z < front;
        }
    }
}

public class Text : RenderObjectBase<SFML.Graphics.Text>
{
    public static Font Roboto = new("Assets/Roboto.ttf");
    public static Font FiraCode = new("Assets/FiraCode.ttf");
    
    public string Value
    {
        get => Delegate.DisplayedString;
        set => Delegate.DisplayedString = value;
    }
    public Font Font
    {
        get => Delegate.Font;
        set => Delegate.Font = value;
    }
    public uint FontSize
    {
        get => Delegate.CharacterSize;
        set => Delegate.CharacterSize = value;
    }

    public Text(IGameObject gameObject, ITransform transform = null!)
        : base(new SFML.Graphics.Text(string.Empty, FiraCode, 20), gameObject, transform)
    {
    }
}
