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
    private static object Lock = new();
    private static GameBase game = null!;
    private static RenderWindow window = null!;
    private static readonly ConcurrentDictionary<Keyboard.Key, KeyState> Key = new();
    internal static readonly ConcurrentDictionary<Mouse.Button, KeyState> MouseButton = new();
    public static Vector2 MousePosition { get; private set; }
    public static Vector2 MouseDelta { get; private set; }
    public static float MouseDeltaTime { get; private set; }
    private static float MouseSetTime { get; set; }

    private static Vector2 MousePositionOffset => (window.GetView().Size / 2).To2() + game.Camera.Position.To2();
    //public static float MouseWheelDelta { get; private set; }

    public static KeyState GetKey(Keyboard.Key key) => Key.GetOrAdd(key, k => new KeyState(k.ToString(), false, false));
    public static KeyState GetKey(Mouse.Button btn) => MouseButton.GetOrAdd(btn, b => new KeyState(b.ToString(), false, false));

    public static void Initialize(GameBase game, RenderWindow win)
    {
        void SetMousePos(float x, float y)
        {
            lock (Lock)
            {
                MouseDelta = MousePosition - (MousePosition = new Vector2(x, y) - MousePositionOffset);
                MouseDeltaTime = (MouseSetTime - DebugUtil.UnixTime()) / 1000;
                MouseSetTime = DebugUtil.UnixTime();
                // todo: fixme using multithreading
            }
        }

        Input.game = game;
        window = win;
        win.MouseMoved += (_, e) => SetMousePos(e.X, e.Y);
        win.MouseButtonPressed += (_, e) =>
        {
            SetMousePos(e.X, e.Y);
            if (!MouseButton.ContainsKey(e.Button))
                MouseButton[e.Button] = new KeyState(e.Button.ToString(), false, false);
            MouseButton[e.Button].Press();
        };
        win.MouseButtonReleased += (_, e) =>
        {
            SetMousePos(e.X, e.Y);
            if (!MouseButton.ContainsKey(e.Button))
                MouseButton[e.Button] = new KeyState(e.Button.ToString(), false, false);
            MouseButton[e.Button].Release();
        };
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
    }

    internal static void LateUpdate()
    {
        foreach (var state in Key.Values.Concat(MouseButton.Values)) 
            state.Push();
        lock (Lock)
        {
        }
    }
}