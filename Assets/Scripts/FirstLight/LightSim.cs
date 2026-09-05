using System.Collections.Generic;
using UnityEngine;

namespace FirstLight
{
    /// <summary>Tile glyphs used by the ASCII level maps.</summary>
    public static class Tile
    {
        public const char Wall = '#';
        public const char Floor = '.';
        public const char Void = '_';
        public const char Prism = 'P';
        public const char PlateA = 'p';
        public const char BridgeA = 'b';
        public const char PlateB = 'q';
        public const char BridgeB = 'c';
        public const char ReceiverA = 'R';
        public const char DoorA = 'D';
        public const char ReceiverB = 'r';
        public const char DoorB = 'd';
        public const char Switch = 'T';
        public const char Gate = 'G';
        public const char HeldA = 'H';      // open only while plate A is lit
        public const char HeldB = 'h';
        public const char NightA = 'n';     // open only while plate A is dark
        public const char NightB = 'm';
        public const char Lantern = 'Y';    // burns while lit, casting light four ways
        public const char Cloud = 'C';
        public const char Gloom = 'g';        // dark room: only the lit cells are safe
        public const char Eye = 'e';
        public const char EyeRoaming = 'o';
        public const char Ward = 'w';         // eye behind a shell: needs two beams at once
        public const char Focus = 'F';        // lens: charged while two beams cross it
        public const char FocusDoor = 'K';    // held open by a charged focus
        public const char Exit = 'X';
        public const char Start = '@';

        public static bool IsMirror(char c) => c >= '0' && c <= '3';
        public static bool IsSource(char c) => c == '>' || c == '<' || c == '^' || c == 'v';
        public static bool IsEye(char c) => c == Eye || c == EyeRoaming || c == Ward;
        public static bool IsCloudish(char c) => c == Cloud || c == Gloom || IsEye(c);
        public static bool IsDoor(char c) =>
            c == DoorA || c == DoorB || c == Gate || c == HeldA || c == HeldB ||
            c == FocusDoor;

        public static Vector2Int SourceDir(char c) => c switch
        {
            '>' => Vector2Int.right,
            '<' => Vector2Int.left,
            '^' => Vector2Int.up,
            _ => Vector2Int.down,
        };

        /// <summary>Beam passes over these without being stopped.</summary>
        public static bool BeamPasses(char c) =>
            c == Floor || c == Void || c == PlateA || c == PlateB || c == ReceiverA ||
            c == ReceiverB || c == Switch || c == Exit || c == Start || c == BridgeA ||
            c == BridgeB || c == Cloud || c == NightA || c == NightB ||
            c == Gloom || c == Focus;

        /// <summary>Walkable ignoring circuit state (see LevelDef.Walkable for the full test).</summary>
        public static bool WalkableBase(char c) =>
            c == Floor || c == PlateA || c == PlateB || c == ReceiverA || c == ReceiverB ||
            c == Switch || c == Exit || c == Start || IsCloudish(c);
    }

    /// <summary>
    /// One-sided 45 degree mirrors. Orientation is the index of the reflective face normal:
    /// 0 = NE, 1 = NW, 2 = SW, 3 = SE. A beam arriving at the back face is absorbed,
    /// which is what makes a four-step rotation meaningful.
    /// </summary>
    public static class Mirror
    {
        public static bool Reflect(int orient, Vector2Int inDir, out Vector2Int outDir)
        {
            outDir = Vector2Int.zero;
            switch (orient)
            {
                case 0: // normal NE, drawn "\"
                    if (inDir == Vector2Int.left) { outDir = Vector2Int.up; return true; }
                    if (inDir == Vector2Int.down) { outDir = Vector2Int.right; return true; }
                    return false;
                case 1: // normal NW, drawn "/"
                    if (inDir == Vector2Int.right) { outDir = Vector2Int.up; return true; }
                    if (inDir == Vector2Int.down) { outDir = Vector2Int.left; return true; }
                    return false;
                case 2: // normal SW, drawn "\"
                    if (inDir == Vector2Int.right) { outDir = Vector2Int.down; return true; }
                    if (inDir == Vector2Int.up) { outDir = Vector2Int.left; return true; }
                    return false;
                default: // 3, normal SE, drawn "/"
                    if (inDir == Vector2Int.left) { outDir = Vector2Int.down; return true; }
                    if (inDir == Vector2Int.up) { outDir = Vector2Int.right; return true; }
                    return false;
            }
        }

        /// <summary>Face normal in world space, used for drawing.</summary>
        public static Vector2 Normal(int orient) => orient switch
        {
            0 => new Vector2(0.7071f, 0.7071f),
            1 => new Vector2(-0.7071f, 0.7071f),
            2 => new Vector2(-0.7071f, -0.7071f),
            _ => new Vector2(0.7071f, -0.7071f),
        };

        /// <summary>Angle of the mirror plate itself (the reflective line), in degrees.</summary>
        public static float PlateAngle(int orient) => (orient == 0 || orient == 2) ? -45f : 45f;
    }

    public struct BeamSegment
    {
        public Vector2Int A, B;
        public BeamSegment(Vector2Int a, Vector2Int b) { A = a; B = b; }
    }

    /// <summary>The settled state of the light for a given set of mirror orientations.</summary>
    public class LightState
    {
        public readonly HashSet<Vector2Int> Lit = new();
        public readonly List<BeamSegment> Segments = new();
        public readonly HashSet<int> ClearedEyes = new();
        public bool Switched;
        public bool CircuitA, CircuitB, ReceiverA, ReceiverB;
        public bool Focused;

        public bool IsOpen(char door) => door switch
        {
            Tile.DoorA => ReceiverA,     // latched: lit once, open for good
            Tile.DoorB => ReceiverB,
            Tile.Gate => Switched,
            Tile.HeldA => CircuitA,      // held: shuts the moment the light moves on
            Tile.HeldB => CircuitB,
            Tile.FocusDoor => Focused,   // held by two beams crossing at once
            _ => false,
        };
    }

    public class CloudRegion
    {
        public readonly List<Vector2Int> Cells = new();
        public readonly List<int> Eyes = new();

        /// <summary>A gloom is crossable along the beam; a cloud never is.</summary>
        public bool IsGloom;
    }

    /// <summary>A parsed level plus the light simulation that runs on it.</summary>
    public class LevelDef
    {
        public readonly string Name;
        public readonly string Idea;
        public readonly int Width, Height;
        readonly char[,] grid;              // [x, y] with y increasing upward

        public Vector2Int Start;
        public readonly List<(Vector2Int cell, Vector2Int dir)> Sources = new();
        public readonly List<Vector2Int> Mirrors = new();
        public readonly List<int> InitialOrients = new();
        public readonly List<Vector2Int> Eyes = new();
        public readonly List<CloudRegion> Clouds = new();
        readonly Dictionary<Vector2Int, CloudRegion> regionByCell = new();

        public LevelDef(string name, string idea, string map)
        {
            Name = name;
            Idea = idea;
            string[] rows = map.Trim('\n', '\r').Replace("\r", "").Split('\n');
            Height = rows.Length;
            Width = 0;
            foreach (var r in rows) Width = Mathf.Max(Width, r.Length);
            grid = new char[Width, Height];

            for (int ry = 0; ry < Height; ry++)
            {
                int y = Height - 1 - ry;
                for (int x = 0; x < Width; x++)
                {
                    char ch = x < rows[ry].Length ? rows[ry][x] : Tile.Wall;
                    if (ch == ' ') ch = Tile.Wall;
                    grid[x, y] = ch;
                    var c = new Vector2Int(x, y);
                    if (ch == Tile.Start) Start = c;
                    else if (Tile.IsSource(ch)) Sources.Add((c, Tile.SourceDir(ch)));
                    else if (Tile.IsMirror(ch)) Mirrors.Add(c);
                    else if (Tile.IsEye(ch)) Eyes.Add(c);
                }
            }

            // Deterministic ordering so saved orientations always line up.
            Mirrors.Sort(Compare);
            Eyes.Sort(Compare);
            foreach (var m in Mirrors) InitialOrients.Add(grid[m.x, m.y] - '0');
            BuildCloudRegions();
        }

        static int Compare(Vector2Int a, Vector2Int b) => a.x != b.x ? a.x - b.x : a.y - b.y;

        public char At(Vector2Int c) =>
            (c.x < 0 || c.y < 0 || c.x >= Width || c.y >= Height) ? Tile.Wall : grid[c.x, c.y];

        public int MirrorIndexAt(Vector2Int c) => Mirrors.IndexOf(c);
        public int EyeIndexAt(Vector2Int c) => Eyes.IndexOf(c);

        void BuildCloudRegions()
        {
            var seen = new HashSet<Vector2Int>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (!Tile.IsCloudish(At(c)) || seen.Contains(c)) continue;
                    var region = new CloudRegion();
                    var queue = new Queue<Vector2Int>();
                    queue.Enqueue(c);
                    seen.Add(c);
                    while (queue.Count > 0)
                    {
                        var cur = queue.Dequeue();
                        region.Cells.Add(cur);
                        foreach (var d in Dirs)
                        {
                            var nb = cur + d;
                            if (!seen.Contains(nb) && Tile.IsCloudish(At(nb)))
                            {
                                seen.Add(nb);
                                queue.Enqueue(nb);
                            }
                        }
                    }
                    foreach (var cell in region.Cells)
                    {
                        int ei = EyeIndexAt(cell);
                        if (ei >= 0) region.Eyes.Add(ei);
                        if (At(cell) == Tile.Gloom) region.IsGloom = true;
                    }
                    foreach (var cell in region.Cells) regionByCell[cell] = region;
                    Clouds.Add(region);
                }
        }

        public static readonly Vector2Int[] Dirs =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left
        };

        public CloudRegion RegionOfEye(int eyeIndex)
        {
            foreach (var r in Clouds)
                if (r.Eyes.Contains(eyeIndex)) return r;
            return null;
        }

        public bool CloudAlive(CloudRegion r, HashSet<int> cleared)
        {
            // An eyeless gloom is simply permanent dark; an eyeless cloud is nothing.
            if (r.Eyes.Count == 0) return r.IsGloom;
            foreach (int e in r.Eyes)
                if (!cleared.Contains(e)) return true;
            return false;
        }

        public CloudRegion RegionAt(Vector2Int c) =>
            regionByCell.TryGetValue(c, out var r) ? r : null;

        /// <summary>
        /// True where the darkness would take you. A cloud is deadly everywhere until
        /// its eye is out; a gloom only where the beam is not currently falling, so the
        /// lit cells are a walkway you have to keep alive.
        /// </summary>
        public bool InLiveCloud(Vector2Int c, LightState st)
        {
            if (!Tile.IsCloudish(At(c))) return false;
            var r = RegionAt(c);
            if (r == null || !CloudAlive(r, st.ClearedEyes)) return false;
            return !(r.IsGloom && st.Lit.Contains(c));
        }

        public bool Walkable(Vector2Int c, LightState st)
        {
            char ch = At(c);
            if (Tile.IsCloudish(ch)) return !InLiveCloud(c, st);
            // A lamp is a stone you can stand on only while it burns - which makes it
            // the one place a light corridor can turn a corner inside a gloom.
            if (ch == Tile.Lantern) return st.Lit.Contains(c);
            if (ch == Tile.BridgeA) return st.CircuitA;
            if (ch == Tile.BridgeB) return st.CircuitB;
            // a nightbloom is the inverse of a held door: light closes it
            if (ch == Tile.NightA) return !st.CircuitA;
            if (ch == Tile.NightB) return !st.CircuitB;
            if (Tile.IsDoor(ch)) return st.IsOpen(ch);
            return Tile.WalkableBase(ch);
        }

        // ---------------------------------------------------------------- light

        /// <summary>
        /// Settles the beam. Light opens doors, an opened door lets more light through,
        /// and a struck eye stops absorbing - so the trace is repeated until nothing
        /// changes. Every step only adds light, so this always converges.
        ///
        /// Receivers, the switch and struck eyes latch, so they are carried in from the
        /// previous state. Plates are held, so the bridges, held doors and nightblooms
        /// they drive are re-read from the settled light every time.
        /// </summary>
        public LightState Resolve(IList<int> orients, HashSet<int> alreadyCleared,
                                  bool alreadySwitched, bool receiverA = false, bool receiverB = false)
        {
            var st = new LightState
            {
                Switched = alreadySwitched,
                ReceiverA = receiverA,
                ReceiverB = receiverB,
            };
            if (alreadyCleared != null) st.ClearedEyes.UnionWith(alreadyCleared);

            for (int pass = 0; pass < 8; pass++)
            {
                var prevDoors = (st.ReceiverA, st.ReceiverB, st.Switched, st.Focused);
                int prevCleared = st.ClearedEyes.Count;

                Trace(orients, st);

                st.CircuitA = AnyLit(st, Tile.PlateA);
                st.CircuitB = AnyLit(st, Tile.PlateB);
                st.ReceiverA = st.ReceiverA || AnyLit(st, Tile.ReceiverA);
                st.ReceiverB = st.ReceiverB || AnyLit(st, Tile.ReceiverB);
                st.Switched = st.Switched || AnyLit(st, Tile.Switch);

                if (prevDoors == (st.ReceiverA, st.ReceiverB, st.Switched, st.Focused) &&
                    prevCleared == st.ClearedEyes.Count && pass > 0)
                    break;
            }
            return st;
        }

        bool AnyLit(LightState st, char tile)
        {
            foreach (var c in st.Lit)
                if (At(c) == tile) return true;
            return false;
        }

        readonly Dictionary<Vector2Int, HashSet<Vector2Int>> arrivals = new();

        void Trace(IList<int> orients, LightState st)
        {
            st.Lit.Clear();
            st.Segments.Clear();
            arrivals.Clear();

            var pending = new Stack<(Vector2Int cell, Vector2Int dir)>();
            var seen = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (var s in Sources)
            {
                pending.Push(s);
                st.Lit.Add(s.cell);
            }

            int guard = 0;
            while (pending.Count > 0 && guard++ < 4096)
            {
                var (cell, dir) = pending.Pop();
                if (!seen.Add((cell, dir))) continue;

                var segStart = cell;
                var cur = cell;
                while (guard++ < 4096)
                {
                    var next = cur + dir;
                    char ch = At(next);

                    if (Tile.IsMirror(ch))
                    {
                        st.Lit.Add(next);
                        st.Segments.Add(new BeamSegment(segStart, next));
                        int orient = orients[MirrorIndexAt(next)];
                        if (!Mirror.Reflect(orient, dir, out var outDir)) break;
                        dir = outDir;
                        cur = next;
                        segStart = next;
                        if (!seen.Add((cur, dir))) break;
                        continue;
                    }

                    if (ch == Tile.Lantern)
                    {
                        // catches the beam and burns, throwing light four ways;
                        // it goes out the instant the beam stops arriving
                        st.Lit.Add(next);
                        st.Segments.Add(new BeamSegment(segStart, next));
                        foreach (var nd in Dirs) pending.Push((next, nd));
                        break;
                    }

                    if (ch == Tile.Prism)
                    {
                        st.Lit.Add(next);
                        st.Segments.Add(new BeamSegment(segStart, next));
                        pending.Push((next, new Vector2Int(-dir.y, dir.x)));
                        pending.Push((next, new Vector2Int(dir.y, -dir.x)));
                        break;
                    }

                    if (Tile.IsEye(ch))
                    {
                        st.Lit.Add(next);
                        int ei = EyeIndexAt(next);
                        if (!st.ClearedEyes.Contains(ei))
                        {
                            st.Segments.Add(new BeamSegment(segStart, next));
                            RecordArrival(next, dir);
                            // a plain eye dies to one beam; a warded one needs two at once
                            if (ch != Tile.Ward) st.ClearedEyes.Add(ei);
                            break;
                        }
                        cur = next;
                        continue;
                    }

                    bool doorOpen = Tile.IsDoor(ch) && st.IsOpen(ch);
                    if (Tile.BeamPasses(ch) || doorOpen)
                    {
                        st.Lit.Add(next);
                        if (ch == Tile.Focus) RecordArrival(next, dir);
                        // A roaming eye patrols its whole cloud, so any beam crossing
                        // that cloud will eventually meet it.
                        for (int ei = 0; ei < Eyes.Count; ei++)
                        {
                            if (st.ClearedEyes.Contains(ei)) continue;
                            if (At(Eyes[ei]) != Tile.EyeRoaming) continue;
                            var region = RegionOfEye(ei);
                            if (region != null && region.Cells.Contains(next)) st.ClearedEyes.Add(ei);
                        }
                        cur = next;
                        continue;
                    }

                    st.Segments.Add(new BeamSegment(segStart, cur));
                    break;
                }
            }

            // Two beams from different directions break a ward and charge a lens.
            st.Focused = false;
            foreach (var kv in arrivals)
            {
                if (kv.Value.Count < 2) continue;
                char ch = At(kv.Key);
                if (ch == Tile.Ward) st.ClearedEyes.Add(EyeIndexAt(kv.Key));
                else if (ch == Tile.Focus) st.Focused = true;
            }
        }

        void RecordArrival(Vector2Int cell, Vector2Int dir)
        {
            if (!arrivals.TryGetValue(cell, out var dirs))
                arrivals[cell] = dirs = new HashSet<Vector2Int>();
            dirs.Add(dir);
        }

        public bool IsWin(Vector2Int playerCell, LightState st) =>
            At(playerCell) == Tile.Exit &&
            st.Lit.Contains(playerCell) &&
            st.ClearedEyes.Count >= Eyes.Count;
    }
}
