using UnityEngine;

namespace N89.Hacking
{
    public enum QuickhackEffect
    {
        Unlock,
        ShortCircuit,
    }

    // Описание одного взлома (как quickhack в Cyberpunk): название, цена в RAM, время загрузки.
    [CreateAssetMenu(menuName = "N89/Quickhack", fileName = "Quickhack")]
    public class QuickhackData : ScriptableObject
    {
        [SerializeField] private string _displayName = "ВЗЛОМ";
        [SerializeField] private int _ramCost = 1;
        [SerializeField] private float _uploadTime = 0.8f;
        [SerializeField] private QuickhackEffect _effect;
        [Tooltip("Урон для боевых взломов (Короткое замыкание)")]
        [SerializeField] private int _damage = 99;

        public string DisplayName => _displayName;
        public int RamCost => _ramCost;
        public float UploadTime => _uploadTime;
        public QuickhackEffect Effect => _effect;
        public int Damage => _damage;
    }
}
