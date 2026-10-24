using N89.Combat;
using N89.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace N89.Enemies
{
    // Камера наблюдения на стене: водит взглядом влево-вправо, конус обзора виден на полу (зелёный).
    // Заметила игрока — конус краснеет, камера следит за ним, и все враги на уровне идут к нему.
    // Взлом «Отключить камеру» гасит её насовсем.
    public class SecurityCamera : MonoBehaviour
    {
        private const int Rays = 28;

        [SerializeField] private Transform _head;
        [SerializeField] private SpriteRenderer _led;
        [SerializeField] private Light2D _light;
        [SerializeField] private MeshFilter _coneFilter;
        [SerializeField] private MeshRenderer _coneRenderer;

        [Header("Обзор")]
        [Tooltip("Куда смотрит в середине поворота, градусы")]
        [SerializeField] private float _baseAngle;
        [Tooltip("Насколько поворачивает в каждую сторону от базового угла")]
        [SerializeField] private float _sweepAngle = 40f;
        [Tooltip("Секунд на полный проход туда-обратно")]
        [SerializeField] private float _sweepPeriod = 6f;
        [SerializeField] private float _fov = 60f;
        [SerializeField] private float _range = 7f;
        [Tooltip("Сколько игрок должен пробыть в конусе, чтобы поднялась тревога")]
        [SerializeField] private float _spotTime = 0.2f;
        [Tooltip("Сколько камера остаётся красной после того, как потеряла игрока")]
        [SerializeField] private float _calmDownTime = 2f;
        [SerializeField] private float _alarmInterval = 0.5f;
        [SerializeField] private LayerMask _sightBlockers;

        [Header("Цвета")]
        [SerializeField] private Color _calmColor = new Color(0.25f, 1f, 0.35f, 0.14f);
        [SerializeField] private Color _alarmColor = new Color(1f, 0.15f, 0.2f, 0.24f);

        private Transform _target;
        private Health _targetHealth;
        private Mesh _mesh;
        private readonly Vector3[] _vertices = new Vector3[Rays + 2];
        private readonly Color[] _colors = new Color[Rays + 2];
        private float _angle;
        private float _sweepTime;
        private float _seenFor;
        private float _alarmUntil = -1f;
        private float _nextAlarm;

        public bool IsDisabled { get; private set; }
        public bool IsAlarmed => Time.time < _alarmUntil;

        private void Start()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                _target = player.transform;
                _targetHealth = player.Health;
            }

            _mesh = new Mesh { name = "CameraCone" };
            _mesh.MarkDynamic();
            var triangles = new int[Rays * 3];
            for (int i = 0; i < Rays; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            _mesh.vertices = _vertices;
            _mesh.triangles = triangles;
            _coneFilter.sharedMesh = _mesh;
            // Конус рисуется в мировых координатах — сам объект не вращаем
            _coneFilter.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _angle = _baseAngle;
        }

        private void Update()
        {
            if (IsDisabled)
            {
                return;
            }

            bool sees = SeesTarget();
            _seenFor = sees ? _seenFor + Time.deltaTime : 0f;
            if (_seenFor >= _spotTime)
            {
                _alarmUntil = Time.time + _calmDownTime;
                if (Time.time >= _nextAlarm)
                {
                    _nextAlarm = Time.time + _alarmInterval;
                    RaiseAlarm();
                }
            }

            if (sees)
            {
                // Держит игрока в центре кадра, но не выходит за пределы своего поворота
                Vector2 to = _target.position - transform.position;
                float wanted = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                float offset = Mathf.Clamp(Mathf.DeltaAngle(_baseAngle, wanted), -_sweepAngle, _sweepAngle);
                _angle = Mathf.MoveTowardsAngle(_angle, _baseAngle + offset, 180f * Time.deltaTime);
                _sweepTime = Mathf.Asin(Mathf.Clamp(offset / Mathf.Max(_sweepAngle, 0.01f), -1f, 1f)) / (Mathf.PI * 2f) * _sweepPeriod;
            }
            else if (!IsAlarmed)
            {
                _sweepTime += Time.deltaTime;
                float target = _baseAngle + Mathf.Sin(_sweepTime / _sweepPeriod * Mathf.PI * 2f) * _sweepAngle;
                _angle = Mathf.MoveTowardsAngle(_angle, target, 90f * Time.deltaTime);
            }

            _head.rotation = Quaternion.Euler(0f, 0f, _angle);
            Color color = IsAlarmed ? _alarmColor : _calmColor;
            UpdateCone(color);

            Color solid = new Color(color.r, color.g, color.b, 1f);
            if (_led != null)
            {
                // Тревога — LED часто мигает
                bool blink = IsAlarmed && Mathf.Repeat(Time.time * 6f, 1f) > 0.5f;
                _led.color = blink ? Color.white : solid;
            }

            if (_light != null)
            {
                _light.color = solid;
            }
        }

        // Взлом: камера гаснет насовсем
        public void Disable()
        {
            IsDisabled = true;
            _alarmUntil = -1f;
            _coneRenderer.enabled = false;
            if (_led != null)
            {
                _led.color = new Color(0.15f, 0.15f, 0.18f);
            }

            if (_light != null)
            {
                _light.enabled = false;
            }
        }

        private bool SeesTarget()
        {
            if (_target == null || _targetHealth == null || _targetHealth.IsDead)
            {
                return false;
            }

            Vector2 to = _target.position - transform.position;
            if (to.magnitude > _range)
            {
                return false;
            }

            float angle = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            if (Mathf.Abs(Mathf.DeltaAngle(_angle, angle)) > _fov * 0.5f)
            {
                return false;
            }

            return !Physics2D.Linecast(transform.position, _target.position, _sightBlockers);
        }

        private static void RaiseAlarm()
        {
            foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>())
            {
                if (enemy.isActiveAndEnabled)
                {
                    enemy.Provoke();
                }
            }
        }

        // Веер лучей от камеры, обрезанный стенами: в центре плотнее, к краю прозрачнее
        private void UpdateCone(Color color)
        {
            Vector2 origin = transform.position;
            _vertices[0] = origin;
            _colors[0] = color;
            Color edge = new Color(color.r, color.g, color.b, color.a * 0.25f);
            for (int i = 0; i <= Rays; i++)
            {
                float a = (_angle - _fov * 0.5f + _fov * i / Rays) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                RaycastHit2D hit = Physics2D.Raycast(origin, dir, _range, _sightBlockers);
                float distance = hit ? hit.distance : _range;
                _vertices[i + 1] = origin + dir * distance;
                _colors[i + 1] = Color.Lerp(color, edge, distance / _range);
            }

            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();
        }
    }
}
