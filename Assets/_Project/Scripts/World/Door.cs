using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace N89.World
{
    // Раздвижная дверь: панель уезжает в стену, коллайдер отключается, индикатор меняет цвет.
    public class Door : MonoBehaviour
    {
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Transform _panel;
        [SerializeField] private Vector2 _openOffset = new Vector2(0f, 3f);
        [SerializeField] private float _speed = 5f;
        [SerializeField] private SpriteRenderer _statusLamp;
        [SerializeField] private Light2D _statusLight;
        [SerializeField] private Color _lockedColor = new Color(1f, 0.15f, 0.25f);
        [SerializeField] private Color _openColor = new Color(0.2f, 1f, 0.5f);
        [SerializeField] private bool _startOpen;

        private Vector3 _closedPosition;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            _closedPosition = _panel.localPosition;
            SetOpen(_startOpen, true);
        }

        public void Open() => SetOpen(true, false);

        public void Close() => SetOpen(false, false);

        private void SetOpen(bool open, bool instant)
        {
            IsOpen = open;
            _collider.enabled = !open;

            Color color = open ? _openColor : _lockedColor;
            if (_statusLamp != null) _statusLamp.color = color;
            if (_statusLight != null) _statusLight.color = color;

            if (instant)
            {
                _panel.localPosition = TargetPosition();
            }
        }

        private Vector3 TargetPosition()
        {
            return IsOpen ? _closedPosition + (Vector3)_openOffset : _closedPosition;
        }

        private void Update()
        {
            _panel.localPosition = Vector3.MoveTowards(_panel.localPosition, TargetPosition(), _speed * Time.deltaTime);
        }
    }
}
