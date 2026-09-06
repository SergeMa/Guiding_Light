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

        // The world's ambient light already rises level by level; the music rises with
        // it, so the last stretch is the loudest the game ever gets.
        const float QuietVolume = 0.20f;
        const float FullVolume = 0.44f;

        AudioClip[] tracks;
        AudioSource front, back;
        int playing = -1;
        float blend = 1f;          // 1 = fully on the front source
        float volume = QuietVolume;

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

        /// <summary>How loud the music sits at a given point in the game.</summary>
        public static float VolumeFor(int levelIndex, int levelCount)
        {
            if (levelCount <= 1) return FullVolume;
            float through = Mathf.Clamp01(levelIndex / (float)(levelCount - 1));
            return Mathf.Lerp(QuietVolume, FullVolume, through);
        }

        public void PlayFor(int levelIndex, int levelCount)
        {
            // set before the early return: the volume climbs every level, not only
            // on the three levels where the track happens to change
            volume = VolumeFor(levelIndex, levelCount);

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
            front.volume = volume * blend;
            back.volume = volume * (1f - blend);
            if (blend >= 1f && back.isPlaying) back.Stop();
        }
    }
}
