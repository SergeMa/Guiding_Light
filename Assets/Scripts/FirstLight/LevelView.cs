using System.Collections.Generic;
using UnityEngine;

namespace FirstLight
{
    /// <summary>
    /// Builds a level's visuals from its ASCII map and keeps them in sync with the
    /// light simulation. Nothing here is authored in the editor - every renderer is
    /// created at runtime from <see cref="Art"/>.
    /// </summary>
    public class LevelView : MonoBehaviour
    {
        class Piece
        {
            public Vector2Int Cell;
            public SpriteRenderer Sr;
            public Color Dark, Lit;
            public char Kind;
            public int MirrorIndex = -1;
            public bool IgnoreCellLight;
            public SpriteRenderer Halo;
        }

        public LevelDef Level { get; private set; }
        public LightState State { get; private set; }
        public List<int> Orients { get; private set; }

        readonly List<Piece> pieces = new();
        readonly List<SpriteRenderer> beamPool = new();
        readonly List<SpriteRenderer> fogPieces = new();
        readonly Dictionary<int, Transform> eyeVisuals = new();
        readonly Dictionary<int, Vector2> eyeHome = new();
        readonly Dictionary<int, SpriteRenderer> wardShells = new();
        readonly Dictionary<Vector2Int, float> cellLight = new();

        Transform beamRoot, fogRoot;
        float ambient;
        float roamTimer;

        public static LevelView Create(LevelDef def, float ambient)
        {
            var go = new GameObject("Level: " + def.Name);
            var view = go.AddComponent<LevelView>();
            view.Build(def, ambient);
            return view;
        }

        void Build(LevelDef def, float ambientLight)
        {
            Level = def;
            ambient = ambientLight;
            Orients = new List<int>(def.InitialOrients);

            beamRoot = new GameObject("Beam").transform;
            beamRoot.SetParent(transform, false);
            fogRoot = new GameObject("Darkness").transform;
            fogRoot.SetParent(transform, false);

            for (int x = 0; x < def.Width; x++)
                for (int y = 0; y < def.Height; y++)
                    BuildCell(new Vector2Int(x, y));

            BuildFog();
            Recompute();
        }

        // --------------------------------------------------------------- building

        SpriteRenderer Spawn(string name, Sprite sprite, Vector2 pos, int order,
                             Vector2 scale, float angle = 0f, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            go.transform.rotation = Quaternion.Euler(0, 0, angle);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        void Add(Piece p) => pieces.Add(p);

        void BuildCell(Vector2Int c)
        {
            char ch = Level.At(c);
            Vector2 pos = c;

            if (ch == Tile.Void)
            {
                Spawn("void", Art.Square(), pos, Order.Floor, Vector2.one).color = Palette.VoidDeep;
                return;
            }

            if (ch == Tile.Wall)
            {
                Add(new Piece
                {
                    Cell = c,
                    Sr = Spawn("wall", Art.Tile(), pos, Order.Wall, Vector2.one),
                    Dark = Palette.WallDark,
                    Lit = Palette.WallLit,
                    Kind = ch,
                });
                return;
            }

            // every non-wall, non-void cell gets a floor plate underneath
            Add(new Piece
            {
                Cell = c,
                Sr = Spawn("floor", Art.Tile(), pos, Order.Floor, Vector2.one * 0.96f),
                Dark = Palette.FloorDark,
                Lit = Palette.FloorLit,
                Kind = Tile.Floor,
            });

            if (Tile.IsMirror(ch))
            {
                int idx = Level.MirrorIndexAt(c);
                Spawn("mirror-mount", Art.Circle(), pos, Order.Device, Vector2.one * 0.34f)
                    .color = new Color(0.10f, 0.12f, 0.18f);
                var sr = Spawn("mirror", Art.MirrorPlate(), pos, Order.Device + 1, Vector2.one);
                Add(new Piece
                {
                    Cell = c, Sr = sr, Kind = ch, MirrorIndex = idx,
                    Dark = Palette.MirrorDark, Lit = Palette.MirrorLit,
                });
                return;
            }

            switch (ch)
            {
                case Tile.Prism:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch,
                        Sr = Spawn("prism", Art.Diamond(), pos, Order.Device, Vector2.one * 0.9f),
                        Dark = Palette.PrismDark, Lit = Palette.PrismLit,
                    });
                    break;

                case Tile.PlateA:
                case Tile.PlateB:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Sr = Spawn("plate", Art.Ring(), pos, Order.Decor, Vector2.one * 0.86f),
                        Dark = Palette.DeviceDark, Lit = Palette.DeviceLit,
                    });
                    break;

                case Tile.ReceiverA:
                case Tile.ReceiverB:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Sr = Spawn("receiver", Art.Diamond(), pos, Order.Decor, Vector2.one * 0.7f),
                        Dark = Palette.DeviceDark, Lit = Palette.DeviceLit,
                    });
                    break;

                case Tile.Switch:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Sr = Spawn("switch", Art.Circle(), pos, Order.Decor, Vector2.one * 0.55f),
                        Dark = Palette.DeviceDark, Lit = Palette.DeviceLit,
                    });
                    break;

                case Tile.BridgeA:
                case Tile.BridgeB:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Sr = Spawn("bridge", Art.Tile(), pos, Order.Device, Vector2.one * 0.96f),
                        Dark = Palette.DeviceDark, Lit = Palette.DeviceLit,
                    });
                    break;

                case Tile.NightA:
                case Tile.NightB:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Sr = Spawn("nightbloom", Art.Petals(), pos, Order.Device, Vector2.one * 0.96f),
                        Dark = Palette.ShadeDark, Lit = Palette.ShadeOpen,
                    });
                    break;

                case Tile.Lantern:
                {
                    var halo = Spawn("lantern-glow", Art.Glow(), pos, Order.Decor, Vector2.one * 3.2f);
                    halo.color = new Color(Palette.LanternLit.r, Palette.LanternLit.g,
                                           Palette.LanternLit.b, 0f);
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true, Halo = halo,
                        Sr = Spawn("lantern", Art.Lamp(), pos, Order.Device, Vector2.one * 0.92f),
                        Dark = Palette.LanternDark, Lit = Palette.LanternLit,
                    });
                    break;
                }

                case Tile.Focus:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Sr = Spawn("focus", Art.Lens(), pos, Order.Device, Vector2.one * 0.92f),
                        Dark = Palette.FocusDark, Lit = Palette.FocusLit,
                    });
                    break;

                case Tile.FocusDoor:
                case Tile.HeldA:
                case Tile.HeldB:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Halo = Spawn("door-leaf", Art.Tile(), pos, Order.Device, new Vector2(0.44f, 0.9f)),
                        Sr = Spawn("door-leaf", Art.Tile(), pos, Order.Device, new Vector2(0.44f, 0.9f)),
                        Dark = Palette.WallDark, Lit = Palette.DeviceLit,
                    });
                    break;

                case Tile.DoorA:
                case Tile.DoorB:
                case Tile.Gate:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Sr = Spawn("door", Art.Tile(), pos, Order.Device, new Vector2(0.9f, 0.9f)),
                        Dark = Palette.WallDark, Lit = Palette.DeviceLit,
                    });
                    break;

                case Tile.Exit:
                    Add(new Piece
                    {
                        Cell = c, Kind = ch, IgnoreCellLight = true,
                        Sr = Spawn("exit", Art.Ring(), pos, Order.Decor, Vector2.one * 0.95f),
                        Dark = Palette.ExitDark, Lit = Palette.ExitLit,
                    });
                    break;
            }

            if (Tile.IsSource(ch))
            {
                Spawn("source", Art.Square(), pos, Order.Wall, Vector2.one).color = Palette.WallDark;
                float angle = ch switch
                {
                    '>' => 0f, '^' => 90f, '<' => 180f, _ => 270f,
                };
                Spawn("source-arrow", Art.Arrow(), pos, Order.Device, Vector2.one * 0.8f, angle)
                    .color = Palette.BeamCore;
            }

            if (Tile.IsEye(ch))
            {
                int ei = Level.EyeIndexAt(c);
                if (ch == Tile.Ward)
                {
                    // a shell that one beam cannot break
                    var shell = Spawn("ward", Art.Ring(), pos, Order.Eye - 2, Vector2.one * 0.98f);
                    shell.color = new Color(Palette.WardShell.r, Palette.WardShell.g,
                                            Palette.WardShell.b, 0.75f);
                    wardShells[ei] = shell;
                }
                var eye = Spawn("eye", Art.Circle(), pos, Order.Eye, Vector2.one * 0.34f);
                eye.color = Palette.Eye;
                var halo = Spawn("eye-glow", Art.Glow(), pos, Order.Eye - 1, Vector2.one * 1.6f);
                halo.color = new Color(1f, 0.1f, 0.1f, 0.5f);
                halo.transform.SetParent(eye.transform, true);
                eyeVisuals[ei] = eye.transform;
                eyeHome[ei] = c;
            }
        }

        void BuildFog()
        {
            foreach (var region in Level.Clouds)
                foreach (var cell in region.Cells)
                {
                    var sr = Spawn("fog", Art.Square(), cell, Order.Fog, Vector2.one * 1.02f,
                                   0f, fogRoot);
                    sr.color = Palette.Fog;
                    fogPieces.Add(sr);
                }
        }

        static class Order
        {
            public const int Floor = 0;
            public const int Decor = 2;
            public const int Wall = 4;
            public const int Device = 6;
            public const int Fog = 20;
            public const int BeamGlow = 22;
            public const int Beam = 23;
            public const int Eye = 25;
            public const int Player = 27;
        }

        // ------------------------------------------------------------- simulation

        /// <summary>Re-settles the light. Cheap enough to call on every mirror turn.</summary>
        public void Recompute()
        {
            var cleared = State != null ? new HashSet<int>(State.ClearedEyes) : null;
            bool switched = State != null && State.Switched;
            bool ra = State != null && State.ReceiverA;
            bool rb = State != null && State.ReceiverB;
            State = Level.Resolve(Orients, cleared, switched, ra, rb);
        }

        public void RotateMirror(int mirrorIndex, int delta)
        {
            Orients[mirrorIndex] = ((Orients[mirrorIndex] + delta) % 4 + 4) % 4;
            Recompute();
        }

        /// <summary>Index of the rotatable mirror orthogonally adjacent to a cell, or -1.</summary>
        public int MirrorNextTo(Vector2Int cell)
        {
            foreach (var d in LevelDef.Dirs)
            {
                int i = Level.MirrorIndexAt(cell + d);
                if (i >= 0) return i;
            }
            return -1;
        }

        // -------------------------------------------------------------- rendering

        public void Render(Vector2 playerPos, float time)
        {
            ComputeCellLight(playerPos);
            RenderPieces(time);
            RenderBeam(time);
            RenderFogAndEyes(time);
        }

        void ComputeCellLight(Vector2 playerPos)
        {
            cellLight.Clear();
            foreach (var c in State.Lit)
            {
                Bump(c, 1f);
                foreach (var d in LevelDef.Dirs) Bump(c + d, 0.5f);
                Bump(c + new Vector2Int(1, 1), 0.34f);
                Bump(c + new Vector2Int(1, -1), 0.34f);
                Bump(c + new Vector2Int(-1, 1), 0.34f);
                Bump(c + new Vector2Int(-1, -1), 0.34f);
            }

            // The entity carries a faint glow of its own, so you are never fully blind.
            var p = new Vector2Int(Mathf.RoundToInt(playerPos.x), Mathf.RoundToInt(playerPos.y));
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 2.2f) continue;
                    Bump(p + new Vector2Int(dx, dy), Mathf.Lerp(0.42f, 0f, d / 2.2f));
                }
        }

        void Bump(Vector2Int c, float amount)
        {
            cellLight.TryGetValue(c, out float cur);
            if (amount > cur) cellLight[c] = amount;
        }

        float LightAt(Vector2Int c)
        {
            cellLight.TryGetValue(c, out float v);
            return Mathf.Clamp01(Mathf.Max(v, ambient));
        }

        void RenderPieces(float time)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * 3f);

            foreach (var p in pieces)
            {
                float amount = p.IgnoreCellLight ? 0f : LightAt(p.Cell);
                bool active = false;

                switch (p.Kind)
                {
                    case Tile.PlateA: active = State.CircuitA; break;
                    case Tile.PlateB: active = State.CircuitB; break;
                    case Tile.ReceiverA: active = State.ReceiverA; break;
                    case Tile.ReceiverB: active = State.ReceiverB; break;
                    case Tile.Switch: active = State.Switched; break;
                    case Tile.BridgeA: active = State.CircuitA; break;
                    case Tile.BridgeB: active = State.CircuitB; break;
                    case Tile.Focus: active = State.Focused; break;
                    case Tile.NightA: active = !State.CircuitA; break;
                    case Tile.NightB: active = !State.CircuitB; break;
                    case Tile.Lantern: active = State.Lit.Contains(p.Cell); break;
                    case Tile.HeldA:
                    case Tile.HeldB:
                    case Tile.DoorA:
                    case Tile.DoorB:
                    case Tile.Gate: active = State.IsOpen(p.Kind); break;
                    case Tile.Exit:
                        active = State.Lit.Contains(p.Cell) &&
                                 State.ClearedEyes.Count >= Level.Eyes.Count;
                        break;
                }

                if (p.IgnoreCellLight)
                {
                    amount = active ? 1f : Mathf.Max(LightAt(p.Cell) * 0.8f, ambient);
                    if (p.Kind == Tile.Exit && active) amount = 0.75f + 0.25f * pulse;
                }

                var col = Color.Lerp(p.Dark, p.Lit, amount);

                // A held door is two leaves that part to the sides and slam back when
                // the light moves on; a latched door simply thins out to a frame.
                if (p.Kind == Tile.HeldA || p.Kind == Tile.HeldB || p.Kind == Tile.FocusDoor)
                {
                    float part = active ? 0.30f : 0f;
                    col.a = active ? 0.55f : 1f;
                    p.Sr.transform.localPosition = new Vector3(p.Cell.x - part, p.Cell.y, 0f);
                    p.Sr.transform.localScale = new Vector3(active ? 0.28f : 0.46f, 0.9f, 1f);
                    if (p.Halo != null)
                    {
                        p.Halo.color = col;
                        p.Halo.transform.localPosition = new Vector3(p.Cell.x + part, p.Cell.y, 0f);
                        p.Halo.transform.localScale = p.Sr.transform.localScale;
                    }
                }
                else if (Tile.IsDoor(p.Kind))
                {
                    col.a = active ? 0.22f : 1f;
                    p.Sr.transform.localScale = active
                        ? new Vector3(0.9f, 0.16f, 1f)
                        : new Vector3(0.9f, 0.9f, 1f);
                }

                // A bridge only exists while its plate is held in the light,
                // and a nightbloom only while its plate is dark.
                if (p.Kind == Tile.BridgeA || p.Kind == Tile.BridgeB ||
                    p.Kind == Tile.NightA || p.Kind == Tile.NightB)
                {
                    col.a = active ? 1f : 0.10f;
                    p.Sr.transform.localScale = Vector3.one * (active ? 0.96f : 0.55f);
                }

                if (p.Kind == Tile.Lantern && p.Halo != null)
                {
                    float burn = active ? 0.34f + 0.06f * pulse : 0f;
                    var lc = Palette.LanternLit;
                    p.Halo.color = new Color(lc.r, lc.g, lc.b, burn);
                    p.Halo.transform.localScale = Vector3.one * (active ? 3.2f + 0.25f * pulse : 1f);
                }

                p.Sr.color = col;

                if (p.MirrorIndex >= 0)
                {
                    int orient = Orients[p.MirrorIndex];
                    var n = Mirror.Normal(orient);
                    // point the bright face along the mirror's normal
                    float angle = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg - 90f;
                    var t = p.Sr.transform;
                    t.rotation = Quaternion.Lerp(t.rotation, Quaternion.Euler(0, 0, angle),
                                                 1f - Mathf.Exp(-18f * Time.deltaTime));
                }
            }
        }

        void RenderBeam(float time)
        {
            int used = 0;
            float flicker = 0.94f + 0.06f * Mathf.Sin(time * 11f);

            foreach (var seg in State.Segments)
            {
                Vector2 a = seg.A, b = seg.B;
                if (a == b) continue;
                Vector2 mid = (a + b) * 0.5f;
                float len = Vector2.Distance(a, b);
                bool horizontal = Mathf.Approximately(a.y, b.y);

                var core = BeamRenderer(used++, Order.Beam);
                core.transform.position = mid;
                core.transform.localScale = horizontal
                    ? new Vector3(len, 0.13f, 1f)
                    : new Vector3(0.13f, len, 1f);
                core.color = Palette.BeamCore * flicker;

                var glow = BeamRenderer(used++, Order.BeamGlow);
                glow.transform.position = mid;
                glow.transform.localScale = horizontal
                    ? new Vector3(len, 0.6f, 1f)
                    : new Vector3(0.6f, len, 1f);
                var g = Palette.BeamGlow;
                glow.color = new Color(g.r, g.g, g.b, g.a * flicker);
            }

            for (int i = used; i < beamPool.Count; i++) beamPool[i].enabled = false;
        }

        SpriteRenderer BeamRenderer(int index, int order)
        {
            while (beamPool.Count <= index)
                beamPool.Add(Spawn("beam", Art.Square(), Vector2.zero, order, Vector2.one, 0f, beamRoot));
            var sr = beamPool[index];
            sr.enabled = true;
            sr.sortingOrder = order;
            return sr;
        }

        void RenderFogAndEyes(float time)
        {
            foreach (var sr in fogPieces)
            {
                var cell = new Vector2Int(Mathf.RoundToInt(sr.transform.position.x),
                                          Mathf.RoundToInt(sr.transform.position.y));
                bool alive = Level.InLiveCloud(cell, State);
                var region = Level.RegionAt(cell);
                var c = region != null && region.IsGloom ? Palette.GloomFog : Palette.Fog;
                float drift = 0.93f + 0.07f * Mathf.Sin(time * 1.7f + cell.x * 0.7f + cell.y * 1.3f);
                c.a = alive ? drift : 0f;
                sr.color = c;
                sr.enabled = alive;
            }

            roamTimer += Time.deltaTime;

            foreach (var kv in eyeVisuals)
            {
                int ei = kv.Key;
                var t = kv.Value;
                bool dead = State.ClearedEyes.Contains(ei);
                if (wardShells.TryGetValue(ei, out var shell) && shell != null)
                {
                    var sc = shell.color;
                    sc.a = dead ? Mathf.MoveTowards(sc.a, 0f, Time.deltaTime * 2f)
                                : 0.60f + 0.15f * Mathf.Sin(time * 2.2f + ei);
                    shell.color = sc;
                    shell.transform.localScale = Vector3.one *
                        (dead ? Mathf.MoveTowards(shell.transform.localScale.x, 0f, Time.deltaTime)
                              : 0.98f + 0.04f * Mathf.Sin(time * 2.2f + ei));
                }
                if (dead)
                {
                    // fade out and shrink once struck
                    var sr = t.GetComponent<SpriteRenderer>();
                    var col = sr.color;
                    col.a = Mathf.MoveTowards(col.a, 0f, Time.deltaTime * 2f);
                    sr.color = col;
                    t.localScale = Vector3.MoveTowards(t.localScale, Vector3.zero, Time.deltaTime);
                    continue;
                }

                Vector2 target = eyeHome[ei];
                if (Level.At(Level.Eyes[ei]) == Tile.EyeRoaming)
                    target = RoamTarget(ei, time);

                t.position = Vector2.Lerp(t.position, target, 1f - Mathf.Exp(-4f * Time.deltaTime));
                float blink = 0.30f + 0.06f * Mathf.Sin(time * 4f + ei);
                t.localScale = Vector3.one * blink;
            }
        }

        /// <summary>A roaming eye drifts around its own cloud on a slow lissajous path.</summary>
        Vector2 RoamTarget(int eyeIndex, float time)
        {
            var region = Level.RegionOfEye(eyeIndex);
            if (region == null || region.Cells.Count == 0) return eyeHome[eyeIndex];

            float min = float.MaxValue, max = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var c in region.Cells)
            {
                min = Mathf.Min(min, c.x); max = Mathf.Max(max, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
            }
            float t = time * 0.45f + eyeIndex * 2f;
            return new Vector2(Mathf.Lerp(min, max, 0.5f + 0.5f * Mathf.Sin(t)),
                               Mathf.Lerp(minY, maxY, 0.5f + 0.5f * Mathf.Sin(t * 1.6f + 1.1f)));
        }
    }
}
