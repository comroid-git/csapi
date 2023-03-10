using System.Numerics;
using SFML.Graphics;
using SFML.Window;

namespace comroid.gamelib;

public class WindowCamera : ICamera
{
    public WindowCamera(RenderWindow window)
    {
        Window = window;
    }

    public RenderWindow Window { get; }
    public Vector3 Position { get; set; }
    public Vector3 Scale
    {
        get => Window.GetView().Size.To3();
        set => throw new NotSupportedException();
    }

    public Quaternion Rotation
    {
        get => Quaternion.Identity;
        set => throw new NotSupportedException();
    }
}
