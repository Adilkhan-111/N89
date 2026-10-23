using N89.Combat;
using N89.Player;
using UnityEngine;

namespace N89.Enemies
{
    // Враг корпорации Е (охранник или боевой дрон): патрулирует, замечает игрока
    // (дальность + угол обзора + линия видимости), после короткой реакции разворачивается и стреляет.
    // Слышит выстрелы рядом. Потеряв игрока из виду, смотрит туда, где видел его в последний раз.
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private Transform _body;
        [SerializeField] private Weapon _weapon;
        [SerializeField] private Health _health;

        [Header("Patrol")]
        [Tooltip("Смещение второй точки патруля от стартовой позиции. Ноль — стоит на месте")]
        [SerializeField] private Vector2 _patrolOffset;
        [SerializeField] private float _patrolSpeed = 1.5f;

        [Header("Senses")]
        [SerializeField] private float _viewDistance = 10f;
        [SerializeField] private float _viewAngle = 120f;
        [Tooltip("Вплотную замечает игрока даже спиной")]
        [SerializeField] private float _closeRange = 2.5f;
        [SerializeField] private float _hearingRadius = 8f;
        [SerializeField] private float _reactionTime = 0.5f;
        [SerializeField] private float _forgetTime = 3f;
        [SerializeField] private LayerMask _sightBlockers;

        [Header("Aim")]
        [SerializeField] private float _turnSpeed = 420f;
        [SerializeField] private float _fireAngle = 12f;

        private Transform _target;
        private Health _targetHealth;
        private Vector2 _start;
        private float _patrolT;
        private float _seenTimer;
        private float _alertUntil = -1f;
        private Vector2 _lastSeen;

        private void Start()
        {
            _start = transform.position;
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                SetTarget(player);
            }
        }

        private void OnDestroy()
        {
            if (_target != null && _target.TryGetComponent(out Weapon weapon))
            {
                weapon.Fired -= OnTargetFired;
            }
        }

        // Сразу атаковать цель, не дожидаясь, пока она попадёт в поле зрения (для скриптовых сцен)
        public void Engage(PlayerController player, float reactionTime)
        {
            SetTarget(player);
            _reactionTime = reactionTime;
            Alert(player.transform.position);
        }

        private void SetTarget(PlayerController player)
        {
            if (_target != null && _target.TryGetComponent(out Weapon oldWeapon))
            {
                oldWeapon.Fired -= OnTargetFired;
            }

            _target = player.transform;
            _targetHealth = player.Health;
            if (_target.TryGetComponent(out Weapon weapon))
            {
                weapon.Fired += OnTargetFired;
            }
        }

        private void OnTargetFired()
        {
            if (Vector2.Distance(transform.position, _target.position) <= _hearingRadius)
            {
                Alert(_target.position);
            }
        }

        private void Alert(Vector2 point)
        {
            _lastSeen = point;
            _alertUntil = Time.time + _forgetTime;
        }

        private void Update()
        {
            if (_target == null || _health.IsDead)
            {
                return;
            }

            bool sees = !_targetHealth.IsDead && CanSeeTarget();
            if (sees)
            {
                Alert(_target.position);
                _seenTimer += Time.deltaTime;
            }
            else
            {
                _seenTimer = Mathf.Max(0f, _seenTimer - Time.deltaTime * 0.5f);
            }

            if (Time.time < _alertUntil)
            {
                bool aimed = FaceTowards(_lastSeen);
                if (sees && aimed && _seenTimer >= _reactionTime)
                {
                    _weapon.TryFire(_body.right);
                }
            }
            else
            {
                Patrol();
            }
        }

        private bool CanSeeTarget()
        {
            Vector2 toTarget = _target.position - transform.position;
            float distance = toTarget.magnitude;
            if (distance > _viewDistance)
            {
                return false;
            }

            bool alerted = Time.time < _alertUntil;
            if (!alerted && distance > _closeRange && Vector2.Angle(_body.right, toTarget) > _viewAngle * 0.5f)
            {
                return false;
            }

            return !Physics2D.Linecast(transform.position, _target.position, _sightBlockers);
        }

        private bool FaceTowards(Vector2 point)
        {
            Vector2 dir = point - (Vector2)transform.position;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float next = Mathf.MoveTowardsAngle(_body.eulerAngles.z, targetAngle, _turnSpeed * Time.deltaTime);
            _body.rotation = Quaternion.Euler(0f, 0f, next);
            return Mathf.Abs(Mathf.DeltaAngle(next, targetAngle)) < _fireAngle;
        }

        private void Patrol()
        {
            float length = _patrolOffset.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            _patrolT += Time.deltaTime * _patrolSpeed / length;
            Vector2 previous = transform.position;
            Vector2 next = _start + _patrolOffset * Mathf.SmoothStep(0f, 1f, Mathf.PingPong(_patrolT, 1f));
            transform.position = next;

            Vector2 velocity = next - previous;
            if (velocity.sqrMagnitude > 0.000001f)
            {
                float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
                _body.rotation = Quaternion.Slerp(_body.rotation, Quaternion.Euler(0f, 0f, angle), 8f * Time.deltaTime);
            }
        }
    }
}
