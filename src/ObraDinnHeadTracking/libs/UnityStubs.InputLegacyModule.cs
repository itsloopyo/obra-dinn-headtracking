// Unity stub for CI builds — InputLegacyModule slice (also compiled into UnityEngine.InputModule.dll for OBRA csproj compat)
namespace UnityEngine {
    public static class Input {
        public static bool GetKeyDown(KeyCode key) => false;
        public static bool GetKeyUp(KeyCode key) => false;
        public static bool GetKey(KeyCode key) => false;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
        public static float GetAxis(string axisName) => 0;
        public static float GetAxisRaw(string axisName) => 0;
        public static Vector3 mousePosition { get; }
    }
    public enum KeyCode {
        None = 0, Backspace = 8, Tab = 9, Clear = 12, Return = 13, Pause = 19, Escape = 27, Space = 32,
        Alpha0 = 48, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        A = 97, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Delete = 127, Keypad0 = 256, Keypad1, Keypad2, Keypad3, Keypad4, Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
        F1 = 282, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15,
        UpArrow = 273, DownArrow = 274, RightArrow = 275, LeftArrow = 276,
        Insert = 277, Home = 278, End = 279, PageUp = 280, PageDown = 281,
        RightShift = 303, LeftShift = 304, RightControl = 305, LeftControl = 306,
        RightAlt = 307, LeftAlt = 308, Mouse0 = 323, Mouse1, Mouse2
    }
}
