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

public interface IGameComponent : ITransform, ILoadable, IEnableable, ITickable, IDisposable, ISet<IGameComponent>
{
    ITransform Transform { get; }
}

public interface IGameObject : IGameComponent, IDrawable
{
}

public interface IRenderObject : IGameComponent, IDrawable
{
    IGameObject GameObject { get; }
}
