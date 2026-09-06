using System.Collections;
using FirstLight;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// The art and music references live in the scene file as raw GUID and fileID
/// pairs. A wrong id there does not fail loudly - it just resolves to null and the
/// game quietly falls back to its procedural sprites, which looks like nothing is
/// wrong. This loads the shipped scene and checks every reference actually landed.
/// </summary>
public class SceneWiringTests
{
    [UnityTest]
    public IEnumerator ShippedSceneHasItsArtAndMusicAttached()
    {
        yield return SceneManager.LoadSceneAsync("FirstLight", LoadSceneMode.Single);
        yield return null;
        yield return null;

        var game = Object.FindAnyObjectByType<GameManager>();
        Assert.IsNotNull(game, "the scene should hold a GameManager");

        var skin = game.skin;
        Assert.IsNotNull(skin, "the skin should be serialized on the scene object");

        Assert.AreEqual(8, skin.entityFrames.Length,
                        "expected the eight frames of the entity");
        for (int i = 0; i < skin.entityFrames.Length; i++)
            Assert.IsNotNull(skin.entityFrames[i],
                $"entity frame {i} did not resolve - check its fileID in the scene");

        Assert.IsNotNull(skin.mirror, "the mirror sprite did not resolve");
        Assert.IsNotNull(skin.eye, "the creature sprite did not resolve");
        Assert.IsTrue(skin.HasEntity);

        var cues = game.sounds;
        Assert.IsNotNull(cues, "the sound bank should be serialized on the scene object");
        foreach (var (name, clips) in new[]
                 {
                     ("mirror turns", cues.mirrorTurn), ("device on", cues.deviceOn),
                     ("device off", cues.deviceOff), ("enemy down", cues.enemyDown),
                 })
        {
            Assert.IsTrue(SoundBank.Has(clips), $"the {name} cue resolved to nothing");
            foreach (var c in clips)
                Assert.IsNotNull(c, $"a {name} clip did not resolve - check its guid");
        }
        Assert.AreEqual(3, cues.mirrorTurn.Length, "expected three mirror takes");
        Assert.AreEqual(2, cues.enemyDown.Length, "expected two enemy takes");

        Assert.IsNotNull(game.stageMusic, "no music array on the scene object");
        Assert.AreEqual(3, game.stageMusic.Length, "expected three stage tracks");
        for (int i = 0; i < game.stageMusic.Length; i++)
            Assert.IsNotNull(game.stageMusic[i], $"stage track {i + 1} did not resolve");
    }

    [UnityTest]
    public IEnumerator EveryStretchOfTheGameGetsATrack()
    {
        var host = new GameObject("Music");
        var clips = new[]
        {
            AudioClip.Create("a", 64, 1, 8000, false),
            AudioClip.Create("b", 64, 1, 8000, false),
            AudioClip.Create("c", 64, 1, 8000, false),
        };
        var music = Music.Create(host.transform, clips);
        yield return null;

        int count = LevelLibrary.Build().Count;
        var seen = new bool[clips.Length];
        int previous = -1;

        for (int i = 0; i < count; i++)
        {
            int track = music.TrackFor(i, count);
            Assert.GreaterOrEqual(track, 0);
            Assert.Less(track, clips.Length);
            Assert.GreaterOrEqual(track, previous, "the music must never go backwards");
            previous = track;
            seen[track] = true;
        }

        for (int i = 0; i < seen.Length; i++)
            Assert.IsTrue(seen[i], $"track {i + 1} is never reached in {count} levels");

        Object.Destroy(host);
        yield return null;
    }

    [Test]
    public void TheMusicSitsAtOneLevelThroughout()
    {
        int count = LevelLibrary.Build().Count;
        float everywhere = Music.VolumeFor(count - 1, count);   // what the last level plays at

        for (int i = 0; i < count; i++)
            Assert.AreEqual(everywhere, Music.VolumeFor(i, count), 1e-6f,
                $"level {i + 1} plays at a different volume - the music is meant to be flat");

        Assert.That(everywhere, Is.InRange(0f, 1f), "volume has to stay a volume");
        Assert.AreEqual(everywhere, Music.VolumeFor(0, 1), 1e-6f,
                        "a one level game must not divide its way to a different answer");
    }

    [UnityTest]
    public IEnumerator NoMusicWiredIsNotAnError()
    {
        var host = new GameObject("Music");
        var music = Music.Create(host.transform, null);
        yield return null;

        Assert.AreEqual(-1, music.TrackFor(0, 32), "with no clips there is no track");
        music.PlayFor(0, 32);          // must not throw
        yield return null;

        Object.Destroy(host);
        yield return null;
    }
}
