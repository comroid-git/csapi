using System.Numerics;
using comroid.gamelib.Capability;
using SFML.Graphics;

namespace comroid.gamelib;

public interface ICancellable
{
    bool Cancel();
}

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

public interface IUpdatable
{
    bool EarlyUpdate();
    bool Update();
    bool LateUpdate();
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

public interface ICamera : ITransform
{
}

public interface IGameComponent : ITransform, ILoadable, IEnableable, IUpdatable, IDisposable, IDrawable, ISet<IGameComponent>
{
    string Name { get; set; }
    IGameComponent? Parent { get; }
    GameBase Game { get; }
    ITransform Transform { get; }
    
    R? Add<R>(string? name = null) where R : IGameComponent;
    R? FindComponent<R>();
    IEnumerable<R> FindComponents<R>();
    IEnumerable<IGameComponent> AllComponents();
    bool Destroy();
}

public interface IGameObject : IGameComponent
{
}

public interface IGameObjectComponent : IGameComponent
{
    IGameObject GameObject { get; }
}

public interface IRenderObject : IGameObjectComponent
{
}

public interface ICollider : IGameObjectComponent
{
    bool Inverse { get; set; }
    
    bool CollidesWith2D(ICollider other, out Vector2? point);
    IEnumerable<Vector2> GetBoundary2D();

    bool IsPointInside(Vector2 p);
    bool IsPointInside(Vector3 p);
    Vector3 CalculateCollisionOutputDirection(Collision collision, Vector3 velocity, float bounciness);
}
