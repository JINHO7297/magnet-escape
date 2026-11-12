using UnityEngine;

namespace MagnetEscape
{
    // Short, distinct synthesized game cues. No downloaded audio or runtime dependencies.
    public sealed class GameAudio : MonoBehaviour
    {
        private readonly AudioClip[] clips = new AudioClip[5];
        private AudioSource source;
        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.playOnAwake = false;
            for (int kind = 0; kind < clips.Length; kind++) clips[kind] = Synthesize(kind);
        }

        public void Play(float cue)
        {
            int kind = cue < 300f ? 0 : cue < 600f ? 1 : cue < 800f ? 2 : cue < 1000f ? 3 : 4;
            source.PlayOneShot(clips[kind]);
        }

        private static AudioClip Synthesize(int kind)
        {
            const int rate = 22050;
            float duration = kind >= 3 ? 0.32f : 0.16f;
            float[] samples = new float[Mathf.RoundToInt(rate * duration)];
            float phase = 0f;
            uint noise = 71;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                float frequency = kind == 0 ? Mathf.Lerp(260f, 70f, t)
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
                samples[i] = wave * envelope * 0.22f;
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
