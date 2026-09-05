using UnityEngine;

namespace FirstLight
{
    /// <summary>
    /// Every sprite in the game is generated at runtime, so the project needs no
    /// imported art and no prefab wiring. All sprites are authored at 32 px per
    /// world unit, so a 32x32 sprite covers exactly one grid cell.
    /// </summary>
    public static class Art
    {
        public const int PPU = 32;

        static Sprite Make(Texture2D tex)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                 new Vector2(0.5f, 0.5f), PPU);
        }

        static Texture2D Blank(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            tex.SetPixels(px);
            return tex;
        }

        static Sprite square;
        /// <summary>Plain 1x1 cell fill. Used for floors, walls, beams and fog.</summary>
        public static Sprite Square()
        {
            if (square != null) return square;
            var tex = Blank(PPU, PPU);
            for (int y = 0; y < PPU; y++)
                for (int x = 0; x < PPU; x++)
                    tex.SetPixel(x, y, Color.white);
            return square = Make(tex);
        }

        static Sprite tile;
        /// <summary>Cell fill with a 1 px inset border, so floors read as a grid.</summary>
        public static Sprite Tile()
        {
            if (tile != null) return tile;
            var tex = Blank(PPU, PPU);
            for (int y = 0; y < PPU; y++)
                for (int x = 0; x < PPU; x++)
                {
                    bool edge = x == 0 || y == 0 || x == PPU - 1 || y == PPU - 1;
                    tex.SetPixel(x, y, edge ? new Color(1, 1, 1, 0.35f) : Color.white);
                }
            return tile = Make(tex);
        }

        static Sprite circle;
        public static Sprite Circle()
        {
            if (circle != null) return circle;
            return circle = Make(Disc(PPU, PPU * 0.5f - 1f, 0f));
        }

        static Texture2D Disc(int size, float radius, float inner)
        {
            var tex = Blank(size, size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = Mathf.Clamp01(radius - d);
                    if (inner > 0f) a = Mathf.Min(a, Mathf.Clamp01(d - inner));
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            return tex;
        }

        static Sprite ring;
        public static Sprite Ring()
        {
            if (ring != null) return ring;
            return ring = Make(Disc(PPU, PPU * 0.5f - 1f, PPU * 0.5f - 5f));
        }

        static Sprite glow;
        /// <summary>Soft radial falloff, used for the beam bloom and the player's aura.</summary>
        public static Sprite Glow()
        {
            if (glow != null) return glow;
            const int size = 64;
            var tex = Blank(size, size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1, 1, 1, a * a * a));
                }
            return glow = Make(tex);
        }

        static Sprite diamond;
        public static Sprite Diamond()
        {
            if (diamond != null) return diamond;
            var tex = Blank(PPU, PPU);
            float c = (PPU - 1) * 0.5f;
            for (int y = 0; y < PPU; y++)
                for (int x = 0; x < PPU; x++)
                {
                    float d = Mathf.Abs(x - c) + Mathf.Abs(y - c);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(c - 1f - d)));
                }
            return diamond = Make(tex);
        }

        static Sprite mirrorPlate;
        /// <summary>
        /// A mirror bar: bright reflective face on the +y side, dull backing on -y.
        /// Rotating the object points that bright face along the mirror's normal.
        /// </summary>
        public static Sprite MirrorPlate()
        {
            if (mirrorPlate != null) return mirrorPlate;
            const int w = 40, h = 12;
            var tex = Blank(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    // taper the ends so the plate reads as a blade, not a brick
                    float taper = Mathf.Clamp01(Mathf.Min(x, w - 1 - x) / 3f);
                    float a = taper;
                    Color col = y >= h / 2 ? Color.white : new Color(0.42f, 0.45f, 0.55f, 1f);
                    if (y == h / 2 || y == h / 2 - 1) col = Color.white;
                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, a));
                }
            return mirrorPlate = Make(tex);
        }

        static Sprite star;
        /// <summary>The heavenly entity: a four-pointed star with a soft core.</summary>
        public static Sprite Star()
        {
            if (star != null) return star;
            const int size = 40;
            var tex = Blank(size, size);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - c) / c, dy = Mathf.Abs(y - c) / c;
                    // four-pointed star: sharp along the axes, pinched on the diagonals
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float pinch = 1f - 0.85f * Mathf.Min(dx, dy) / Mathf.Max(d, 0.001f);
                    float a = Mathf.Clamp01((pinch - d) * 3f);
                    float core = Mathf.Clamp01(1f - d * 3.4f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(a + core)));
                }
            return star = Make(tex);
        }

        static Sprite lamp;
        /// <summary>A lantern: solid core with four spokes, so it reads as casting four ways.</summary>
        public static Sprite Lamp()
        {
            if (lamp != null) return lamp;
            var tex = Blank(PPU, PPU);
            float c = (PPU - 1) * 0.5f;
            for (int y = 0; y < PPU; y++)
                for (int x = 0; x < PPU; x++)
                {
                    float dx = x - c, dy = y - c;
                    float core = Mathf.Clamp01(PPU * 0.22f - Mathf.Sqrt(dx * dx + dy * dy));
                    // spokes along the axes
                    float spokeH = Mathf.Clamp01(2f - Mathf.Abs(dy)) * Mathf.Clamp01(c - 1f - Mathf.Abs(dx));
                    float spokeV = Mathf.Clamp01(2f - Mathf.Abs(dx)) * Mathf.Clamp01(c - 1f - Mathf.Abs(dy));
                    float a = Mathf.Clamp01(Mathf.Max(core, Mathf.Max(spokeH, spokeV) * 0.85f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            return lamp = Make(tex);
        }

        static Sprite petals;
        /// <summary>A four-petal rosette for the nightbloom - it opens in the dark.</summary>
        public static Sprite Petals()
        {
            if (petals != null) return petals;
            var tex = Blank(PPU, PPU);
            float c = (PPU - 1) * 0.5f;
            float off = PPU * 0.20f, r = PPU * 0.24f;
            var centres = new[]
            {
                new Vector2(c, c + off), new Vector2(c, c - off),
                new Vector2(c + off, c), new Vector2(c - off, c),
            };
            for (int y = 0; y < PPU; y++)
                for (int x = 0; x < PPU; x++)
                {
                    float a = 0f;
                    foreach (var p in centres)
                    {
                        float d = Mathf.Sqrt((x - p.x) * (x - p.x) + (y - p.y) * (y - p.y));
                        a = Mathf.Max(a, Mathf.Clamp01(r - d));
                    }
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            return petals = Make(tex);
        }

        static Sprite lens;
        /// <summary>A lens for the focus: two arcs meeting at a point, drawn upright.</summary>
        public static Sprite Lens()
        {
            if (lens != null) return lens;
            var tex = Blank(PPU, PPU);
            float c = (PPU - 1) * 0.5f, off = PPU * 0.34f, r = PPU * 0.52f;
            for (int y = 0; y < PPU; y++)
                for (int x = 0; x < PPU; x++)
                {
                    float dl = Mathf.Sqrt((x - (c - off)) * (x - (c - off)) + (y - c) * (y - c));
                    float dr = Mathf.Sqrt((x - (c + off)) * (x - (c + off)) + (y - c) * (y - c));
                    // the vesica where the two discs overlap
                    float a = Mathf.Min(Mathf.Clamp01(r - dl), Mathf.Clamp01(r - dr));
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            return lens = Make(tex);
        }

        static Sprite arrow;
        /// <summary>Triangle, used for the fixed beam source.</summary>
        public static Sprite Arrow()
        {
            if (arrow != null) return arrow;
            var tex = Blank(PPU, PPU);
            for (int y = 0; y < PPU; y++)
                for (int x = 0; x < PPU; x++)
                {
                    float t = x / (float)(PPU - 1);          // 0 at tail, 1 at tip
                    float half = (1f - t) * (PPU * 0.5f - 1f);
                    float d = Mathf.Abs(y - (PPU - 1) * 0.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(half - d)));
                }
            return arrow = Make(tex);
        }
    }
}
