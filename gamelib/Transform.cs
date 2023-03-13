using System.Numerics;

namespace comroid.gamelib;

public class Singularity : ITransform
{
    public static readonly Vector3 DefaultPosition = Vector3.Zero;
    public static readonly Vector3 DefaultScale = Vector3.One;
    public static readonly Quaternion DefaultRotation = Quaternion.Identity;
    public static Singularity Default() => new();

    public Vector3 Position { get; set; } = DefaultPosition;
    public Vector3 Scale { get; set; } = DefaultScale;
    public Quaternion Rotation { get; set; } = DefaultRotation;
}
