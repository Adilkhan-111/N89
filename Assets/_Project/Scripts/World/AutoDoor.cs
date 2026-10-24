using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace N89.World
{
    // Автоматическая раздвижная дверь в проёме между комнатами: две створки уезжают в стены, когда рядом
    // кто-то есть (герой или враг), и съезжаются обратно через полсекунды после того, как все ушли.
    // Закрытая дверь — на слое Door: закрывает обзор врагам и ловит пули, но не мешает поиску пути
    // (NavGrid её не видит) — перед идущим врагом она всё равно откроется.
    public class AutoDoor : MonoBehaviour
    {
        [SerializeField] private Transform _left;
        [SerializeField] private Transform _right;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Light2D _light;
        [SerializeField] private SpriteRenderer[] _statusLeds;
        [Tooltip("На сколько уезжает каждая створка (локально, вдоль двери)")]
        [SerializeField] private float _slide = 1f;
        [SerializeField] private float _detectRadius = 1.7f;
        [SerializeField] private float _speed = 4f;
        [SerializeField] private float _holdOpen = 0.5f;
        [SerializeField] private LayerMask _actors;

        private static readonly Color OpenColor = new Color(0.3f, 1f, 0.45f);
        private static readonly Color ClosedColor = new Color(1f, 0.25f, 0.2f);

        private Vector3 _leftClosed;
        private Vector3 _rightClosed;
        private float _open;
        private float _closeAt;

        public bool IsOpen => _open > 0.9f;

        private void Awake()
        {
            _leftClosed = _left.localPosition;
            _rightClosed = _right.localPosition;
        }

        private void Update()
        {
            if (Physics2D.OverlapCircle(transform.position, _detectRadius, _actors) != null)
            {
                _closeAt = Time.time + _holdOpen;
            }

            float target = Time.time < _closeAt ? 1f : 0f;
            _open = Mathf.MoveTowards(_open, target, _speed * Time.deltaTime);
            float eased = Mathf.SmoothStep(0f, 1f, _open);
            _left.localPosition = _leftClosed + Vector3.left * (_slide * eased);
            _right.localPosition = _rightClosed + Vector3.right * (_slide * eased);

            // Пока створки хоть немного сомкнуты — дверь твёрдая
            _collider.enabled = _open < 0.6f;

            Color status = _open > 0.5f ? OpenColor : ClosedColor;
            if (_light != null) _light.color = status;
            foreach (SpriteRenderer led in _statusLeds)
            {
                if (led != null) led.color = status;
            }
        }
    }
}
