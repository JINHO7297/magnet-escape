using UnityEngine;

namespace MagnetEscape
{
    // Short, distinct synthesized game cues. No downloaded audio or runtime dependencies.
    public sealed class GameAudio : MonoBehaviour
    {
        public enum Cue { Death, Jump, Dash, Checkpoint, Clear, Land, MagnetAttract, MagnetRepel }
        private readonly AudioClip[] clips = new AudioClip[8];
        private AudioSource source;
        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            for (int kind = 0; kind < clips.Length; kind++) clips[kind] = Synthesize(kind);
        }

        public void Play(Cue cue)
        {
            source.PlayOneShot(clips[(int)cue]);
        }

        private static AudioClip Synthesize(int kind)
        {
            const int rate = 22050;
            bool landing = kind == (int)Cue.Land;
            bool magnetic = kind == (int)Cue.MagnetAttract || kind == (int)Cue.MagnetRepel;
            float duration = landing ? 0.08f : magnetic ? 0.12f : kind >= 3 ? 0.32f : 0.16f;
            float[] samples = new float[Mathf.RoundToInt(rate * duration)];
            float phase = 0f;
            uint noise = 71;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                float frequency = magnetic ? (kind == (int)Cue.MagnetAttract ? Mathf.Lerp(240f, 540f, t) : Mathf.Lerp(540f, 240f, t))
                    : landing ? Mathf.Lerp(140f, 60f, t) : kind == 0 ? Mathf.Lerp(260f, 70f, t)
                    : kind == 1 ? Mathf.Lerp(330f, 750f, t)
                    : kind == 2 ? Mathf.Lerp(900f, 180f, t)
                    : kind == 3 ? (t < 0.5f ? 660f : 990f)
                    : (t < 0.33f ? 523f : t < 0.66f ? 659f : 784f);
                phase += frequency * Mathf.PI * 2f / rate;
                float wave = Mathf.Sin(phase);
                if (kind == 2)
                {
                    noise = unchecked(noise * 1664525u + 1013904223u);
                    wave = wave * 0.3f + ((noise & 65535) / 32767.5f - 1f) * 0.7f;
                }
                float envelope = Mathf.Min(t * 40f, 1f) * Mathf.Pow(1f - t, 1.5f);
                samples[i] = wave * envelope * (landing || magnetic ? 0.12f : 0.22f);
            }
            AudioClip clip = AudioClip.Create("Magnet cue " + kind, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in clips) if (clip != null) Destroy(clip);
        }
    }
}
