using System.Collections.Generic;
using FirstLight;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Exhaustive check that every shipped level can actually be finished with the
/// rules the game runs on. The search covers the full reachable state space:
/// where you are standing, which way each mirror faces, which eyes are out and
/// whether the one-shot switch has fired. If a map is ever edited into an
/// unwinnable shape, this fails.
/// </summary>
public class LevelSolvabilityTests
{
    struct Node
    {
        public Vector2Int Pos;
        public int Orients;     // base-4 packed
        public int Cleared;     // bitmask
        public bool Switched;
        public bool ReceiverA, ReceiverB;
    }

    [Test]
    public void EveryLevelIsSolvable()
    {
        var levels = LevelLibrary.Build();
        Assert.AreEqual(20, levels.Count, "expected twenty levels");

        for (int i = 0; i < levels.Count; i++)
        {
            var def = levels[i];
            Assert.LessOrEqual(def.Mirrors.Count, 8, $"level {i + 1} packs too many mirrors to search");
            Assert.IsTrue(Solve(def), $"level {i + 1} \"{def.Name}\" cannot be solved");
        }
    }

    [Test]
    public void EveryLevelHasTheStructureItNeeds()
    {
        var levels = LevelLibrary.Build();
        for (int i = 0; i < levels.Count; i++)
        {
            var def = levels[i];
            string where = $"level {i + 1} \"{def.Name}\"";
            Assert.AreNotEqual(Vector2Int.zero, def.Start, $"{where} has no start");
            Assert.AreEqual(Tile.Start, def.At(def.Start), $"{where} start is not a start tile");
            Assert.IsNotEmpty(def.Sources, $"{where} has no light source");

            int exits = 0;
            for (int x = 0; x < def.Width; x++)
                for (int y = 0; y < def.Height; y++)
                    if (def.At(new Vector2Int(x, y)) == Tile.Exit) exits++;
            Assert.AreEqual(1, exits, $"{where} should have exactly one exit");

            // A cloud with no eye could never be cleared, so it would wall the level off
            // for good. A gloom is different: the beam is always a way through it, so an
            // eyeless gloom is legitimate as permanent dark.
            foreach (var cloud in def.Clouds)
                if (!cloud.IsGloom)
                    Assert.IsNotEmpty(cloud.Eyes, $"{where} has a cloud with no eye");
        }
    }

    static bool Solve(LevelDef def)
    {
        int mirrorCount = def.Mirrors.Count;
        int allEyes = (1 << def.Eyes.Count) - 1;

        int startOrients = 0;
        for (int i = mirrorCount - 1; i >= 0; i--)
            startOrients = startOrients * 4 + def.InitialOrients[i];

        var start = new Node { Pos = def.Start, Orients = startOrients, Cleared = 0, Switched = false };
        var seen = new HashSet<(Vector2Int, int, int, bool, bool, bool)> { Key(start) };
        var queue = new Queue<Node>();
        queue.Enqueue(start);

        var orients = new List<int>(new int[mirrorCount]);
        var cleared = new HashSet<int>();

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            Unpack(node.Orients, mirrorCount, orients);
            cleared.Clear();
            for (int e = 0; e < def.Eyes.Count; e++)
                if ((node.Cleared & (1 << e)) != 0) cleared.Add(e);

            var st = def.Resolve(orients, cleared, node.Switched, node.ReceiverA, node.ReceiverB);

            int settledCleared = 0;
            foreach (int e in st.ClearedEyes) settledCleared |= 1 << e;

            // The ground can go out from under you - a collapsing bridge, a nightbloom
            // closing - which costs you the walk back from the start.
            if (node.Pos != def.Start && !def.Walkable(node.Pos, st))
            {
                var fallen = node;
                fallen.Pos = def.Start;
                fallen.Cleared = settledCleared;
                fallen.Switched = st.Switched;
                fallen.ReceiverA = st.ReceiverA;
                fallen.ReceiverB = st.ReceiverB;
                if (seen.Add(Key(fallen))) queue.Enqueue(fallen);
                continue;
            }

            // Striking an eye, tripping the switch or charging a receiver is
            // irreversible: fold that in and re-visit rather than treat it as a move.
            if (settledCleared != node.Cleared || st.Switched != node.Switched ||
                st.ReceiverA != node.ReceiverA || st.ReceiverB != node.ReceiverB)
            {
                var settled = node;
                settled.Cleared = settledCleared;
                settled.Switched = st.Switched;
                settled.ReceiverA = st.ReceiverA;
                settled.ReceiverB = st.ReceiverB;
                if (seen.Add(Key(settled))) queue.Enqueue(settled);
                continue;
            }

            if (def.At(node.Pos) == Tile.Exit && st.Lit.Contains(node.Pos) && node.Cleared == allEyes)
                return true;

            foreach (var d in LevelDef.Dirs)
            {
                var next = node.Pos + d;
                if (def.InLiveCloud(next, st)) continue;   // stepping in just sends you back
                if (!def.Walkable(next, st)) continue;
                var move = node;
                move.Pos = next;
                if (seen.Add(Key(move))) queue.Enqueue(move);
            }

            for (int m = 0; m < mirrorCount; m++)
            {
                var cell = def.Mirrors[m];
                if (Mathf.Abs(cell.x - node.Pos.x) + Mathf.Abs(cell.y - node.Pos.y) != 1) continue;
                for (int k = 1; k <= 3; k++)
                {
                    Unpack(node.Orients, mirrorCount, orients);
                    orients[m] = (orients[m] + k) % 4;
                    var turn = node;
                    turn.Orients = Pack(orients);
                    if (seen.Add(Key(turn))) queue.Enqueue(turn);
                }
            }
        }
        return false;
    }

    static (Vector2Int, int, int, bool, bool, bool) Key(Node n) =>
        (n.Pos, n.Orients, n.Cleared, n.Switched, n.ReceiverA, n.ReceiverB);

    static void Unpack(int packed, int count, List<int> into)
    {
        for (int i = 0; i < count; i++)
        {
            into[i] = packed & 3;
            packed >>= 2;
        }
    }

    static int Pack(List<int> orients)
    {
        int packed = 0;
        for (int i = orients.Count - 1; i >= 0; i--) packed = (packed << 2) | orients[i];
        return packed;
    }
}
