using UnityEngine;

namespace N89.World
{
    // Рыбка в бассейне: плывёт к случайной точке внутри _area, у цели выбирает новую.
    // Спрайт смотрит вправо (+x); хвост виляет — корпус слегка покачивается на ходу.
    public class Fish : MonoBehaviour
    {
        [Tooltip("Где плавает (мировые координаты)")]
        [SerializeField] private Rect _area;
        [SerializeField] private float _speed = 0.7f;
        [SerializeField] private float _turnSpeed = 160f;

        private Vector2 _goal;
        private float _phase;
        private float _angle;

        private void Start()
        {
            _phase = Random.value * 10f;
            _angle = transform.eulerAngles.z;
            PickGoal();
        }

        private void Update()
        {
            Vector2 position = transform.position;
            Vector2 toGoal = _goal - position;
            if (toGoal.sqrMagnitude < 0.04f)
            {
                PickGoal();
                return;
            }

            float wanted = Mathf.Atan2(toGoal.y, toGoal.x) * Mathf.Rad2Deg;
            _angle = Mathf.MoveTowardsAngle(_angle, wanted, _turnSpeed * Time.deltaTime);
            float wiggle = Mathf.Sin((Time.time + _phase) * 9f) * 8f;
            transform.rotation = Quaternion.Euler(0f, 0f, _angle + wiggle);

            // Плывёт туда, куда смотрит, — поворачивает плавно, а не скользит боком
            Vector2 forward = Quaternion.Euler(0f, 0f, _angle) * Vector2.right;
            position += forward * (_speed * Time.deltaTime);
            position.x = Mathf.Clamp(position.x, _area.xMin, _area.xMax);
            position.y = Mathf.Clamp(position.y, _area.yMin, _area.yMax);
            transform.position = position;
        }

        private void PickGoal()
        {
            _goal = new Vector2(Random.Range(_area.xMin, _area.xMax), Random.Range(_area.yMin, _area.yMax));
        }
    }
}
