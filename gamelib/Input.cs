using System.Collections.Concurrent;
using System.Numerics;
using SFML.Graphics;
using SFML.Window;

namespace comroid.gamelib;

public static class Input
{
    public static readonly ConcurrentDictionary<Keyboard.Key, byte> Key = new();
    public static readonly ConcurrentDictionary<Mouse.Button, byte> MouseButton = new();
    public static Vector2 MousePosition { get; private set; }
    //public static float MouseWheelDelta { get; private set; }

    public static void CreateHandlers(RenderWindow win)
    {
        win.MouseMoved += (_, e) => MousePosition = new Vector2(e.X, e.Y);
    }
}