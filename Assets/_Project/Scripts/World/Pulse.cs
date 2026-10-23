using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace N89.World
{
    // Пульсация неона: масштаб, прозрачность спрайта и яркость 2D-света.
    public class Pulse : MonoBehaviour
    {
        [SerializeField] private float _speed = 3f;
        [SerializeField] private float _scaleAmount = 0.08f;
        [SerializeField] private float _alphaMin = 0.5f;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Light2D _light;
        [SerializeField] private float _lightMin = 0.4f;
        [SerializeField] private float _lightMax = 1.5f;

        private Vector3 _baseScale;
        private float _phase;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _phase = Random.value * 10f;
        }

        private void Update()
        {
            float wave = 0.5f + 0.5f * Mathf.Sin((Time.time + _phase) * _speed);
            transform.localScale = _baseScale * (1f + wave * _scaleAmount);

            if (_renderer != null)
            {
                Color c = _renderer.color;
                c.a = Mathf.Lerp(_alphaMin, 1f, wave);
                _renderer.color = c;
            }

            if (_light != null)
            {
                _light.intensity = Mathf.Lerp(_lightMin, _lightMax, wave);
            }
        }
    }
}
