using UnityEngine;

namespace FirstLight
{
    /// <summary>
    /// Tiny procedural sound bank - no audio assets required. Each cue is a short
    /// enveloped tone so the frequent actions (turning a mirror, lighting a device,
    /// striking an eye) each have their own voice.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;

        AudioSource source;
        AudioClip turn, activate, strike, complete, deny, finale, deactivate;
        SoundBank bank = new();
        int lastTurn = -1, lastOn = -1, lastOff = -1, lastDown = -1;

        public static Sfx Create(Transform parent, SoundBank bank = null)
        {
            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            var sfx = go.AddComponent<Sfx>();
            sfx.bank = bank ?? new SoundBank();
            sfx.source = go.AddComponent<AudioSource>();
            sfx.source.playOnAwake = false;
            sfx.turn = Tone("turn", 0.10f, 420f, 620f, 0.20f);
            sfx.activate = Tone("activate", 0.22f, 660f, 990f, 0.24f);
            sfx.strike = Tone("strike", 0.28f, 180f, 70f, 0.30f, noise: 0.35f);
            sfx.complete = Tone("complete", 0.55f, 523f, 1046f, 0.26f);
            sfx.deny = Tone("deny", 0.16f, 150f, 90f, 0.22f);
            sfx.finale = Tone("finale", 1.60f, 262f, 1046f, 0.30f);
            sfx.deactivate = Tone("deactivate", 0.26f, 720f, 240f, 0.22f);
            return sfx;
        }

        static AudioClip Tone(string name, float seconds, float fromHz, float toHz,
                              float gain, float noise = 0f)
        {
            int samples = Mathf.RoundToInt(seconds * Rate);
            var data = new float[samples];
            float phase = 0f;
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float hz = Mathf.Lerp(fromHz, toHz, t * t);
                phase += 2f * Mathf.PI * hz / Rate;
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * Mathf.Exp(-2.2f * t);
                float v = Mathf.Sin(phase) * 0.7f + Mathf.Sin(phase * 2f) * 0.3f;
                if (noise > 0f) v += (float)(rng.NextDouble() * 2 - 1) * noise;
                data[i] = v * env * gain;
            }
            var clip = AudioClip.Create(name, samples, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Play(AudioClip clip, float pitch = 1f)
        {
            if (clip == null || source == null) return;
            source.pitch = pitch;
            source.PlayOneShot(clip);
        }

        // Every cue is pitched a little differently each time, recorded or generated.
        public void Turn() =>
            Play(SoundBank.Pick(bank.mirrorTurn, ref lastTurn, turn), bank.RandomPitch());

        public void Activate() =>
            Play(SoundBank.Pick(bank.deviceOn, ref lastOn, activate), bank.RandomPitch());

        /// <summary>The beam has left something, so it drops, shuts or goes out.</summary>
        public void Deactivate() =>
            Play(SoundBank.Pick(bank.deviceOff, ref lastOff, deactivate), bank.RandomPitch());

        public void Strike() =>
            Play(SoundBank.Pick(bank.enemyDown, ref lastDown, strike), bank.RandomPitch());

        public void Complete() => Play(complete);
        public void Deny() => Play(deny);
        public void Finale() => Play(finale);
    }
}
