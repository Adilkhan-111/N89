using UnityEngine;

namespace N89.Core
{
    // Снаряжение, выбранное в арсенале перед миссией. Хранится в PlayerPrefs (переживает перезапуск игры):
    // id — имена ассетов WeaponData / ImplantData.
    public static class Loadout
    {
        private const string MissionKey = "loadout.mission";
        private const string WeaponKey = "loadout.weapon";
        private const string ImplantKey = "loadout.implant";

        public static int Mission => PlayerPrefs.GetInt(MissionKey, 0);
        public static string WeaponId => PlayerPrefs.GetString(WeaponKey, string.Empty);
        public static string ImplantId => PlayerPrefs.GetString(ImplantKey, string.Empty);

        public static bool IsChosenFor(int mission) => Mission == mission && !string.IsNullOrEmpty(WeaponId);

        public static void Save(int mission, string weaponId, string implantId)
        {
            PlayerPrefs.SetInt(MissionKey, mission);
            PlayerPrefs.SetString(WeaponKey, weaponId);
            PlayerPrefs.SetString(ImplantKey, implantId);
            PlayerPrefs.Save();
        }
    }
}
