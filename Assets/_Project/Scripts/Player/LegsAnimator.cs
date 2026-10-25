using UnityEngine;

namespace N89.Player
{
    // Ноги как в Hotline Miami: отдельный спрайт под корпусом смотрит по направлению ходьбы
    // (корпус при этом целится за мышью), кадры шагов сменяются по пройденному пути.
    // Скорость — из физики или по смещению за кадр: так ноги шагают и у охраны (кинематика),
    // и когда персонажа ведёт катсцена (двигает transform).
    public class LegsAnimator : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private SpriteRenderer _legs;
        [Tooltip("Кадр 0 — ноги вместе (стоит), дальше цикл шага")]
        [SerializeField] private Sprite[] _frames;
        [Tooltip("Юнитов пути на один кадр: больше — шаг медленнее")]
        [SerializeField] private float _distancePerFrame = 0.5f;
        [SerializeField] private float _turnSpeed = 900f;
        [SerializeField] private float _minSpeed = 0.3f;

        private float _distance;
        private Vector2 _lastPosition;
        private Vector2 _moved;

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _moved = Vector2.zero;
        }

        private void LateUpdate()
        {
            // В катсценах интерлюдий физика выключена — позу рисует спрайт корпуса, ноги прячем
            _legs.enabled = _rb == null || _rb.simulated;

            // Смещение за кадр (сглаженное: физика двигает тело не каждый кадр); телепорт не считается шагом
            Vector2 position = transform.position;
            Vector2 step = position - _lastPosition;
            _lastPosition = position;
            if (Time.deltaTime > 0f)
            {
                Vector2 now = step.sqrMagnitude < 1f ? step / Time.deltaTime : Vector2.zero;
                _moved = Vector2.Lerp(_moved, now, 0.35f);
            }

            Vector2 velocity = _rb != null && _rb.simulated ? _rb.linearVelocity : Vector2.zero;
            if (_moved.sqrMagnitude > velocity.sqrMagnitude) velocity = _moved;
            float speed = velocity.magnitude;
            if (speed < _minSpeed)
            {
                _legs.sprite = _frames[0];
                _distance = 0f;
                return;
            }

            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            Quaternion target = Quaternion.Euler(0f, 0f, angle);
            _legs.transform.rotation = Quaternion.RotateTowards(_legs.transform.rotation, target, _turnSpeed * Time.deltaTime);

            _distance += speed * Time.deltaTime;
            int frame = 1 + Mathf.FloorToInt(_distance / _distancePerFrame) % (_frames.Length - 1);
            _legs.sprite = _frames[frame];
        }
    }
}
