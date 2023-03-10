using System.Numerics;

namespace comroid.gamelib;

public struct Singularity : ITransform
{
    public static Singularity Default() => new()
        { Position = Vector3.Zero, Scale = Vector3.One, Rotation = Quaternion.Identity };
    
    public Vector3 Position { get; set; }
    public Vector3 Scale { get; set; }
    public Quaternion Rotation { get; set; }
}
