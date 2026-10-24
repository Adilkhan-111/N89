using UnityEngine;

namespace N89.Player
{
    // Ноги как в Hotline Miami: отдельный спрайт под корпусом смотрит по направлению ходьбы
    // (корпус при этом целится за мышью), кадры шагов сменяются по пройденному пути.
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

        private void LateUpdate()
        {
            // В катсценах физика выключена — позу рисует спрайт корпуса, ноги прячем
            _legs.enabled = _rb.simulated;

            Vector2 velocity = _rb.linearVelocity;
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
