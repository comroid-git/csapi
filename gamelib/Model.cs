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

[Flags]
public enum Channel : byte
{
    Default = default,
    
    // types
    Player = 0x10,
    Props = 0x20,
    Board = 0x40,
    Environment = 0x80,
    
    // attributes
    Hidden = 0x01,
    Res1 = 0x02,
    Res2 = 0x04,
    Res3 = 0x08,
}

public interface IGameComponent : ITransform, ILoadable, IEnableable, IUpdatable, IDisposable, IDrawable, ISet<IGameComponent>
{
    Channel Channel { get; set; }
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
    
    bool CollidesWith2D(ICollider other, out Vector2? point, out Vector2? commonPoint);
    IEnumerable<(Vector2 point, Vector2 inside)> GetBoundary2D();

    bool IsPointInside(Vector2 p);
    bool IsPointInside(Vector3 p);
    Vector3 CalculateCollisionOutputDirection(Collision collision, Vector3 velocity, float bounciness);
}
