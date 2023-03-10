using System.Collections.Concurrent;
using System.Numerics;
using comroid.common;
using SFML.Graphics;
using SFML.Window;

namespace comroid.gamelib;

public sealed class KeyState
{
    private readonly string _name;

    internal KeyState(string name, bool pre, bool down)
    {
        _name = name;
        Pre = pre;
        Down = down;
    }

    public bool Pre { get; private set; }
    public bool Down { get; private set; }
    public bool Pressed => !Pre && Down;
    public bool Released => Pre && !Down;

    internal void Press() => Push(true);
    internal void Release() => Push(false);
    internal void Push(bool? state = null!) => (Pre, Down) = (Down, state ?? Down);

    public override string ToString()
    {
        return $"{_name} = {(Pressed ? "Rising" : Released ? "Falling" : Down ? "Pressed" : "Idle")}";
    }

    public static implicit operator bool(KeyState state) => state.Down;
}

public static class Input
{
    private static readonly ConcurrentDictionary<Keyboard.Key, KeyState> Key = new();
    private static readonly ConcurrentDictionary<Mouse.Button, KeyState> MouseButton = new();
    public static Vector2 MousePosition { get; private set; }
    //public static float MouseWheelDelta { get; private set; }

    public static KeyState GetKey(Keyboard.Key key) => Key.GetOrAdd(key, k => new KeyState(k.ToString(), false, false));
    public static KeyState GetKey(Mouse.Button btn) => MouseButton.GetOrAdd(btn, b => new KeyState(b.ToString(), false, false));

    public static void CreateHandlers(RenderWindow win)
    {
        win.MouseMoved += (_, e) => MousePosition = new Vector2(e.X, e.Y);
        win.KeyPressed += (_, e) =>
        {
            if (!Key.ContainsKey(e.Code))
                Key[e.Code] = new KeyState(e.Code.ToString(), false, false);
            Key[e.Code].Press();
        };
        win.KeyReleased += (_, e) =>
        {
            if (!Key.ContainsKey(e.Code))
                Key[e.Code] = new KeyState(e.Code.ToString(), false, false);
            Key[e.Code].Release();
        };
        win.MouseButtonPressed += (_, e) =>
        {
            if (!MouseButton.ContainsKey(e.Button))
                MouseButton[e.Button] = new KeyState(e.Button.ToString(), false, false);
            MouseButton[e.Button].Press();
        };
        win.MouseButtonReleased += (_, e) =>
        {
            if (!MouseButton.ContainsKey(e.Button))
                MouseButton[e.Button] = new KeyState(e.Button.ToString(), false, false);
            MouseButton[e.Button].Release();
        };
    }

    internal static void LateUpdate()
    {
        foreach (var state in Key.Values.Concat(MouseButton.Values)) 
            state.Push();
    }
}