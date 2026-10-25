using UnityEngine;

namespace N89.Core
{
    // Громкость музыки и звуковых эффектов (0..1), хранится в PlayerPrefs. Меняется в меню паузы.
    public static class SoundSettings
    {
        public static float Music
        {
            get => PlayerPrefs.GetFloat("volume.music", 1f);
            set { PlayerPrefs.SetFloat("volume.music", value); PlayerPrefs.Save(); }
        }

        public static float Sfx
        {
            get => PlayerPrefs.GetFloat("volume.sfx", 1f);
            set { PlayerPrefs.SetFloat("volume.sfx", value); PlayerPrefs.Save(); }
        }

        // Шаг по кругу: 100 → 75 → 50 → 25 → 0 → 100 %
        public static float Next(float volume) => volume <= 0.01f ? 1f : Mathf.Max(0f, Mathf.Round((volume - 0.25f) * 4f) / 4f);
    }
}
