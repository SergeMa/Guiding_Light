using UnityEngine;

namespace FirstLight
{
    /// <summary>
    /// Background music, one track per stretch of the game. Two sources so a change
    /// of track crossfades rather than cutting, and the tracks are spread evenly over
    /// however many levels there happen to be, so adding levels never needs this
    /// touched. With no clips wired it simply does nothing.
    /// </summary>
    public class Music : MonoBehaviour
    {
        const float FadeSeconds = 1.6f;

        // One level throughout. It used to climb with the ambient light, but a puzzle
        // you sit on for a while should not get louder underneath you while you think.
        const float Volume = 0.44f;

        AudioClip[] tracks;
        AudioSource front, back;
        int playing = -1;
        float blend = 1f;          // 1 = fully on the front source

        public static Music Create(Transform parent, AudioClip[] clips)
        {
            var go = new GameObject("Music");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<Music>();
            m.tracks = clips;
            m.front = Source(go);
            m.back = Source(go);
            return m;
        }

        static AudioSource Source(GameObject go)
        {
            var a = go.AddComponent<AudioSource>();
            a.loop = true;
            a.playOnAwake = false;
            a.volume = 0f;
            return a;
        }

        /// <summary>Which track a level falls under, spread evenly across the game.</summary>
        public int TrackFor(int levelIndex, int levelCount)
        {
            if (tracks == null || tracks.Length == 0 || levelCount <= 0) return -1;
            int i = levelIndex * tracks.Length / levelCount;
            return Mathf.Clamp(i, 0, tracks.Length - 1);
        }

        /// <summary>
        /// How loud the music sits. Deliberately the same at every level - kept as a
        /// function so that stays something a test can hold onto.
        /// </summary>
        public static float VolumeFor(int levelIndex, int levelCount) => Volume;

        public void PlayFor(int levelIndex, int levelCount)
        {
            int want = TrackFor(levelIndex, levelCount);
            if (want < 0 || want == playing) return;
            var clip = tracks[want];
            if (clip == null) return;

            // swap the sources and fade across
            (front, back) = (back, front);
            front.clip = clip;
            front.time = 0f;
            front.Play();
            blend = 0f;
            playing = want;
        }

        void Update()
        {
            if (playing < 0) return;

            blend = Mathf.MoveTowards(blend, 1f, Time.unscaledDeltaTime / FadeSeconds);
            front.volume = Volume * blend;
            back.volume = Volume * (1f - blend);
            if (blend >= 1f && back.isPlaying) back.Stop();
        }
    }
}
