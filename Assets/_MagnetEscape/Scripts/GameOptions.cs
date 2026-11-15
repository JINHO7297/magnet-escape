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
            bool fullscreen = PlayerPrefs.GetInt("MagnetEscape.Fullscreen", 1) != 0;
            ApplyDisplayMode(fullscreen);
        }

        public static void ToggleFullscreen()
        {
            if (Application.isEditor) return;
            bool fullscreen = !Screen.fullScreen;
            PlayerPrefs.SetInt("MagnetEscape.Fullscreen", fullscreen ? 1 : 0);
            PlayerPrefs.Save();
            ApplyDisplayMode(fullscreen);
        }

        private static void ApplyDisplayMode(bool fullscreen)
        {
            Resolution desktop = Screen.currentResolution;
            if (fullscreen)
                Screen.SetResolution(desktop.width, desktop.height, FullScreenMode.FullScreenWindow);
            else
            {
                float scale = Mathf.Min(1f, desktop.width * 0.9f / 1600f, desktop.height * 0.85f / 900f);
                Screen.SetResolution(Mathf.RoundToInt(1600f * scale), Mathf.RoundToInt(900f * scale), FullScreenMode.Windowed);
            }
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
