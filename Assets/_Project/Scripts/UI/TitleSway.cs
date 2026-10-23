using UnityEngine;

namespace N89.UI
{
    // Покачивание заголовка как в Hotline Miami: поворот, "дыхание" масштаба, плавание по вертикали
    // и редкие глитч-рывки. Несколько слоёв с разной фазой дают эффект запаздывающей тени.
    [RequireComponent(typeof(RectTransform))]
    public class TitleSway : MonoBehaviour
    {
        [SerializeField] private float _rotationAmplitude = 6f;
        [SerializeField] private float _rotationSpeed = 1.1f;
        [SerializeField] private float _scaleAmplitude = 0.06f;
        [SerializeField] private float _scaleSpeed = 1.7f;
        [SerializeField] private float _bobAmplitude = 8f;
        [SerializeField] private float _bobSpeed = 0.9f;
        [Tooltip("Сдвиг фазы в секундах: отрицательное значение — слой отстаёт от основного")]
        [SerializeField] private float _phaseOffset;

        [Header("Glitch")]
        [SerializeField] private bool _glitch = true;
        [SerializeField] private float _glitchChancePerSecond = 0.6f;
        [SerializeField] private float _glitchDuration = 0.08f;
        [SerializeField] private float _glitchStrength = 14f;

        private RectTransform _rect;
        private Vector2 _basePosition;
        private float _glitchUntil;
        private Vector2 _glitchOffset;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _basePosition = _rect.anchoredPosition;
        }

        private void Update()
        {
            float t = Time.unscaledTime + _phaseOffset;

            float angle = Mathf.Sin(t * _rotationSpeed) * _rotationAmplitude;
            float scale = 1f + Mathf.Sin(t * _scaleSpeed) * _scaleAmplitude;
            float bob = Mathf.Sin(t * _bobSpeed + 1.3f) * _bobAmplitude;

            if (_glitch)
            {
                UpdateGlitch();
            }

            _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            _rect.localScale = new Vector3(scale, scale, 1f);
            _rect.anchoredPosition = _basePosition + new Vector2(0f, bob) + _glitchOffset;
        }

        private void UpdateGlitch()
        {
            float now = Time.unscaledTime;
            if (now < _glitchUntil)
            {
                return;
            }

            _glitchOffset = Vector2.zero;
            if (Random.value < _glitchChancePerSecond * Time.unscaledDeltaTime)
            {
                _glitchUntil = now + _glitchDuration;
                _glitchOffset = new Vector2(Random.Range(-1f, 1f) * _glitchStrength, Random.Range(-0.3f, 0.3f) * _glitchStrength);
            }
        }
    }
}
