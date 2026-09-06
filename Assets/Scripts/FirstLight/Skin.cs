using UnityEngine;

namespace FirstLight
{
    /// <summary>
    /// Hand-drawn art laid over the procedural sprites. Every field is optional:
    /// whatever is left empty falls back to <see cref="Art"/>, so the game still
    /// runs with nothing wired at all - which is exactly what the tests do.
    /// </summary>
    [System.Serializable]
    public class Skin
    {
        [Tooltip("Frames of the entity, played in order. Empty falls back to the star.")]
        public Sprite[] entityFrames;

        [Tooltip("Frames per second for the entity animation.")]
        public float entityFps = 7f;

        [Tooltip("The mirror. Empty falls back to the drawn plate.")]
        public Sprite mirror;

        [Tooltip("Which way the mirror's reflective face points in the sprite's own " +
                 "artwork, in degrees: 0 right, 90 up, 180 left. The drawn plate faces up; " +
                 "mirror.png faces left.")]
        public float mirrorFaceDegrees = 180f;

        [Tooltip("The creature in the dark. Empty falls back to the red eye.")]
        public Sprite eye;

        public bool HasEntity => entityFrames != null && entityFrames.Length > 0;

        public Sprite EntityFrame(float time)
        {
            if (!HasEntity) return null;
            int i = Mathf.FloorToInt(time * Mathf.Max(entityFps, 0.01f)) % entityFrames.Length;
            return entityFrames[i < 0 ? i + entityFrames.Length : i];
        }

        /// <summary>
        /// Local scale that makes a sprite span roughly the given number of grid cells,
        /// whatever pixels-per-unit it was imported at.
        /// </summary>
        public static float ScaleToCells(Sprite sprite, float cells)
        {
            if (sprite == null) return cells;
            var size = sprite.bounds.size;
            float longest = Mathf.Max(size.x, size.y);
            return longest > 0.0001f ? cells / longest : cells;
        }
    }
}
