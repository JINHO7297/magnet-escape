using UnityEngine;

namespace MagnetEscape
{
    public static class GameOptions
    {
        private static bool displayApplied;
        public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat("MagnetEscape.Volume", 0.65f));

        public static void ApplyDisplayPreferences()
        {
            if (Application.isEditor || displayApplied) return;
            displayApplied = true;
            if (!PlayerPrefs.HasKey("MagnetEscape.Fullscreen")) return;
            bool fullscreen = PlayerPrefs.GetInt("MagnetEscape.Fullscreen") != 0;
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        public static void ToggleFullscreen()
        {
            if (Application.isEditor) return;
            bool fullscreen = !Screen.fullScreen;
            PlayerPrefs.SetInt("MagnetEscape.Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

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
