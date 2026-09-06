using UnityEngine;

namespace FirstLight
{
    /// <summary>
    /// Recorded sound effects laid over the procedural tones, the same way
    /// <see cref="Skin"/> lays art over the drawn sprites. Anything left empty falls
    /// back to the generated tone, so the game still has sound with nothing wired.
    ///
    /// Every cue is pitched a little differently each time it fires. These are the
    /// most repeated sounds in the game - a mirror turns hundreds of times in a
    /// playthrough - and identical playback is what makes a cue start to grate.
    /// </summary>
    [System.Serializable]
    public class SoundBank
    {
        [Tooltip("Turning a mirror. Several takes get picked between.")]
        public AudioClip[] mirrorTurn;

        [Tooltip("Something the beam has just lit up.")]
        public AudioClip[] deviceOn;

        [Tooltip("Something the beam has just left, so it drops or shuts.")]
        public AudioClip[] deviceOff;

        [Tooltip("An eye going out.")]
        public AudioClip[] enemyDown;

        [Tooltip("Lowest and highest pitch a cue is played back at.")]
        public float pitchLow = 0.92f;
        public float pitchHigh = 1.09f;

        public float RandomPitch() => Random.Range(Mathf.Min(pitchLow, pitchHigh),
                                                   Mathf.Max(pitchLow, pitchHigh));

        /// <summary>
        /// A clip from the bank, avoiding the one played last so two identical takes
        /// never land back to back. Falls back when the bank is empty.
        /// </summary>
        public static AudioClip Pick(AudioClip[] bank, ref int last, AudioClip fallback)
        {
            if (bank == null || bank.Length == 0) return fallback;
            if (bank.Length == 1) return bank[0] != null ? bank[0] : fallback;

            int i = Random.Range(0, bank.Length);
            if (i == last) i = (i + 1) % bank.Length;
            last = i;
            return bank[i] != null ? bank[i] : fallback;
        }

        /// <summary>True when this bank has anything to play at all.</summary>
        public static bool Has(AudioClip[] bank)
        {
            if (bank == null) return false;
            foreach (var c in bank)
                if (c != null) return true;
            return false;
        }
    }
}
