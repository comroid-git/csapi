using System.Numerics;

namespace comroid.gamelib;

public struct Singularity : ITransform
{
    public static readonly Vector3 DefaultPosition = Vector3.Zero;
    public static readonly Vector3 DefaultScale = Vector3.One;
    public static readonly Quaternion DefaultRotation = Quaternion.Identity;
    public static Singularity Default() => new()
        { Position = DefaultPosition, Scale = DefaultScale, Rotation = DefaultRotation };
    
    public Vector3 Position { get; set; }
    public Vector3 Scale { get; set; }
    public Quaternion Rotation { get; set; }
}
