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

        [Header("Shift — осмотреться")]
        [Tooltip("Насколько камера уезжает к прицелу при зажатом Shift (1 — центр экрана встаёт туда, где был курсор)")]
        [SerializeField] private float _peekAmount = 1f;
        [SerializeField] private float _maxPeekDistance = 8f;
        [SerializeField] private float _peekBlendSpeed = 6f;

        private Camera _camera;
        private float _peekWeight;
        private Vector3 _basePosition;
        private float _shakeStrength;
        private float _shakeUntil;
        private bool _flying;
        private Transform _focus;
        private float _focusSmoothing;
        private int _zooms;

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

        // Катсцена: камера плавно переезжает к другой цели и держит её без сдвига к курсору
        public void Focus(Transform target, float smoothing)
        {
            _focus = target;
            _focusSmoothing = smoothing;
        }

        // Монтажная склейка: мгновенно встать над целью и держать её
        public void CutTo(Transform target)
        {
            Focus(target, _smoothing);
            _basePosition = new Vector3(target.position.x, target.position.y, _basePosition.z);
            transform.position = _basePosition;
        }

        public IEnumerator Zoom(float orthographicSize, float duration)
        {
            _zooms++;
            float from = _camera.orthographicSize;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _camera.orthographicSize = Mathf.Lerp(from, orthographicSize, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            _camera.orthographicSize = orthographicSize;
            _zooms--;
        }

        // Удар камерой (выстрел, попадание в катсцене): резкий наезд на долю размера кадра и плавный откат.
        // Во время Zoom не работает — оба меняли бы размер кадра
        public void Punch(float amount = 0.12f, float duration = 0.3f)
        {
            if (_zooms == 0 && isActiveAndEnabled) StartCoroutine(PunchRoutine(amount, duration));
        }

        private IEnumerator PunchRoutine(float amount, float duration)
        {
            _zooms++;
            float size = _camera.orthographicSize;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = t / duration;
                float push = k < 0.15f ? k / 0.15f : 1f - Mathf.SmoothStep(0f, 1f, (k - 0.15f) / 0.85f);
                _camera.orthographicSize = size * (1f - amount * push);
                yield return null;
            }
            _camera.orthographicSize = size;
            _zooms--;
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

            Vector2 targetPos = _focus != null ? _focus.position : _target.position;
            Vector2 offset = Vector2.zero;
            if (_focus == null && Mouse.current != null)
            {
                Vector2 mouseScreen = Mouse.current.position.ReadValue();
                Vector2 mouseWorld = _camera.ScreenToWorldPoint(mouseScreen);
                Vector2 normalOffset = Vector2.ClampMagnitude((mouseWorld - targetPos) * _lookAhead, _maxLookAhead);

                // Сдвиг для Shift считаем от центра экрана, а не от игрока: иначе камера, уезжая
                // к прицелу, сама сдвигала бы точку прицела и улетала бы до упора
                Vector2 screenCenter = new Vector2(_camera.pixelWidth, _camera.pixelHeight) * 0.5f;
                Vector2 cursorFromCenter = mouseWorld - (Vector2)_camera.ScreenToWorldPoint(screenCenter);
                Vector2 peekOffset = Vector2.ClampMagnitude(cursorFromCenter * _peekAmount, _maxPeekDistance);

                bool peeking = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
                _peekWeight = Mathf.MoveTowards(_peekWeight, peeking ? 1f : 0f, _peekBlendSpeed * Time.unscaledDeltaTime);
                offset = Vector2.Lerp(normalOffset, peekOffset, _peekWeight);
            }

            Vector3 desired = new Vector3(targetPos.x + offset.x, targetPos.y + offset.y, _basePosition.z);
            float smoothing = _focus != null ? _focusSmoothing : _smoothing;
            _basePosition = Vector3.Lerp(_basePosition, desired, 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));

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
