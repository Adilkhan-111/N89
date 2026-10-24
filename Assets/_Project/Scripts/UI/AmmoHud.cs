using N89.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace N89.UI
{
    // Счётчик патронов в духе Hotline Miami: «16/16rnds» на чёрной плашке в углу, плашка покачивается,
    // на выстреле подпрыгивает. Пусто или идёт перезарядка — мигает красным «RELOAD».
    // Прячется, если у оружия нет магазина.
    public class AmmoHud : MonoBehaviour
    {
        [SerializeField] private Weapon _weapon;
        [SerializeField] private ActiveReload _reload;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _box;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private TextMeshProUGUI _shadow;
        [Tooltip("Полоска остатка патронов (Image Filled): уменьшается с каждым выстрелом")]
        [SerializeField] private Image _bar;
        [SerializeField] private Color _barColor = new Color(1f, 0.25f, 0.8f);
        [SerializeField] private Color _color = new Color(1f, 0.72f, 0.9f);
        [SerializeField] private Color _emptyColor = new Color(1f, 0.25f, 0.3f);
        [SerializeField] private float _swayAngle = 3f;

        private float _punch;
        private int _lastAmmo = -1;
        private bool _lastReloading;

        // Туториал прячет счётчик, пока не дошёл до стрельбы
        public bool Visible { get; set; } = true;

        private void Awake()
        {
            if (_weapon != null) _weapon.Fired += OnFired;
        }

        private void OnDestroy()
        {
            if (_weapon != null) _weapon.Fired -= OnFired;
        }

        private void OnFired()
        {
            _punch = 1f;
        }

        private void Update()
        {
            bool show = Visible && _weapon != null && _weapon.MagazineSize > 0;
            float dt = Time.unscaledDeltaTime;
            _group.alpha = Mathf.MoveTowards(_group.alpha, show ? 1f : 0f, 4f * dt);
            if (!show)
            {
                return;
            }

            bool reloading = _reload != null && _reload.IsReloading;
            if (_weapon.Ammo != _lastAmmo || reloading != _lastReloading)
            {
                _lastAmmo = _weapon.Ammo;
                _lastReloading = reloading;
                string value = reloading ? "RELOAD" : $"{_weapon.Ammo}/{_weapon.MagazineSize}rnds";
                _text.text = value;
                _shadow.text = value;
            }

            bool alarm = reloading || _weapon.Ammo <= 0;
            bool blink = alarm && Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f;
            _text.color = alarm ? (blink ? Color.white : _emptyColor) : _color;

            if (_bar != null)
            {
                // Быстро догоняет остаток; меньше трети магазина — краснеет
                float share = (float)_weapon.Ammo / _weapon.MagazineSize;
                _bar.fillAmount = Mathf.MoveTowards(_bar.fillAmount, share, 3f * dt);
                _bar.color = share <= 0.34f ? _emptyColor : _barColor;
            }

            _punch = Mathf.MoveTowards(_punch, 0f, 6f * dt);
            float scale = 1f + _punch * 0.12f;
            _box.localScale = new Vector3(scale, scale, 1f);
            _box.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.6f) * _swayAngle);
        }
    }
}
