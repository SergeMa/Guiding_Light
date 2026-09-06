using System.IO;
using FirstLight;
using UnityEditor;
using UnityEngine;

namespace FirstLight.EditorTools
{
    /// <summary>
    /// Draws the game's icon from the same palette the game itself is drawn with, so
    /// the icon cannot drift away from how the game looks.
    ///
    /// The picture is the game in one frame: a beam comes in from the edge, turns off
    /// a mirror, and the floor exists only where the light falls on it.
    ///
    /// Tools > First Light > Generate icon, or in batch mode:
    ///   -executeMethod FirstLight.EditorTools.IconGenerator.Generate
    /// writing to $FL_ICON_DIR, defaulting to Assets/Icon.
    /// </summary>
    public static class IconGenerator
    {
        const int Master = 1024;                       // drawn once, sampled down
        static readonly int[] Sizes = { 1024, 512, 256, 128, 64, 32 };

        // Layout in canvas fractions, y upwards. The beam arrives along the lower
        // third, turns at the mirror and leaves through the top.
        const float BeamY = 0.34f;
        const float TurnX = 0.60f;
        static readonly Vector2 Star = new(0.255f, 0.72f);

        [MenuItem("Tools/First Light/Generate icon")]
        public static void Generate()
        {
            string dir = System.Environment.GetEnvironmentVariable("FL_ICON_DIR");
            if (string.IsNullOrEmpty(dir)) dir = Path.Combine(Application.dataPath, "Icon");
            Directory.CreateDirectory(dir);

            var master = Draw(Master);
            foreach (int size in Sizes)
            {
                var tex = size == Master ? master : Downsample(master, size);
                File.WriteAllBytes(Path.Combine(dir, $"icon_{size}.png"), tex.EncodeToPNG());
                if (tex != master) Object.DestroyImmediate(tex);
            }
            Object.DestroyImmediate(master);

            Debug.Log($"First Light: icons written to {dir}");
            AssetDatabase.Refresh();
        }

        static Texture2D Draw(int n)
        {
            var px = new Color[n * n];

            Vector2 beamIn = new(-0.02f, BeamY), corner = new(TurnX, BeamY);
            Vector2 beamOut = new(TurnX, 1.02f);

            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    // canvas fractions, y upwards - SetPixels fills bottom row first,
                    // so row 0 already is the bottom and must not be flipped
                    var p = new Vector2((x + 0.5f) / n, (y + 0.5f) / n);

                    float toBeam = Mathf.Min(DistToSegment(p, beamIn, corner),
                                             DistToSegment(p, corner, beamOut));
                    float toStar = Vector2.Distance(p, Star);

                    // how much of the world is revealed here: warm from the beam,
                    // a small cool pool around the entity
                    float warm = Mathf.Clamp01(1f - toBeam / 0.30f);
                    warm *= warm;
                    float cool = Mathf.Clamp01(1f - toStar / 0.17f);
                    cool *= cool;

                    var c = Palette.Background;

                    // floor tiles, visible only where the light reaches them
                    float reveal = Mathf.Clamp01(warm * 1.15f + cool * 0.75f);
                    if (reveal > 0.004f)
                    {
                        var tile = Color.Lerp(Palette.FloorDark, Palette.FloorLit, warm);
                        tile = Color.Lerp(tile, Palette.PlayerGlow, cool * 0.55f);
                        c = Color.Lerp(c, tile, reveal);

                        // the grid the game is played on
                        const float cell = 1f / 7f;
                        float gx = Mathf.Abs(p.x / cell - Mathf.Round(p.x / cell));
                        float gy = Mathf.Abs(p.y / cell - Mathf.Round(p.y / cell));
                        float line = Mathf.Clamp01(1f - Mathf.Min(gx, gy) / 0.055f);
                        c = Color.Lerp(c, Palette.Background, line * 0.55f * reveal);
                    }

                    // the beam: a wide warm bloom under a hard bright core
                    c += (Color)(Vector4)Palette.BeamGlow *
                         (Mathf.Clamp01(1f - toBeam / 0.115f) * 0.62f);
                    c = Color.Lerp(c, Palette.BeamCore,
                                   Mathf.Clamp01((0.026f - toBeam) / 0.008f));

                    // the mirror: dull backing under a bright reflective face, the way
                    // the game draws it, so it reads as a plate and not a stray highlight
                    // east in, north out: the plate lies "/" and its bright face points
                    // north-west, bisecting the two rays - the same rule the game uses
                    float along = Vector2.Dot(p - corner, new Vector2(0.7071f, 0.7071f));
                    float across = Vector2.Dot(p - corner, new Vector2(-0.7071f, 0.7071f));
                    if (Mathf.Abs(along) < 0.125f)
                    {
                        float taper = Mathf.Clamp01((0.125f - Mathf.Abs(along)) / 0.030f);
                        if (across > -0.030f && across < 0.004f)
                            c = Color.Lerp(c, Palette.MirrorDark, taper);
                        if (across >= 0.004f && across < 0.026f)
                            c = Color.Lerp(c, Palette.MirrorLit, taper);
                    }

                    // the entity: a four-pointed star
                    var d = p - Star;
                    float r = d.magnitude;
                    if (r < 0.10f)
                    {
                        float pinch = 1f - 0.86f * Mathf.Min(Mathf.Abs(d.x), Mathf.Abs(d.y)) /
                                      Mathf.Max(r, 1e-4f);
                        float star = Mathf.Clamp01((pinch * 0.085f - r) / 0.012f);
                        float core = Mathf.Clamp01((0.016f - r) / 0.010f);
                        c = Color.Lerp(c, Palette.Player, Mathf.Clamp01(star + core));
                    }

                    c.a = 1f;
                    px[y * n + x] = c;
                }

            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>Box filter down from the master, so small sizes stay clean.</summary>
        static Texture2D Downsample(Texture2D src, int size)
        {
            int step = src.width / size;
            var from = src.GetPixels();
            var to = new Color[size * size];

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var sum = Color.clear;
                    for (int sy = 0; sy < step; sy++)
                        for (int sx = 0; sx < step; sx++)
                            sum += from[(y * step + sy) * src.width + x * step + sx];
                    to[y * size + x] = sum / (step * step);
                }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(to);
            tex.Apply();
            return tex;
        }
    }
}
