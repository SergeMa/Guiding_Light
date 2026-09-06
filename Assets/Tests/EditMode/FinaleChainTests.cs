using System.Collections.Generic;
using FirstLight;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The last three levels are one tower climbed in three goes: each room begins in
/// the state the one before it ended in - the same mirrors at the angles you left
/// them, standing on the exit you just reached, with more of the shaft opened up.
///
/// That promise is invisible in any single map, so editing one room could break it
/// silently. These tests hold the seam together.
/// </summary>
public class FinaleChainTests
{
    static readonly string[] Chain = { "The Tower", "The Long Climb", "Return to the Sky" };

    static List<LevelDef> Rooms()
    {
        var all = LevelLibrary.Build();
        var rooms = new List<LevelDef>();
        foreach (var name in Chain)
        {
            var found = all.Find(l => l.Name == name);
            Assert.IsNotNull(found, $"the finale chain is missing \"{name}\"");
            rooms.Add(found);
        }
        Assert.AreEqual(all.Count - Chain.Length, all.IndexOf(rooms[0]),
                        "the chain must be the last three levels in the game");
        return rooms;
    }

    static Vector2Int ExitOf(LevelDef def)
    {
        for (int x = 0; x < def.Width; x++)
            for (int y = 0; y < def.Height; y++)
                if (def.At(new Vector2Int(x, y)) == Tile.Exit) return new Vector2Int(x, y);
        Assert.Fail($"{def.Name} has no exit");
        return default;
    }

    [Test]
    public void EachRoomBeginsOnTheExitOfTheOneBefore()
    {
        var rooms = Rooms();
        for (int i = 1; i < rooms.Count; i++)
            Assert.AreEqual(ExitOf(rooms[i - 1]), rooms[i].Start,
                $"\"{rooms[i].Name}\" should start on the exit of \"{rooms[i - 1].Name}\" - " +
                "the player is meant to be standing exactly where they stopped");
    }

    [Test]
    public void MirrorsCarriedOverKeepTheirPositions()
    {
        var rooms = Rooms();
        for (int i = 1; i < rooms.Count; i++)
        {
            var prev = rooms[i - 1];
            var next = rooms[i];
            int shared = 0;
            for (int m = 0; m < next.Mirrors.Count; m++)
            {
                int before = prev.Mirrors.IndexOf(next.Mirrors[m]);
                if (before < 0) continue;      // a mirror the tower only reveals later
                shared++;
                Assert.AreEqual(prev.Mirrors[before], next.Mirrors[m],
                    $"a mirror moved between \"{prev.Name}\" and \"{next.Name}\"");
            }
            Assert.Greater(shared, 0,
                $"\"{next.Name}\" shares no mirrors with \"{prev.Name}\", so nothing carries over");
        }
    }

    [Test]
    public void TheTowerOnlyEverOpensUp()
    {
        var rooms = Rooms();
        for (int i = 1; i < rooms.Count; i++)
        {
            var prev = rooms[i - 1];
            var next = rooms[i];
            Assert.AreEqual(prev.Width, next.Width, "the tower changed width");
            Assert.AreEqual(prev.Height, next.Height, "the tower changed height");

            int revealed = 0;
            for (int x = 0; x < prev.Width; x++)
                for (int y = 0; y < prev.Height; y++)
                {
                    var c = new Vector2Int(x, y);
                    char was = prev.At(c), now = next.At(c);
                    if (was == Tile.Wall && now != Tile.Wall) revealed++;

                    // the exit you used becomes ordinary floor, and the eye you put out
                    // is gone - but nothing you could stand on may turn back into wall
                    if (was != Tile.Wall && was != Tile.Exit)
                        Assert.AreNotEqual(Tile.Wall, now,
                            $"{c} was open in \"{prev.Name}\" and is walled in \"{next.Name}\"");
                }
            Assert.Greater(revealed, 0, $"\"{next.Name}\" reveals no new tower");
        }
    }

    [Test]
    public void TheClearedEyeDoesNotComeBack()
    {
        var rooms = Rooms();
        Assert.IsNotEmpty(rooms[0].Eyes, "the first room of the tower should have its eye");
        for (int i = 1; i < rooms.Count; i++)
            Assert.IsEmpty(rooms[i].Eyes,
                $"\"{rooms[i].Name}\" brings an eye back after it was already put out");
    }
}
