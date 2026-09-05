using System.Collections.Generic;
using FirstLight;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The on-screen pad. The first and most important property is that it does not
/// exist anywhere but a phone or tablet: on desktop it is inert and takes no room.
/// The rest is layout and hit-testing, which are pure functions of the screen size
/// and so can be checked here without a device.
/// </summary>
public class TouchControlsTests
{
    const float W = 1280f, H = 720f;   // a landscape phone

    static TouchControls Mobile() => new() { Mode = TouchControlsMode.Always };

    [Test]
    public void AutoIsOffOnDesktopSoNothingChangesThere()
    {
        var pad = new TouchControls();   // Auto
        Assert.IsFalse(pad.Active,
            "Auto must stay off anywhere that is not Android or iOS - these tests run on desktop");
        Assert.AreEqual(0f, pad.ReservedBottomFor(W, H),
            "an inactive pad must not reserve any screen for itself");
    }

    [Test]
    public void NeverStaysOffAndAlwaysTurnsOn()
    {
        Assert.IsFalse(new TouchControls { Mode = TouchControlsMode.Never }.Active);
        Assert.IsTrue(Mobile().Active);
        Assert.Greater(Mobile().ReservedBottomFor(W, H), 0f);
    }

    [Test]
    public void AnInactivePadReadsNothingEvenWhereAButtonWouldBe()
    {
        var off = new TouchControls();          // desktop
        var on = Mobile();
        var spot = on.RectFor(TouchControls.Button.Up, W, H).center;

        var state = off.Evaluate(new List<Vector2> { spot }, W, H);
        Assert.AreEqual(Vector2Int.zero, state.Move,
            "a press where a button would be must do nothing while the pad is off");
        Assert.IsFalse(state.TurnCcw);
        Assert.IsFalse(state.Restart);
    }

    [Test]
    public void ButtonsDoNotOverlapAndStayOnScreen()
    {
        var pad = Mobile();
        var rects = new List<(TouchControls.Button, Rect)>();
        foreach (var b in TouchControls.All)
        {
            var r = pad.RectFor(b, W, H);
            Assert.GreaterOrEqual(r.xMin, 0f, $"{b} runs off the left");
            Assert.GreaterOrEqual(r.yMin, 0f, $"{b} runs off the top");
            Assert.LessOrEqual(r.xMax, W, $"{b} runs off the right");
            Assert.LessOrEqual(r.yMax, H, $"{b} runs off the bottom");
            Assert.GreaterOrEqual(Mathf.Min(r.width, r.height), 40f,
                                  $"{b} is too small to hit with a thumb");
            rects.Add((b, r));
        }

        for (int i = 0; i < rects.Count; i++)
            for (int j = i + 1; j < rects.Count; j++)
                Assert.IsFalse(rects[i].Item2.Overlaps(rects[j].Item2),
                    $"{rects[i].Item1} overlaps {rects[j].Item1}");
    }

    [Test]
    public void EachDirectionReadsAsThatDirection()
    {
        var pad = Mobile();
        var expected = new Dictionary<TouchControls.Button, Vector2Int>
        {
            [TouchControls.Button.Up] = Vector2Int.up,
            [TouchControls.Button.Down] = Vector2Int.down,
            [TouchControls.Button.Left] = Vector2Int.left,
            [TouchControls.Button.Right] = Vector2Int.right,
        };
        foreach (var kv in expected)
        {
            var point = pad.RectFor(kv.Key, W, H).center;
            Assert.AreEqual(kv.Value, pad.Evaluate(new List<Vector2> { point }, W, H).Move,
                            $"{kv.Key} should read as {kv.Value}");
        }
    }

    [Test]
    public void AHeldDirectionKeepsReadingWhileATurnFiresOnlyOnce()
    {
        var pad = Mobile();
        var right = pad.RectFor(TouchControls.Button.Right, W, H).center;
        var turn = pad.RectFor(TouchControls.Button.TurnCcw, W, H).center;
        var both = new List<Vector2> { right, turn };

        var first = pad.Evaluate(both, W, H);
        Assert.AreEqual(Vector2Int.right, first.Move, "two thumbs at once should both register");
        Assert.IsTrue(first.TurnCcw, "the turn should fire on the frame it goes down");

        var second = pad.Evaluate(both, W, H);
        Assert.AreEqual(Vector2Int.right, second.Move,
                        "a held direction keeps reading, so movement repeats");
        Assert.IsFalse(second.TurnCcw,
                       "a held turn must not fire again - one press, one turn");

        pad.Evaluate(new List<Vector2>(), W, H);
        Assert.IsTrue(pad.Evaluate(both, W, H).TurnCcw, "and it fires again after release");
    }

    [Test]
    public void TapAnywhereIsReportedOnceForTheTitleScreen()
    {
        var pad = Mobile();
        var somewhere = new List<Vector2> { new(W * 0.5f, H * 0.3f) };

        Assert.IsTrue(pad.Evaluate(somewhere, W, H).AnyTap, "a new touch is a tap");
        Assert.IsFalse(pad.Evaluate(somewhere, W, H).AnyTap, "holding it is not another tap");
        pad.Evaluate(new List<Vector2>(), W, H);
        Assert.IsTrue(pad.Evaluate(somewhere, W, H).AnyTap, "touching again is");
    }

    [Test]
    public void PadStaysUsableOnAPortraitPhoneAndATablet()
    {
        var pad = Mobile();
        foreach (var (w, h) in new[] { (1080f, 1920f), (2048f, 1536f), (960f, 540f) })
        {
            foreach (var b in TouchControls.All)
            {
                var r = pad.RectFor(b, w, h);
                Assert.IsTrue(r.xMin >= 0 && r.yMin >= 0 && r.xMax <= w && r.yMax <= h,
                              $"{b} leaves the screen at {w}x{h}");
            }
            Assert.Less(pad.ReservedBottomFor(w, h), 0.45f,
                        $"the pad should not eat the level at {w}x{h}");
        }
    }
}
