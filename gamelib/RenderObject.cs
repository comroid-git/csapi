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
        Delegate.Position = GetDelegateTransformData_Position();
        Delegate.Scale = GetDelegateTransformData_Scale();
        Delegate.Rotation = GetDelegateTransformData_Rotation();
    }
    
    protected virtual Vector2f GetDelegateTransformData_Position() => (AbsolutePosition - AbsoluteScale / 2).To2f(); 
    protected virtual Vector2f GetDelegateTransformData_Scale() => AbsoluteScale.To2f(); 
    protected virtual float GetDelegateTransformData_Rotation() => AbsoluteRotation.Euler().Z; 

    public override void Draw(RenderWindow win)
    {
        UpdateDelegateTransformData();
        ApplyExtraData?.Invoke(Delegate);
        win.Draw(Delegate);
        base.Draw(win);
    }
}

public class ShapeBase<TDelegate> : RenderObjectBase<TDelegate> where TDelegate : Shape
{
    public Color Color
    {
        get => Delegate.FillColor;
        set => Delegate.FillColor = value;
    }

    public ShapeBase(TDelegate @delegate, IGameObject gameObject, ITransform transform = null!)
        : base(@delegate, gameObject, transform)
    {
    }
}

public partial class Circle : ShapeBase<CircleShape>
{
    public float Radius
    {
        get => Delegate.Radius;
        set => Delegate.Radius = value;
    }
    
    public Circle(IGameObject gameObject, ITransform transform = null!)
        : base(new CircleShape(), gameObject, transform)
    {
        //Add(new Collider(GameObject, this));
    }

    protected override Vector2f GetDelegateTransformData_Position() => (AbsolutePosition - Vector3.One * Radius).To2f();
}

public partial class Rect : ShapeBase<RectangleShape>
{
    public Vector2f Size
    {
        get => Delegate.Size;
        set => Delegate.Size = value;
    }

    public Rect(IGameObject gameObject, ITransform transform = null!)
        : base(new RectangleShape(new Vector2f(1,1)), gameObject, transform)
    {
        //Add(new Collider(GameObject, this));
    }

    protected override Vector2f GetDelegateTransformData_Position() => AbsolutePosition.To2f() - Size / 2;
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
    public Color FontColor
    {
        get => Delegate.FillColor;
        set => Delegate.FillColor = value;
    }

    public Text(IGameObject gameObject, ITransform transform = null!)
        : base(new SFML.Graphics.Text(string.Empty, FiraCode, 20), gameObject, transform)
    {
    }
}

public class Sprite : RenderObjectBase<SFML.Graphics.Sprite>
{
    public Texture Texture
    {
        get => Delegate.Texture;
        set => Delegate.Texture = value;
    }
    
    public Sprite(IGameObject gameObject, ITransform transform = null!)
        : base(new SFML.Graphics.Sprite(), gameObject, transform)
    {
    }
}
