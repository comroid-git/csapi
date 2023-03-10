using System.Data;
using System.Numerics;
using SFML.Graphics;
using SFML.System;

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
    }
}

public class Rect : RenderObjectBase<RectangleShape>
{
    public Rect(IGameObject gameObject, ITransform transform = null!)
        : base(new RectangleShape(new Vector2f(1,1)), gameObject, transform)
    {
    }

    protected override void UpdateDelegateTransformData_Scale()
    {
        Delegate.Size = AbsoluteScale.To2f();
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
