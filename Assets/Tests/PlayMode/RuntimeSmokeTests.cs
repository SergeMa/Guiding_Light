using System.Collections;
using FirstLight;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Boots the real game object and runs a few frames of every level. Everything is
/// generated at runtime - sprites, level geometry, beams, fog - so this is what
/// proves the whole build-and-render path actually executes. Any logged exception
/// fails the test.
/// </summary>
public class RuntimeSmokeTests
{
    [UnityTest]
    public IEnumerator EveryLevelBootsAndRenders()
    {
        var host = new GameObject("Game");
        var game = host.AddComponent<GameManager>();
        yield return null;   // Awake

        int count = LevelLibrary.Build().Count;
        for (int i = 0; i < count; i++)
        {
            game.BeginAt(i);
            for (int frame = 0; frame < 3; frame++) yield return null;

            var view = host.GetComponentInChildren<LevelView>();
            Assert.IsNotNull(view, $"level {i + 1} did not build a view");
            Assert.AreEqual(LevelLibrary.Build()[i].Name, view.Level.Name);
            Assert.IsNotNull(view.State, $"level {i + 1} produced no light state");
            Assert.IsNotEmpty(view.State.Lit, $"level {i + 1} has a source that lights nothing");
        }

        Object.Destroy(host);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TurningAMirrorChangesTheLight()
    {
        var host = new GameObject("Game");
        var game = host.AddComponent<GameManager>();
        yield return null;

        game.BeginAt(0);
        yield return null;

        var view = host.GetComponentInChildren<LevelView>();
        int before = view.State.Lit.Count;

        // the first level is solved by turning its single mirror to face south-west
        for (int turn = 0; turn < 4; turn++)
        {
            view.RotateMirror(0, 1);
            yield return null;
            if (view.State.Lit.Count != before) break;
        }

        Assert.AreNotEqual(before, view.State.Lit.Count,
                           "rotating the mirror never changed what the beam touches");

        Object.Destroy(host);
        yield return null;
    }
}
