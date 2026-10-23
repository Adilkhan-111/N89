using UnityEngine;

namespace N89.Enemies
{
    // Учебный дрон: летает туда-обратно и смотрит по ходу движения. Не стреляет.
    public class TrainingBot : MonoBehaviour
    {
        [Tooltip("Смещение второй точки патруля относительно стартовой позиции")]
        [SerializeField] private Vector2 _patrolOffset = new Vector2(4f, 0f);
        [SerializeField] private float _speed = 2.5f;
        [SerializeField] private Transform _body;

        private Vector2 _start;
        private float _phase;

        private void Awake()
        {
            _start = transform.position;
            _phase = Random.value;
        }

        private void Update()
        {
            float length = _patrolOffset.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            float t = Mathf.PingPong(Time.time * _speed / length + _phase, 1f);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector2 previous = transform.position;
            Vector2 next = _start + _patrolOffset * eased;
            transform.position = next;

            Vector2 velocity = next - previous;
            if (_body != null && velocity.sqrMagnitude > 0.00001f)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                _body.rotation = Quaternion.Slerp(_body.rotation, Quaternion.Euler(0f, 0f, angle), 10f * Time.deltaTime);
            }
        }
    }
}
