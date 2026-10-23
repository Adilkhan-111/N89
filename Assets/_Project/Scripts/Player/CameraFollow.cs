using System.Collections;
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
        private bool _flying;

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

        // Мгновенно встать над целью (после телепорта игрока)
        public void SnapToTarget()
        {
            _basePosition = new Vector3(_target.position.x, _target.position.y, _basePosition.z);
            transform.position = _basePosition;
        }

        // Камера "улетает вверх": перестаёт следить за игроком, отдаляется и плывёт к точке обзора
        public IEnumerator FlyAway(Vector2 center, float orthographicSize, float duration)
        {
            _flying = true;
            Vector3 from = transform.position;
            Vector3 to = new Vector3(center.x, center.y, from.z);
            float fromSize = _camera.orthographicSize;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                transform.position = Vector3.Lerp(from, to, k);
                // Сначала медленно, потом быстрее — как будто камера набирает высоту
                _camera.orthographicSize = Mathf.Lerp(fromSize, orthographicSize, k * k);
                yield return null;
            }

            transform.position = to;
            _camera.orthographicSize = orthographicSize;
        }

        private void LateUpdate()
        {
            if (_target == null || _flying)
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
