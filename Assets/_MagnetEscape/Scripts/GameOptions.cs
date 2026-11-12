using UnityEngine;

namespace MagnetEscape
{
    public static class GameOptions
    {
        public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat("MagnetEscape.Volume", 0.65f));

        public static void SetVolume(float value)
        {
            float volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("MagnetEscape.Volume", volume);
            AudioListener.volume = volume;
        }

        public static void ToggleMute()
        {
            if (Volume > 0f)
            {
                PlayerPrefs.SetFloat("MagnetEscape.PreMuteVolume", Volume);
                SetVolume(0f);
            }
            else SetVolume(PlayerPrefs.GetFloat("MagnetEscape.PreMuteVolume", 0.65f));
            PlayerPrefs.Save();
        }
    }
}
