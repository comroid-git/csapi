using System.Numerics;
using SFML.Graphics;

namespace comroid.gamelib;

public interface ILoadable
{
    bool Loaded { get; }

    bool Load();
    bool Unload();
}

public interface IEnableable
{
    bool Enabled { get; }

    bool Enable();
    bool Disable();
}

public interface ITickable
{
    bool Tick();
}

public interface IDrawable
{
    void Draw(RenderWindow win);
}

public interface ITransform
{
    Vector3 Position { get; set; }
    Vector3 Scale { get; set; }
    Quaternion Rotation { get; set; }

    Vector3 AbsolutePosition => Position;
    Vector3 AbsoluteScale => Scale;
    Quaternion AbsoluteRotation => Rotation;
}

public interface IGameComponent : ITransform, ILoadable, IEnableable, ITickable, IDisposable, IDrawable, ISet<IGameComponent>
{
    ITransform Transform { get; }

    R? FindChild<R>();
    IEnumerable<R> FindChildren<R>();
}

public interface IGameObject : IGameComponent
{
}

public interface IGameObjectComponent : IGameComponent
{
}

public interface IRenderObject : IGameObjectComponent
{
    IGameObject GameObject { get; }
}

public interface ICollider : IGameObjectComponent
{
    bool IsPointInside(Vector2 p);
    bool IsPointInside(Vector3 p);
}
