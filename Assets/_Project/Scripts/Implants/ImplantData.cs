using UnityEngine;

namespace N89.Implants
{
    // Киберимплант: на каждую миссию даётся ровно один — тот, у которого совпадает номер миссии.
    [CreateAssetMenu(menuName = "N89/Implant", fileName = "Implant")]
    public class ImplantData : ScriptableObject
    {
        [SerializeField] private string _displayName = "ИМПЛАНТ";
        [SerializeField, TextArea(3, 10)] private string _description;
        [SerializeField] private Sprite _icon;
        [Tooltip("На какой миссии имплант доступен (1–3)")]
        [SerializeField] private int _mission = 1;

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int Mission => _mission;
    }
}
