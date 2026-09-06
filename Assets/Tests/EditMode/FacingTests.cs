using FirstLight;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Which way the entity is turned. This is a sign convention, which is the easiest
/// kind of thing to get backwards and the hardest to notice once it is - the art
/// still shows, just mirrored, and only looks slightly wrong.
/// </summary>
public class FacingTests
{
    // the angel's train trails right, so the artwork reads as facing left
    const bool FacesLeft = false;
    const bool FacesRight = true;

    [Test]
    public void ArtThatFacesLeftIsMirroredToWalkRight()
    {
        Assert.AreEqual(-1f, Skin.FacingSign(1, FacesLeft), "walking right must mirror it");
        Assert.AreEqual(1f, Skin.FacingSign(-1, FacesLeft), "walking left leaves it alone");
    }

    [Test]
    public void ArtThatFacesRightIsMirroredTheOtherWay()
    {
        Assert.AreEqual(1f, Skin.FacingSign(1, FacesRight));
        Assert.AreEqual(-1f, Skin.FacingSign(-1, FacesRight));
    }

    [Test]
    public void TheTurnIsAlwaysAHalfTurn()
    {
        foreach (bool art in new[] { FacesLeft, FacesRight })
        {
            float right = Skin.FacingSign(1, art);
            float left = Skin.FacingSign(-1, art);
            Assert.AreEqual(-right, left,
                "the two directions must be exact opposites - that is what makes it 180 degrees");
            Assert.AreEqual(1f, Mathf.Abs(right), "facing must never change the sprite's size");
        }
    }

    [Test]
    public void StandingStillDoesNotFlipAnything()
    {
        Assert.AreEqual(1f, Skin.FacingSign(0, FacesLeft),
                        "no horizontal movement means no turn");
        Assert.AreEqual(1f, Skin.FacingSign(0, FacesRight));
    }
}
