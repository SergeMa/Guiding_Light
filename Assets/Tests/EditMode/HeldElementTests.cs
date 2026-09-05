using System.Collections.Generic;
using FirstLight;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The rules that separate the two families of device: held things work only while
/// the beam is actually on their plate, latched things stay on once charged.
/// </summary>
public class HeldElementTests
{
    // beam runs east into a mirror, which can send it down past a plate and a receiver
    const string PlateAndReceiver = @"
#######
#>...0#
#....p#
#....R#
#######";

    static readonly List<int> Aimed = new() { 2 };    // E -> S, lights both
    static readonly List<int> Away = new() { 0 };     // absorbs the beam, lights neither

    [Test]
    public void PlateIsHeldButReceiverLatches()
    {
        var def = new LevelDef("held", "", PlateAndReceiver);

        var lit = def.Resolve(Aimed, null, false);
        Assert.IsTrue(lit.CircuitA, "plate should read as lit while the beam is on it");
        Assert.IsTrue(lit.ReceiverA, "receiver should charge while the beam is on it");

        // turn the mirror away, carrying the latched receiver forward
        var dark = def.Resolve(Away, lit.ClearedEyes, lit.Switched, lit.ReceiverA, lit.ReceiverB);
        Assert.IsFalse(dark.CircuitA, "plate is held - it must drop when the light moves on");
        Assert.IsTrue(dark.ReceiverA, "receiver is latched - it must stay charged");
    }

    [Test]
    public void HeldDoorClosesWithItsPlateWhileALatchedDoorStaysOpen()
    {
        var def = new LevelDef("held", "", PlateAndReceiver);

        var lit = def.Resolve(Aimed, null, false);
        Assert.IsTrue(lit.IsOpen(Tile.HeldA), "held door should be open while the plate is lit");
        Assert.IsTrue(lit.IsOpen(Tile.DoorA), "latched door should be open once charged");

        var dark = def.Resolve(Away, lit.ClearedEyes, lit.Switched, lit.ReceiverA, lit.ReceiverB);
        Assert.IsFalse(dark.IsOpen(Tile.HeldA), "held door must shut when the light moves on");
        Assert.IsTrue(dark.IsOpen(Tile.DoorA), "latched door must stay open");
    }

    [Test]
    public void NightbloomIsWalkableOnlyWhileItsPlateIsDark()
    {
        var def = new LevelDef("bloom", "", @"
#######
#>...0#
#....p#
#..n..#
#######");
        var bloom = new Vector2Int(3, 1);
        Assert.AreEqual(Tile.NightA, def.At(bloom), "expected the nightbloom at 3,1");

        Assert.IsFalse(def.Walkable(bloom, def.Resolve(Aimed, null, false)),
                       "light should close the nightbloom");
        Assert.IsTrue(def.Walkable(bloom, def.Resolve(Away, null, false)),
                      "the nightbloom should open again in the dark");
    }

    [Test]
    public void LanternCastsLightThatTheBeamAloneCouldNotReach()
    {
        const string map = @"
#######
#>..Y.#
#.....#
#.....#
#######";
        var below = new Vector2Int(4, 2);   // directly under the lantern, off the beam line
        var noMirrors = new List<int>();

        var withLantern = new LevelDef("lantern", "", map).Resolve(noMirrors, null, false);
        Assert.IsTrue(withLantern.Lit.Contains(below),
                      "a burning lantern should light the cell below it");

        var withoutLantern = new LevelDef("plain", "", map.Replace('Y', '.'))
            .Resolve(noMirrors, null, false);
        Assert.IsFalse(withoutLantern.Lit.Contains(below),
                       "without the lantern that cell is nowhere near the beam");
    }

    [Test]
    public void LanternGoesOutWhenTheBeamStopsArriving()
    {
        var def = new LevelDef("lantern", "", @"
#######
#>...0#
#....Y#
#.....#
#######");
        var lantern = new Vector2Int(5, 2);

        Assert.IsTrue(def.Resolve(Aimed, null, false).Lit.Contains(lantern),
                      "lantern should burn while the beam reaches it");
        Assert.IsFalse(def.Resolve(Away, null, false).Lit.Contains(lantern),
                       "lantern should go out the moment the beam is turned away");
    }
}
