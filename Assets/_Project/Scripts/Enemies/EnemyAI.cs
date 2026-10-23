using System.Collections.Generic;
using N89.Combat;
using N89.Player;
using UnityEngine;

namespace N89.Enemies
{
    // Враг корпорации Е (охранник или боевой дрон): патрулирует, замечает игрока
    // (дальность + угол обзора + линия видимости), после короткой реакции разворачивается и стреляет.
    // Слышит выстрелы рядом.
    // Охранник преследует: идёт туда, где видел/слышал игрока, осматривается, потом по своему следу
    // возвращается на пост. Дрон с поста не уходит — облетает свой периметр и на углах сканирует.
    public class EnemyAI : MonoBehaviour
    {
        private const float ArriveDistance = 0.25f;
        private const float TrailStep = 0.6f;

        [SerializeField] private Transform _body;
        [SerializeField] private Weapon _weapon;
        [SerializeField] private Health _health;

        [Header("Patrol")]
        [Tooltip("Смещение второй точки патруля от стартовой позиции (ходит туда-обратно). Ноль — стоит на месте")]
        [SerializeField] private Vector2 _patrolOffset;
        [Tooltip("Периметр: точки облёта относительно старта, по кругу. Если задан — используется вместо Patrol Offset")]
        [SerializeField] private Vector2[] _perimeter;
        [SerializeField] private float _patrolSpeed = 1.5f;
        [Tooltip("Сколько секунд осматривается в каждой точке патруля")]
        [SerializeField] private float _waypointPause = 0.8f;

        [Header("Chase")]
        [Tooltip("Преследовать игрока. Выключено — стоит/летает на своём посту и стреляет оттуда")]
        [SerializeField] private bool _chases = true;
        [SerializeField] private float _chaseSpeed = 3.2f;
        [Tooltip("Если видит игрока — подходит не ближе этого")]
        [SerializeField] private float _stopDistance = 3f;
        [SerializeField] private float _bodyRadius = 0.38f;
        [SerializeField] private LayerMask _moveBlockers;

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
        [Tooltip("Насколько поворачивает голову, осматриваясь")]
        [SerializeField] private float _lookAroundAngle = 70f;

        private readonly List<Vector2> _trail = new List<Vector2>();
        private Transform _target;
        private Health _targetHealth;
        private Vector2[] _route;
        private int _routeIndex;
        private int _routeStep = 1;
        private float _pauseUntil;
        private float _lookBase;
        private float _postAngle;
        private float _seenTimer;
        private float _alertUntil = -1f;
        private Vector2 _lastSeen;
        private bool _searching;
        private float _stuckTime;

        // Для катсцен: повернуть охранника и найти ствол
        public Transform Body => _body;

        private void Start()
        {
            BuildRoute(transform.position);
            _lookBase = _body.eulerAngles.z;
            _postAngle = _lookBase;

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

        private void BuildRoute(Vector2 start)
        {
            if (_perimeter != null && _perimeter.Length > 0)
            {
                _route = new Vector2[_perimeter.Length + 1];
                _route[0] = start;
                for (int i = 0; i < _perimeter.Length; i++) _route[i + 1] = start + _perimeter[i];
            }
            else if (_patrolOffset.sqrMagnitude > 0.0001f)
            {
                _route = new[] { start, start + _patrolOffset };
            }
            else
            {
                _route = new[] { start };
            }
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
            _searching = false;
            _stuckTime = 0f;
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
                Combat(sees);
            }
            else if (_trail.Count > 0)
            {
                ReturnToPost();
            }
            else
            {
                Patrol();
            }
        }

        private void Combat(bool sees)
        {
            if (!_chases)
            {
                Shoot(sees, FaceTowards(_lastSeen));
                return;
            }

            if (_searching)
            {
                // Дошёл до места, где потерял игрока, — крутит головой, пока не забудет
                LookAround();
                return;
            }

            Vector2 pos = transform.position;
            float distance = Vector2.Distance(pos, _lastSeen);
            float keep = sees ? _stopDistance : ArriveDistance;

            bool aimed = FaceTowards(_lastSeen);
            Shoot(sees, aimed);

            if (distance > keep)
            {
                // Стреляет на ходу, но пока целится — идёт медленнее
                float speed = sees ? _chaseSpeed * 0.6f : _chaseSpeed;
                float moved = MoveTowards(_lastSeen, speed);
                RecordTrail(pos);

                // Упёрся в стену (дверь закрылась и т.п.) — дальше не пройти, ищет отсюда
                _stuckTime = moved < speed * Time.deltaTime * 0.2f ? _stuckTime + Time.deltaTime : 0f;
                if (!sees && _stuckTime > 0.5f)
                {
                    StartSearching();
                }
            }
            else if (!sees)
            {
                StartSearching();
            }
        }

        private void Shoot(bool sees, bool aimed)
        {
            if (sees && aimed && _seenTimer >= _reactionTime)
            {
                _weapon.TryFire(_body.right);
            }
        }

        private void StartSearching()
        {
            _searching = true;
            _lookBase = _body.eulerAngles.z;
        }

        private void ReturnToPost()
        {
            Vector2 point = _trail[_trail.Count - 1];
            MoveTowards(point, _patrolSpeed > 0f ? Mathf.Max(_patrolSpeed, 1.5f) : 1.5f);
            FaceMovement(point);
            if (Vector2.Distance(transform.position, point) < ArriveDistance)
            {
                _trail.RemoveAt(_trail.Count - 1);
            }
        }

        private void RecordTrail(Vector2 from)
        {
            if (_trail.Count == 0 || Vector2.Distance(_trail[_trail.Count - 1], from) > TrailStep)
            {
                _trail.Add(from);
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

        private void FaceMovement(Vector2 point)
        {
            Vector2 dir = point - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                _body.rotation = Quaternion.Slerp(_body.rotation, Quaternion.Euler(0f, 0f, angle), 8f * Time.deltaTime);
            }
        }

        // Плавно водит взглядом влево-вправо от направления, в котором остановился
        private void LookAround()
        {
            float angle = _lookBase + Mathf.Sin(Time.time * 2.5f) * _lookAroundAngle;
            float next = Mathf.MoveTowardsAngle(_body.eulerAngles.z, angle, _turnSpeed * 0.5f * Time.deltaTime);
            _body.rotation = Quaternion.Euler(0f, 0f, next);
        }

        private void Patrol()
        {
            if (_route.Length < 2)
            {
                // Стоит на посту — вернувшись, разворачивается как стоял
                float next = Mathf.MoveTowardsAngle(_body.eulerAngles.z, _postAngle, _turnSpeed * 0.5f * Time.deltaTime);
                _body.rotation = Quaternion.Euler(0f, 0f, next);
                return;
            }

            if (Time.time < _pauseUntil)
            {
                LookAround();
                return;
            }

            Vector2 point = _route[_routeIndex];
            MoveTowards(point, _patrolSpeed);
            FaceMovement(point);
            if (Vector2.Distance(transform.position, point) >= ArriveDistance)
            {
                return;
            }

            _pauseUntil = Time.time + _waypointPause;
            _lookBase = _body.eulerAngles.z;

            // Периметр облетается по кругу, обычный патруль — туда-обратно
            bool loop = _perimeter != null && _perimeter.Length > 0;
            if (loop)
            {
                _routeIndex = (_routeIndex + 1) % _route.Length;
            }
            else
            {
                if (_routeIndex + _routeStep < 0 || _routeIndex + _routeStep >= _route.Length) _routeStep = -_routeStep;
                _routeIndex += _routeStep;
            }
        }

        // Движение со скольжением вдоль стен. Возвращает, сколько реально прошёл
        private float MoveTowards(Vector2 point, float speed)
        {
            Vector2 pos = transform.position;
            Vector2 delta = Vector2.ClampMagnitude(point - pos, speed * Time.deltaTime);
            Vector2 start = pos;

            for (int i = 0; i < 2 && delta.sqrMagnitude > 0.000001f; i++)
            {
                float length = delta.magnitude;
                Vector2 dir = delta / length;
                RaycastHit2D hit = Physics2D.CircleCast(pos, _bodyRadius, dir, length + 0.02f, _moveBlockers);
                if (!hit)
                {
                    pos += delta;
                    break;
                }

                float allowed = Mathf.Max(0f, hit.distance - 0.02f);
                pos += dir * allowed;
                Vector2 rest = dir * (length - allowed);
                delta = rest - Vector2.Dot(rest, hit.normal) * hit.normal;
            }

            transform.position = pos;
            return Vector2.Distance(start, pos);
        }
    }
}
