using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight
{
    public enum TouchControlsMode
    {
        /// <summary>On phones and tablets only. Desktop behaves exactly as before.</summary>
        Auto,
        Always,
        Never,
    }

    /// <summary>
    /// Screen-space pad for phones and tablets: a direction cross on the left, two
    /// mirror-turn buttons on the right, and restart in the corner.
    ///
    /// Hit-testing is done against the Input System's touches directly rather than
    /// through IMGUI buttons. That keeps it independent of whether IMGUI receives
    /// pointer events at all, lets a direction repeat while held the way a key does,
    /// and allows one thumb to hold a direction while the other turns a mirror.
    ///
    /// Layout and hit-testing are pure functions of the screen size, so they can be
    /// checked without a device attached.
    /// </summary>
    public class TouchControls
    {
        public enum Button { Up, Down, Left, Right, TurnCcw, TurnCw, Restart }

        public static readonly Button[] All =
        {
            Button.Up, Button.Down, Button.Left, Button.Right,
            Button.TurnCcw, Button.TurnCw, Button.Restart,
        };

        public struct State
        {
            public Vector2Int Move;                  // held, so movement repeats
            public bool TurnCcw, TurnCw, Restart;    // edge triggered, one per press
            public bool AnyTap;                      // anywhere, for the title and ending
        }

        public TouchControlsMode Mode = TouchControlsMode.Auto;

        readonly HashSet<Button> held = new();
        readonly HashSet<Button> before = new();
        readonly List<Vector2> pointers = new();
        bool hadPointers;

        public bool Active => Mode switch
        {
            TouchControlsMode.Always => true,
            TouchControlsMode.Never => false,
            _ => Application.platform == RuntimePlatform.Android ||
                 Application.platform == RuntimePlatform.IPhonePlayer,
        };

        public bool IsHeld(Button b) => held.Contains(b);

        // ------------------------------------------------------------------ layout

        /// <summary>Button size in pixels, from the screen's short side.</summary>
        public static float UnitFor(float w, float h) =>
            Mathf.Clamp(Mathf.Min(w, h) * 0.11f, 40f, 104f);

        /// <summary>
        /// Fraction of screen height the pad occupies along the bottom. The camera
        /// keeps the level clear of this band so nothing is played under a thumb.
        /// </summary>
        public float ReservedBottomFor(float w, float h)
        {
            if (!Active) return 0f;
            float u = UnitFor(w, h);
            return Mathf.Clamp(3.6f * u / h, 0f, 0.42f);
        }

        public float ReservedBottom => ReservedBottomFor(Screen.width, Screen.height);

        /// <summary>Button rectangle in GUI space (origin top-left, y downwards).</summary>
        public Rect RectFor(Button b, float w, float h)
        {
            float u = UnitFor(w, h), m = u * 0.30f;
            float cx = m + 1.5f * u;              // centre of the direction cross
            float cy = h - m - 1.5f * u;

            return b switch
            {
                Button.Up => new Rect(cx - u * 0.5f, cy - u * 1.5f, u, u),
                Button.Down => new Rect(cx - u * 0.5f, cy + u * 0.5f, u, u),
                Button.Left => new Rect(cx - u * 1.5f, cy - u * 0.5f, u, u),
                Button.Right => new Rect(cx + u * 0.5f, cy - u * 0.5f, u, u),
                Button.TurnCcw => new Rect(w - m - u * 1.15f, cy - u * 0.5f, u, u),
                Button.TurnCw => new Rect(w - m - u * 2.45f, cy - u * 0.5f, u, u),
                _ => new Rect(w - m - u * 0.85f, m, u * 0.85f, u * 0.85f),
            };
        }

        // ------------------------------------------------------------------- input

        public State Sample() => Evaluate(GatherPointers(), Screen.width, Screen.height);

        /// <summary>
        /// Turns a set of pressed pointer positions into a control state. Pure, so a
        /// test can drive it with coordinates instead of a finger.
        /// </summary>
        public State Evaluate(IReadOnlyList<Vector2> guiPoints, float w, float h)
        {
            // Inert unless it is actually on, so a stray pointer can never reach the
            // pad on desktop no matter who calls this.
            if (!Active)
            {
                held.Clear();
                before.Clear();
                hadPointers = false;
                return default;
            }

            before.Clear();
            before.UnionWith(held);
            held.Clear();

            for (int i = 0; i < guiPoints.Count; i++)
                foreach (var b in All)
                    if (RectFor(b, w, h).Contains(guiPoints[i]))
                        held.Add(b);

            var s = new State();
            if (held.Contains(Button.Up)) s.Move = Vector2Int.up;
            else if (held.Contains(Button.Down)) s.Move = Vector2Int.down;
            else if (held.Contains(Button.Left)) s.Move = Vector2Int.left;
            else if (held.Contains(Button.Right)) s.Move = Vector2Int.right;

            s.TurnCcw = held.Contains(Button.TurnCcw) && !before.Contains(Button.TurnCcw);
            s.TurnCw = held.Contains(Button.TurnCw) && !before.Contains(Button.TurnCw);
            s.Restart = held.Contains(Button.Restart) && !before.Contains(Button.Restart);

            s.AnyTap = guiPoints.Count > 0 && !hadPointers;
            hadPointers = guiPoints.Count > 0;
            return s;
        }

        List<Vector2> GatherPointers()
        {
            pointers.Clear();

            var screen = Touchscreen.current;
            if (screen != null)
            {
                var touches = screen.touches;
                for (int i = 0; i < touches.Count; i++)
                    if (touches[i].press.isPressed)
                        pointers.Add(ToGui(touches[i].position.ReadValue()));
            }

            // a mouse press stands in for a finger, so the pad can be tried out in
            // the editor with Mode set to Always
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
                pointers.Add(ToGui(mouse.position.ReadValue()));

            return pointers;
        }

        static Vector2 ToGui(Vector2 screenPoint) =>
            new(screenPoint.x, Screen.height - screenPoint.y);

        // ----------------------------------------------------------------- drawing

        public void Draw()
        {
            if (!Active) return;

            float w = Screen.width, h = Screen.height;
            float u = UnitFor(w, h);
            var prevColor = GUI.color;
            var prevMatrix = GUI.matrix;

            // a quiet ground under the pad so the level never reads as continuing into it
            GUI.color = new Color(Palette.Background.r, Palette.Background.g,
                                  Palette.Background.b, 0.82f);
            GUI.DrawTexture(new Rect(0, h * (1f - ReservedBottomFor(w, h)), w,
                                     h * ReservedBottomFor(w, h)), Art.Square().texture);

            foreach (var b in All)
            {
                var r = RectFor(b, w, h);
                bool down = held.Contains(b);

                GUI.color = down ? new Color(1f, 0.82f, 0.42f, 0.34f)
                                 : new Color(0.72f, 0.76f, 0.86f, 0.13f);
                GUI.DrawTexture(r, Art.Circle().texture);

                GUI.color = down ? Palette.BeamCore : new Color(0.80f, 0.84f, 0.92f, 0.72f);
                DrawGlyph(b, r, u);
            }

            GUI.matrix = prevMatrix;
            GUI.color = prevColor;
        }

        void DrawGlyph(Button b, Rect r, float u)
        {
            var centre = r.center;
            var glyph = new Rect(centre.x - u * 0.28f, centre.y - u * 0.28f, u * 0.56f, u * 0.56f);

            switch (b)
            {
                case Button.Up:
                case Button.Down:
                case Button.Left:
                case Button.Right:
                {
                    // Art.Arrow points east; rotate it round the button centre
                    float angle = b switch
                    {
                        Button.Right => 0f,
                        Button.Down => 90f,
                        Button.Left => 180f,
                        _ => 270f,
                    };
                    var saved = GUI.matrix;
                    GUIUtility.RotateAroundPivot(angle, centre);
                    GUI.DrawTexture(glyph, Art.Arrow().texture);
                    GUI.matrix = saved;
                    break;
                }

                case Button.TurnCcw:
                case Button.TurnCw:
                {
                    // the two diagonals a mirror sits at, so the button reads as "turn it"
                    var saved = GUI.matrix;
                    GUIUtility.RotateAroundPivot(b == Button.TurnCcw ? -45f : 45f, centre);
                    GUI.DrawTexture(new Rect(centre.x - u * 0.34f, centre.y - u * 0.07f,
                                             u * 0.68f, u * 0.14f), Art.Square().texture);
                    GUI.matrix = saved;
                    break;
                }

                default:
                    GUI.DrawTexture(glyph, Art.Ring().texture);
                    break;
            }
        }
    }
}
