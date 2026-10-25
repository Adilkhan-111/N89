using UnityEngine;

namespace N89.Implants
{
    // Киберимплант: на миссиях 1–2 даётся ровно один — тот, у которого совпадает номер миссии.
    // На миссии 3 E.N.A встроен (_builtIn), а на руки — выбор из имплантов с _extraMission = 3.
    [CreateAssetMenu(menuName = "N89/Implant", fileName = "Implant")]
    public class ImplantData : ScriptableObject
    {
        [SerializeField] private string _displayName = "ИМПЛАНТ";
        [SerializeField, TextArea(3, 10)] private string _description;
        [SerializeField] private Sprite _icon;
        [Tooltip("На какой миссии имплант доступен (1–3)")]
        [SerializeField] private int _mission = 1;
        [Tooltip("Ещё одна миссия, где имплант можно взять (0 — нет)")]
        [SerializeField] private int _extraMission;
        [Tooltip("Встроен: на своей миссии ставится сам, в арсенале его не выбирают")]
        [SerializeField] private bool _builtIn;

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int Mission => _mission;
        public bool BuiltIn => _builtIn;

        public bool IsAvailableOn(int mission) => mission == _mission || (_extraMission > 0 && mission == _extraMission);
        public string MissionsLabel => _extraMission > 0 ? $"{_mission} И {_extraMission}" : _mission.ToString();
    }
}
