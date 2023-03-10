using System.Numerics;
using SFML.Graphics;

namespace comroid.gamelib;

public abstract class RenderObjectBase<TDelegate> : GameComponent, IRenderObject where TDelegate : Transformable, Drawable
{
    public readonly TDelegate Delegate;

    public IGameObject GameObject { get; }
    
    public event Action<TDelegate>? ApplyExtraData;

    protected RenderObjectBase(TDelegate @delegate, IGameObject gameObject, ITransform transform = null!)
        : base(transform ?? gameObject)
    {
        this.GameObject = gameObject;
        this.Delegate = @delegate;
    }

    protected virtual void UpdateDelegateTransformData()
    {
        Delegate.Position = AbsolutePosition.To2f();
        Delegate.Scale = AbsoluteScale.To2f();
        Delegate.Rotation = AbsoluteRotation.EulerAngles().Z;
    }

    public void Draw(RenderWindow win)
    {
        UpdateDelegateTransformData();
        ApplyExtraData?.Invoke(Delegate);
        win.Draw(Delegate);
    }
}

public class Circle : RenderObjectBase<CircleShape>
{
    public Circle(IGameObject gameObject, ITransform transform = null!)
        : base(new CircleShape(), gameObject, transform)
    {
    }
}

public class Rect : RenderObjectBase<RectangleShape>
{
    public Rect(IGameObject gameObject, ITransform transform = null!)
        : base(new RectangleShape(), gameObject, transform)
    {
    }
}

public class Text : RenderObjectBase<SFML.Graphics.Text>
{
    public string Value
    {
        get => Delegate.DisplayedString;
        set => Delegate.DisplayedString = value;
    }
    
    public Text(IGameObject gameObject, ITransform transform = null!)
        : base(new SFML.Graphics.Text(), gameObject, transform)
    {
    }
}
