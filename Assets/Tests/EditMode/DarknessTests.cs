using System.Collections.Generic;
using FirstLight;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The rules for the harder darkness: a gloom you cross by staying in the beam, a
/// ward that one beam cannot break, and a lens that needs two beams crossing on it.
/// </summary>
public class DarknessTests
{
    static readonly List<int> NoMirrors = new();

    // the beam runs east along the middle row of a gloom that fills the room
    const string EyelessGloom = @"
#######
#ggggg#
#>gggg#
#ggggg#
#######";

    const string GloomWithEye = @"
#######
#ggggg#
#>ggeg#
#ggggg#
#######";

    static readonly Vector2Int OnBeam = new(3, 2);
    static readonly Vector2Int OffBeam = new(3, 3);

    [Test]
    public void GloomIsWalkableOnlyWhereTheBeamFalls()
    {
        var def = new LevelDef("gloom", "", EyelessGloom);
        var st = def.Resolve(NoMirrors, null, false);

        Assert.IsTrue(st.Lit.Contains(OnBeam), "expected the beam along the middle row");
        Assert.IsFalse(st.Lit.Contains(OffBeam), "the row above should be off the beam");

        Assert.IsTrue(def.Walkable(OnBeam, st), "a lit gloom cell is the walkway");
        Assert.IsFalse(def.Walkable(OffBeam, st), "an unlit gloom cell must not be walkable");
    }

    [Test]
    public void EyelessGloomStaysDarkForGood()
    {
        var def = new LevelDef("deep", "", EyelessGloom);
        Assert.IsEmpty(def.Eyes, "this gloom deliberately has no eye");
        Assert.IsTrue(def.Clouds[0].IsGloom, "it should be classed as gloom, not cloud");

        var st = def.Resolve(NoMirrors, null, false);
        Assert.IsFalse(def.Walkable(OffBeam, st),
                       "with no eye to strike, off-beam stays deadly for good");
    }

    [Test]
    public void ClearingTheEyeMakesTheWholeGloomSafe()
    {
        // this eye sits directly on the beam, so it dies at once
        var def = new LevelDef("gloom", "", GloomWithEye);
        var st = def.Resolve(NoMirrors, null, false);

        Assert.IsNotEmpty(st.ClearedEyes, "the beam should have struck the eye");
        Assert.IsTrue(def.Walkable(OffBeam, st),
                      "with its eye out the whole gloom is safe, lit or not");
    }

    [Test]
    public void ALanternCanBeStoodOnOnlyWhileItBurns()
    {
        // the beam runs east along the middle row straight into the lamp
        var def = new LevelDef("lamp", "", @"
#######
#ggggg#
#>ggYg#
#ggggg#
#######");
        var lamp = new Vector2Int(4, 2);
        Assert.AreEqual(Tile.Lantern, def.At(lamp));

        var lit = def.Resolve(NoMirrors, null, false);
        Assert.IsTrue(lit.Lit.Contains(lamp), "the beam should reach the lamp");
        Assert.IsTrue(def.Walkable(lamp, lit), "a burning lamp is a stone you can stand on");

        // the lamp is what lets a corridor turn: its side rays are lit too
        Assert.IsTrue(def.Walkable(new Vector2Int(4, 3), lit),
                      "the lamp throws light sideways, so the gloom above it holds you");
        Assert.IsTrue(def.Walkable(new Vector2Int(4, 1), lit),
                      "and below it");
    }

    // A prism splits the beam; each half is walked around to the middle of the bottom
    // row, so the target is struck from the east and the west at the same moment.
    const string TwoBeams = @"
#########
####v####
#.......#
#0..P..0#
#.......#
#.......#
#0..T..0#
#########";

    static LevelDef Split(char target) =>
        new("split", "", TwoBeams.Replace('T', target));

    // mirrors sort by x then y: (1,1) (1,4) (7,1) (7,4)
    static readonly List<int> Converge = new() { 0, 3, 1, 2 };
    static readonly List<int> OneSide = new() { 0, 3, 1, 0 };   // east mirror swallows its half
    static readonly Vector2Int Target = new(4, 1);

    [Test]
    public void TheFixtureReallyDeliversOneBeamOrTwo()
    {
        var def = Split(Tile.Focus);
        Assert.AreEqual(4, def.Mirrors.Count);
        Assert.IsTrue(def.Resolve(OneSide, null, false).Lit.Contains(Target),
                      "one half should still reach the target");
        Assert.IsTrue(def.Resolve(Converge, null, false).Lit.Contains(Target),
                      "both halves should reach the target");
    }

    [Test]
    public void WardSurvivesOneBeamAndBreaksUnderTwo()
    {
        var def = Split(Tile.Ward);
        Assert.AreEqual(1, def.Eyes.Count);

        Assert.IsEmpty(def.Resolve(OneSide, null, false).ClearedEyes,
                       "one beam must not break a ward");
        Assert.IsNotEmpty(def.Resolve(Converge, null, false).ClearedEyes,
                          "two beams at once should break the ward");
    }

    [Test]
    public void PlainEyeStillDiesToASingleBeam()
    {
        var def = Split(Tile.Eye);
        Assert.IsNotEmpty(def.Resolve(OneSide, null, false).ClearedEyes,
                          "an unwarded eye dies to one beam");
    }

    [Test]
    public void FocusHoldsItsDoorOnlyWhileTwoBeamsCross()
    {
        var def = Split(Tile.Focus);

        var single = def.Resolve(OneSide, null, false);
        Assert.IsFalse(single.Focused, "one beam must not charge the lens");
        Assert.IsFalse(single.IsOpen(Tile.FocusDoor), "so its door stays shut");

        var both = def.Resolve(Converge, null, false);
        Assert.IsTrue(both.Focused, "two crossing beams should charge the lens");
        Assert.IsTrue(both.IsOpen(Tile.FocusDoor), "and hold its door open");

        // held, not latched: drop back to one beam and the door shuts again
        var dropped = def.Resolve(OneSide, both.ClearedEyes, both.Switched,
                                  both.ReceiverA, both.ReceiverB);
        Assert.IsFalse(dropped.IsOpen(Tile.FocusDoor),
                       "the focus door is held - losing a beam must shut it");
    }
}
