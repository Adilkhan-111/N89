using UnityEngine;

namespace N89.Combat
{
    // Оружие из арсенала: описание для экрана выбора и параметры стрельбы для миссии.
    [CreateAssetMenu(menuName = "N89/Weapon", fileName = "Weapon")]
    public class WeaponData : ScriptableObject
    {
        [SerializeField] private string _displayName = "ОРУЖИЕ";
        [SerializeField, TextArea(3, 8)] private string _description;
        [Tooltip("Иконка сбоку для барабана выбора")]
        [SerializeField] private Sprite _icon;
        [Tooltip("Спрайт героя с этим оружием в руках (вид сверху) — ставится в миссии")]
        [SerializeField] private Sprite _heroSprite;

        [Header("Стрельба")]
        [SerializeField] private float _cooldown = 0.13f;
        [SerializeField] private float _spreadDegrees = 2.5f;
        [Tooltip("Сколько пуль за выстрел (дробовик — веер дробин)")]
        [SerializeField] private int _pellets = 1;
        [Tooltip("Дальность полёта пули в юнитах")]
        [SerializeField] private float _range = 30f;
        [Tooltip("Патронов в магазине (патроны бесконечные, перезарядка — мини-игра на время)")]
        [SerializeField] private int _magazineSize = 16;

        [Header("Полоски на экране выбора (1–5)")]
        [SerializeField, Range(1, 5)] private int _fireRateRating = 3;
        [SerializeField, Range(1, 5)] private int _powerRating = 3;
        [SerializeField, Range(1, 5)] private int _accuracyRating = 3;
        [SerializeField, Range(1, 5)] private int _rangeRating = 3;

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Sprite HeroSprite => _heroSprite;
        public float Cooldown => _cooldown;
        public float SpreadDegrees => _spreadDegrees;
        public int Pellets => _pellets;
        public float Range => _range;
        public int MagazineSize => _magazineSize;
        public int[] Ratings => new[] { _fireRateRating, _powerRating, _accuracyRating, _rangeRating };
    }
}
