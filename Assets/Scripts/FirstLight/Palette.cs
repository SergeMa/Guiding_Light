using UnityEngine;

namespace FirstLight
{
    /// <summary>
    /// Two-tone language: everything unlit sits near black, everything the beam
    /// touches goes warm and bright. Red is reserved for enemy eyes and nothing else.
    /// </summary>
    public static class Palette
    {
        public static readonly Color Background = new(0.023f, 0.027f, 0.055f);

        public static readonly Color FloorDark = new(0.055f, 0.062f, 0.105f);
        public static readonly Color FloorLit = new(0.62f, 0.47f, 0.24f);

        public static readonly Color WallDark = new(0.088f, 0.098f, 0.155f);
        public static readonly Color WallLit = new(0.80f, 0.65f, 0.38f);

        public static readonly Color VoidDeep = new(0.008f, 0.010f, 0.024f);

        public static readonly Color MirrorDark = new(0.20f, 0.24f, 0.34f);
        public static readonly Color MirrorLit = new(0.96f, 0.93f, 0.82f);

        public static readonly Color PrismDark = new(0.24f, 0.30f, 0.42f);
        public static readonly Color PrismLit = new(0.72f, 0.90f, 1.00f);

        public static readonly Color DeviceDark = new(0.16f, 0.18f, 0.26f);
        public static readonly Color DeviceLit = new(1.00f, 0.82f, 0.42f);

        // A lantern is the warmest thing in the game when it burns, and dead metal when it does not.
        public static readonly Color LanternDark = new(0.20f, 0.19f, 0.22f);
        public static readonly Color LanternLit = new(1.00f, 0.90f, 0.62f);

        // The nightbloom works the other way round, so it gets the one cool,
        // moonlit note in a warm palette - light closes it.
        public static readonly Color ShadeDark = new(0.14f, 0.18f, 0.30f);
        public static readonly Color ShadeOpen = new(0.45f, 0.66f, 0.98f);

        public static readonly Color BeamCore = new(1.00f, 0.97f, 0.86f);
        public static readonly Color BeamGlow = new(1.00f, 0.76f, 0.32f, 0.40f);

        public static readonly Color Player = new(0.85f, 0.93f, 1.00f);
        public static readonly Color PlayerGlow = new(0.55f, 0.75f, 1.00f, 0.34f);

        // A gloom is a shade lighter than a cloud, so the two darknesses read apart.
        public static readonly Color GloomFog = new(0.020f, 0.021f, 0.040f);

        // A lens is cool and inert until two beams cross on it.
        public static readonly Color FocusDark = new(0.20f, 0.26f, 0.36f);
        public static readonly Color FocusLit = new(1.00f, 0.94f, 0.72f);

        public static readonly Color Fog = new(0.004f, 0.004f, 0.010f);
        public static readonly Color Eye = new(1.00f, 0.16f, 0.16f);
        public static readonly Color WardShell = new(0.85f, 0.22f, 0.28f);

        public static readonly Color ExitDark = new(0.16f, 0.22f, 0.30f);
        public static readonly Color ExitLit = new(1.00f, 0.98f, 0.90f);
    }
}
