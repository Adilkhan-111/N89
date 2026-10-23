using UnityEngine;
using UnityEngine.InputSystem;

namespace N89.Player
{
    // Камера как в Hotline Miami: следует за игроком и смещается в сторону курсора.
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothing = 8f;
        [SerializeField] private float _lookAhead = 0.3f;
        [SerializeField] private float _maxLookAhead = 4f;

        private Camera _camera;
        private Vector3 _basePosition;
        private float _shakeStrength;
        private float _shakeUntil;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _basePosition = transform.position;
        }

        public void Shake(float strength, float duration)
        {
            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeUntil = Mathf.Max(_shakeUntil, Time.unscaledTime + duration);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            Vector2 targetPos = _target.position;
            Vector2 offset = Vector2.zero;
            if (Mouse.current != null)
            {
                Vector2 mouseWorld = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                offset = Vector2.ClampMagnitude((mouseWorld - targetPos) * _lookAhead, _maxLookAhead);
            }

            Vector3 desired = new Vector3(targetPos.x + offset.x, targetPos.y + offset.y, _basePosition.z);
            _basePosition = Vector3.Lerp(_basePosition, desired, 1f - Mathf.Exp(-_smoothing * Time.unscaledDeltaTime));

            Vector3 shake = Vector3.zero;
            if (Time.unscaledTime < _shakeUntil)
            {
                shake = (Vector3)(Random.insideUnitCircle * _shakeStrength);
            }
            else
            {
                _shakeStrength = 0f;
            }

            transform.position = _basePosition + shake;
        }
    }
}
