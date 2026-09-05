using System.Collections;
using FirstLight;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// The pad covers the bottom of the screen, so the camera has to keep the level
/// above it - otherwise the last row of every level is played under a thumb. And
/// the desktop framing must come out exactly as it did before any of this existed.
/// </summary>
public class MobileCameraTests
{
    static GameManager Boot(TouchControlsMode mode)
    {
        var host = new GameObject("Game");
        host.SetActive(false);                       // set the field before Awake runs
        var game = host.AddComponent<GameManager>();
        game.touchControls = mode;
        host.SetActive(true);
        return game;
    }

    /// <summary>World rectangle the camera can actually show.</summary>
    static Rect Visible(Camera cam)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        var p = cam.transform.position;
        return new Rect(p.x - halfW, p.y - halfH, halfW * 2f, halfH * 2f);
    }

    [UnityTest]
    public IEnumerator EveryLevelFitsClearOfTheTouchPad()
    {
        var game = Boot(TouchControlsMode.Always);
        yield return null;

        // the same pure function the game uses, so the band is the real one
        var pad = new TouchControls { Mode = TouchControlsMode.Always };
        float reserved = pad.ReservedBottomFor(Screen.width, Screen.height);
        Assert.Greater(reserved, 0f, "the pad should be reserving a band to test against");

        var levels = LevelLibrary.Build();
        for (int i = 0; i < levels.Count; i++)
        {
            game.BeginAt(i);
            yield return null;

            var def = levels[i];
            var cam = Camera.main;
            var seen = Visible(cam);
            float bandTop = seen.yMin + reserved * seen.height;   // floor of the playable area

            Assert.LessOrEqual(seen.xMin, -0.5f, $"level {i + 1} is cut off on the left");
            Assert.GreaterOrEqual(seen.xMax, def.Width - 0.5f, $"level {i + 1} is cut off on the right");
            Assert.LessOrEqual(bandTop, -0.5f,
                $"level {i + 1} has its bottom row under the touch pad");
            Assert.GreaterOrEqual(seen.yMax, def.Height - 0.5f, $"level {i + 1} is cut off at the top");
        }

        Object.Destroy(game.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator DesktopFramingIsUnchanged()
    {
        var mobile = Boot(TouchControlsMode.Always);
        mobile.BeginAt(0);
        yield return null;
        float mobileSize = Camera.main.orthographicSize;
        Object.Destroy(mobile.gameObject);
        yield return null;

        var desktop = Boot(TouchControlsMode.Auto);   // off anywhere but a phone
        desktop.BeginAt(0);
        yield return null;

        var def = LevelLibrary.Build()[0];
        var cam = Camera.main;

        // exactly the old rule: fit the level, centred, with no band reserved
        float aspect = Mathf.Max(cam.aspect, 0.4f);
        float expected = Mathf.Max(def.Height * 0.5f + 1.2f, (def.Width * 0.5f + 1.2f) / aspect);
        Assert.AreEqual(expected, cam.orthographicSize, 0.001f,
                        "desktop must frame the level the way it always did");
        Assert.AreEqual((def.Width - 1) * 0.5f, cam.transform.position.x, 0.001f);
        Assert.AreEqual((def.Height - 1) * 0.5f, cam.transform.position.y, 0.001f,
                        "desktop must not drop the camera for a pad that is not there");

        Assert.Greater(mobileSize, cam.orthographicSize,
                       "the mobile framing should pull back to make room for the pad");

        Object.Destroy(desktop.gameObject);
        yield return null;
    }
}
