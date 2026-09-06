using System.Collections.Generic;
using FirstLight;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Picking and pitching the recorded cues. A mirror turns hundreds of times in a
/// playthrough, so the thing that matters is that two identical playbacks never
/// land back to back.
/// </summary>
public class SoundBankTests
{
    static AudioClip Clip(string name) => AudioClip.Create(name, 64, 1, 8000, false);

    [Test]
    public void PitchMovesAndStaysInRange()
    {
        var bank = new SoundBank { pitchLow = 0.92f, pitchHigh = 1.09f };
        var seen = new HashSet<float>();
        for (int i = 0; i < 200; i++)
        {
            float p = bank.RandomPitch();
            Assert.That(p, Is.InRange(0.92f, 1.09f), "pitch left its range");
            seen.Add(p);
        }
        Assert.Greater(seen.Count, 50, "the pitch is meant to differ each time, not sit still");
    }

    [Test]
    public void PitchRangeSurvivesBeingWrittenBackwards()
    {
        var bank = new SoundBank { pitchLow = 1.2f, pitchHigh = 0.8f };
        for (int i = 0; i < 50; i++)
            Assert.That(bank.RandomPitch(), Is.InRange(0.8f, 1.2f),
                        "a range entered the wrong way round must still be a range");
    }

    [Test]
    public void TheSameTakeNeverPlaysTwiceRunning()
    {
        var takes = new[] { Clip("a"), Clip("b"), Clip("c") };
        int last = -1, previous = -1;
        for (int i = 0; i < 300; i++)
        {
            var picked = SoundBank.Pick(takes, ref last, null);
            Assert.IsNotNull(picked);
            Assert.AreNotEqual(previous, last, "the same take came round twice in a row");
            previous = last;
        }
    }

    [Test]
    public void EveryTakeGetsUsed()
    {
        var takes = new[] { Clip("a"), Clip("b"), Clip("c") };
        var used = new HashSet<AudioClip>();
        int last = -1;
        for (int i = 0; i < 300; i++) used.Add(SoundBank.Pick(takes, ref last, null));
        Assert.AreEqual(takes.Length, used.Count, "some takes are never heard");
    }

    [Test]
    public void AnEmptyBankFallsBackToTheGeneratedTone()
    {
        var fallback = Clip("tone");
        int last = -1;

        Assert.AreSame(fallback, SoundBank.Pick(null, ref last, fallback));
        Assert.AreSame(fallback, SoundBank.Pick(new AudioClip[0], ref last, fallback));
        Assert.AreSame(fallback, SoundBank.Pick(new AudioClip[] { null }, ref last, fallback),
                       "an empty slot must not silence the cue");

        Assert.IsFalse(SoundBank.Has(null));
        Assert.IsFalse(SoundBank.Has(new AudioClip[] { null, null }));
        Assert.IsTrue(SoundBank.Has(new[] { fallback }));
    }

    [Test]
    public void ASingleTakeIsAlwaysThatTake()
    {
        var only = Clip("only");
        int last = -1;
        for (int i = 0; i < 10; i++)
            Assert.AreSame(only, SoundBank.Pick(new[] { only }, ref last, null));
    }
}
