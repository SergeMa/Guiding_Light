using System.Collections;
using System.Collections.Generic;
using FirstLight;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// A door the rules let you walk through but the view draws as a solid slab is
/// unplayable, even though every simulation test still passes. These tests hold the
/// two sides together: what the player sees has to agree with what the player can do.
/// </summary>
public class RenderAgreesWithRulesTests
{
    [UnityTest]
    public IEnumerator EveryDoorIsDrawnOpenExactlyWhenItCanBeWalkedThrough()
    {
        var host = new GameObject("Game");
        var game = host.AddComponent<GameManager>();
        yield return null;

        var levels = LevelLibrary.Build();
        for (int i = 0; i < levels.Count; i++)
        {
            game.BeginAt(i);
            yield return null;

            var view = host.GetComponentInChildren<LevelView>();
            var def = view.Level;

            // sweep the mirrors so doors are seen both shut and open
            for (int sweep = 0; sweep < 2; sweep++)
            {
                for (int m = 0; m < def.Mirrors.Count; m++)
                {
                    view.RotateMirror(m, 1);
                    CheckDoors(view, def, i);
                }
            }
            CheckDoors(view, def, i);
        }

        Object.Destroy(host);
        yield return null;
    }

    static void CheckDoors(LevelView view, LevelDef def, int levelIndex)
    {
        for (int x = 0; x < def.Width; x++)
            for (int y = 0; y < def.Height; y++)
            {
                var cell = new Vector2Int(x, y);
                char ch = def.At(cell);
                if (!Tile.IsDoor(ch)) continue;

                bool drawnOpen = view.ActiveFor(ch, cell);
                bool walkable = def.Walkable(cell, view.State);
                Assert.AreEqual(walkable, drawnOpen,
                    $"level {levelIndex + 1} \"{def.Name}\": door '{ch}' at {cell} is " +
                    $"{(walkable ? "walkable" : "blocked")} but drawn " +
                    $"{(drawnOpen ? "open" : "shut")}");
            }
    }

    /// <summary>
    /// The specific report this came from: on Focus the lens lights up, but the door it
    /// holds was drawn shut, so the only way into the exit chamber looked like wall.
    /// </summary>
    [UnityTest]
    public IEnumerator FocusDoorOpensOnceBothBeamsCross()
    {
        var host = new GameObject("Game");
        var game = host.AddComponent<GameManager>();
        yield return null;

        var levels = LevelLibrary.Build();
        int focusLevel = levels.FindIndex(l => l.Name == "Focus");
        Assert.GreaterOrEqual(focusLevel, 0, "expected a level named Focus");

        game.BeginAt(focusLevel);
        yield return null;

        var view = host.GetComponentInChildren<LevelView>();
        var def = view.Level;
        var door = FindTile(def, Tile.FocusDoor);
        var lens = FindTile(def, Tile.Focus);

        Assert.IsFalse(view.State.Focused, "the lens should start uncharged");
        Assert.IsFalse(view.ActiveFor(Tile.FocusDoor, door), "so its door starts shut");

        // the solved pose: both halves of the split walked round onto the lens
        var solved = new Dictionary<Vector2Int, int>
        {
            [new Vector2Int(2, 5)] = 3,
            [new Vector2Int(12, 5)] = 2,
            [new Vector2Int(12, 1)] = 1,
        };
        for (int m = 0; m < def.Mirrors.Count; m++)
            if (solved.TryGetValue(def.Mirrors[m], out int want))
                view.Orients[m] = want;
        view.Recompute();
        yield return null;

        Assert.IsTrue(view.State.Lit.Contains(lens), "the lens should be lit");
        Assert.IsTrue(view.State.Focused, "two beams cross the lens, so it should be charged");
        Assert.IsTrue(def.Walkable(door, view.State), "its door should let the player through");
        Assert.IsTrue(view.ActiveFor(Tile.FocusDoor, door), "and it must be drawn open");

        Object.Destroy(host);
        yield return null;
    }

    static Vector2Int FindTile(LevelDef def, char tile)
    {
        for (int x = 0; x < def.Width; x++)
            for (int y = 0; y < def.Height; y++)
                if (def.At(new Vector2Int(x, y)) == tile) return new Vector2Int(x, y);
        Assert.Fail($"no '{tile}' tile in {def.Name}");
        return default;
    }
}
