using N89.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace N89.UI
{
    // Полоса здоровья босса вверху экрана: имя, красная полоса и белая «тень» урона, которая догоняет её
    // с задержкой — видно, сколько сняло последнее попадание.
    public class BossHud : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _lag;
        [SerializeField] private float _lagDelay = 0.35f;
        [SerializeField] private float _lagSpeed = 0.5f;

        private Health _health;
        private bool _visible;
        private float _lagValue = 1f;
        private float _lagHoldUntil;
        private float _lastFraction = 1f;

        private void Awake()
        {
            _group.alpha = 0f;
        }

        public void Show(Health health, string displayName)
        {
            _health = health;
            _name.text = displayName;
            _visible = true;
            _lastFraction = _lagValue = health.Fraction;
        }

        public void Hide()
        {
            _visible = false;
        }

        private void Update()
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, _visible ? 1f : 0f, 3f * Time.unscaledDeltaTime);
            if (_health == null) return;

            float fraction = _health.Fraction;
            if (fraction < _lastFraction) _lagHoldUntil = Time.unscaledTime + _lagDelay;
            _lastFraction = fraction;
            if (Time.unscaledTime >= _lagHoldUntil) _lagValue = Mathf.MoveTowards(_lagValue, fraction, _lagSpeed * Time.unscaledDeltaTime);

            _fill.fillAmount = fraction;
            _lag.fillAmount = Mathf.Max(_lagValue, fraction);
        }
    }
}
