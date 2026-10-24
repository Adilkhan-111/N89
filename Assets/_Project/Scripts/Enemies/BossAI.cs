using System.Collections;
using System.Collections.Generic;
using N89.Combat;
using N89.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace N89.Enemies
{
    // Босс подвала миссии 2. Огромный, в имплантах: правая рука — пулемёт, левая — ракетница.
    // Пулями его почти не пробить (их урон — 1 из сотен здоровья), всерьёз ранят только обрушение потолка
    // и взрыв газового баллона (это решает BossFight). Атаки по кругу:
    //  пулемёт — раскрутка (стволы светятся), потом очередь, которая медленно ведёт за героем — можно обогнать;
    //  ракеты — перед каждой виден пунктир оранжево-красных чёрточек до первого препятствия: сначала следит
    //  за героем, потом замирает и мигает чаще — и ракета уходит ровно по нему.
    // Между атаками идёт к герою в обход стен (NavGrid). Взлом «Отказ ног» (Immobilize) — стоит на месте
    // _immobileTime секунд, но стреляет.
    public class BossAI : MonoBehaviour
    {
        private enum Mode { Track, Gun, Telegraph }

        [SerializeField] private Health _health;
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Transform _body;

        [Header("Ходьба")]
        [SerializeField] private float _moveSpeed = 1.4f;
        [Tooltip("Скорость, пока строчит пулемёт")]
        [SerializeField] private float _fireMoveSpeed = 0.6f;
        [Tooltip("Ближе не подходит")]
        [SerializeField] private float _stopDistance = 2.5f;
        [SerializeField] private float _radius = 0.75f;
        [SerializeField] private float _turnSpeed = 200f;
        [SerializeField] private LayerMask _moveBlockers;

        [Header("Пулемёт")]
        [SerializeField] private Weapon _minigun;
        [SerializeField] private Light2D _minigunGlow;
        [SerializeField] private float _spinUp = 0.8f;
        [SerializeField] private float _burst = 2.4f;
        [Tooltip("Как быстро очередь поворачивает за героем, градусов в секунду")]
        [SerializeField] private float _gunTrack = 70f;

        [Header("Ракеты")]
        [SerializeField] private Rocket _rocketPrefab;
        [SerializeField] private Transform _launcher;
        [SerializeField] private int _rocketsPerVolley = 2;
        [Tooltip("Сколько виден пунктир перед выстрелом")]
        [SerializeField] private float _aimTime = 1.1f;
        [Tooltip("Последние секунды пунктир замирает и мигает")]
        [SerializeField] private float _lockTime = 0.35f;
        [SerializeField] private float _rocketPause = 0.5f;
        [Tooltip("Обо что рвётся ракета — до этого места рисуется пунктир")]
        [SerializeField] private LayerMask _rocketBlockers;
        [SerializeField] private Sprite _pixel;
        [SerializeField] private Material _material;
        [SerializeField] private Color _dashA = new Color(1f, 0.55f, 0.15f);
        [SerializeField] private Color _dashB = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private float _dashLength = 0.3f;
        [SerializeField] private float _dashGap = 0.18f;

        [Header("Ритм")]
        [SerializeField] private float _attackPause = 0.9f;

        [Header("Взлом ног")]
        [SerializeField] private float _immobileTime = 6f;
        [SerializeField] private GameObject _sparkFxPrefab;

        private const int MaxDashes = 70;

        private Transform _target;
        private Health _targetHealth;
        private CameraFollow _cameraFollow;
        private Mode _mode;
        private Vector2 _aim = Vector2.right;
        private float _immobileUntil = -1f;
        private readonly List<Vector2> _path = new List<Vector2>();
        private float _nextPath;
        private SpriteRenderer[] _dashes;

        public bool IsFighting { get; private set; }
        public bool IsImmobilized => Time.time < _immobileUntil;
        public Health Health => _health;
        public float Radius => _radius;

        private void Awake()
        {
            _aim = _body.right;
            if (_minigunGlow != null) _minigunGlow.intensity = 0f;
            _health.Died += _ => Stop();
            BuildDashes();
        }

        public void Activate(Transform target)
        {
            _target = target;
            _targetHealth = target.GetComponent<Health>();
            _cameraFollow = FindAnyObjectByType<CameraFollow>();
            IsFighting = true;
            StartCoroutine(Fight());
        }

        public void Stop()
        {
            IsFighting = false;
            StopAllCoroutines();
            HideDashes();
            if (_minigunGlow != null) _minigunGlow.intensity = 0f;
        }

        public void Immobilize()
        {
            _immobileUntil = Time.time + _immobileTime;
            StartCoroutine(LegSparks());
        }

        private void Update()
        {
            if (!IsFighting) return;
            if (_targetHealth != null && _targetHealth.IsDead)
            {
                Stop();
                return;
            }

            Vector2 toTarget = (Vector2)_target.position - (Vector2)transform.position;
            // Куда смотрит: между атаками — сразу на героя, в очереди — медленно ведёт, в прицеле ракеты — как задал прицел
            if (_mode == Mode.Track) _aim = RotateTowards(_aim, toTarget, _turnSpeed);
            else if (_mode == Mode.Gun) _aim = RotateTowards(_aim, toTarget, _gunTrack);
            _body.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_aim.y, _aim.x) * Mathf.Rad2Deg);

            Walk(toTarget.magnitude);
        }

        private static Vector2 RotateTowards(Vector2 from, Vector2 to, float degreesPerSecond)
        {
            if (to.sqrMagnitude < 0.0001f) return from;
            float current = Mathf.Atan2(from.y, from.x) * Mathf.Rad2Deg;
            float wanted = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float angle = Mathf.MoveTowardsAngle(current, wanted, degreesPerSecond * Time.deltaTime) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        // ------------------------------------------------------------ Ходьба

        private void Walk(float distance)
        {
            if (IsImmobilized || _mode == Mode.Telegraph || distance <= _stopDistance) return;

            Vector2 position = _rb.position;
            if (Time.time >= _nextPath)
            {
                _nextPath = Time.time + 0.4f;
                // Пути нет (герой забился в щель, куда Босс не пролезет) — идёт напрямую, сколько пустят стены
                if (!NavGrid.FindPath(position, _target.position, _radius, _moveBlockers, _path)) _path.Add(_target.position);
            }

            if (_path.Count == 0) return;
            Vector2 toPoint = _path[0] - position;
            if (toPoint.magnitude < 0.12f)
            {
                _path.RemoveAt(0);
                return;
            }

            float speed = _mode == Mode.Gun ? _fireMoveSpeed : _moveSpeed;
            float step = Mathf.Min(speed * Time.deltaTime, toPoint.magnitude);
            Vector2 dir = toPoint.normalized;
            if (!Physics2D.CircleCast(position, _radius * 0.9f, dir, step, _moveBlockers))
            {
                _rb.MovePosition(position + dir * step);
            }
        }

        private IEnumerator LegSparks()
        {
            while (IsImmobilized && IsFighting)
            {
                if (_sparkFxPrefab != null)
                {
                    Instantiate(_sparkFxPrefab, (Vector2)transform.position + Random.insideUnitCircle * _radius * 0.6f, Quaternion.identity);
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        // ------------------------------------------------------------ Атаки

        private IEnumerator Fight()
        {
            yield return new WaitForSeconds(0.6f);
            while (IsFighting)
            {
                yield return Minigun();
                yield return new WaitForSeconds(_attackPause);
                for (int i = 0; i < _rocketsPerVolley && IsFighting; i++)
                {
                    yield return FireRocket();
                    yield return new WaitForSeconds(_rocketPause);
                }
                yield return new WaitForSeconds(_attackPause);
            }
        }

        // Раскрутка — стволы разгораются и корпус дрожит; потом очередь, которая не успевает за быстрым героем
        private IEnumerator Minigun()
        {
            _mode = Mode.Gun;
            Vector3 rest = _body.localPosition;
            for (float t = 0f; t < _spinUp; t += Time.deltaTime)
            {
                if (_minigunGlow != null) _minigunGlow.intensity = Mathf.Lerp(0f, 1.6f, t / _spinUp);
                _body.localPosition = rest + (Vector3)(Random.insideUnitCircle * 0.03f);
                yield return null;
            }

            for (float t = 0f; t < _burst; t += Time.deltaTime)
            {
                _minigun.TryFire(_aim);
                _body.localPosition = rest + (Vector3)(Random.insideUnitCircle * 0.04f);
                if (_minigunGlow != null) _minigunGlow.intensity = Random.Range(1.2f, 2.2f);
                yield return null;
            }

            _body.localPosition = rest;
            if (_minigunGlow != null) _minigunGlow.intensity = 0f;
            _mode = Mode.Track;
        }

        // Пунктир траектории: следит за героем, в конце замирает и мигает — и ракета уходит точно по нему
        private IEnumerator FireRocket()
        {
            _mode = Mode.Telegraph;
            for (float t = 0f; t < _aimTime; t += Time.deltaTime)
            {
                bool locked = t >= _aimTime - _lockTime;
                if (!locked)
                {
                    Vector2 toTarget = (Vector2)_target.position - (Vector2)_launcher.position;
                    _aim = RotateTowards(_aim, toTarget, 360f);
                    _body.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_aim.y, _aim.x) * Mathf.Rad2Deg);
                }
                DrawDashes(_launcher.position, _aim, locked);
                yield return null;
            }

            HideDashes();
            Rocket rocket = Instantiate(_rocketPrefab, _launcher.position, Quaternion.identity);
            rocket.Launch(_aim, transform);
            if (_cameraFollow != null) _cameraFollow.Shake(0.15f, 0.12f);
            _mode = Mode.Track;
        }

        // ------------------------------------------------------------ Пунктир

        private void BuildDashes()
        {
            var root = new GameObject($"{name}_RocketTrajectory").transform;
            _dashes = new SpriteRenderer[MaxDashes];
            for (int i = 0; i < MaxDashes; i++)
            {
                var dash = new GameObject("Dash").AddComponent<SpriteRenderer>();
                dash.transform.SetParent(root, false);
                dash.sprite = _pixel;
                if (_material != null) dash.sharedMaterial = _material;
                dash.sortingOrder = 40;
                dash.enabled = false;
                _dashes[i] = dash;
            }
        }

        private void DrawDashes(Vector2 from, Vector2 dir, bool locked)
        {
            RaycastHit2D hit = Physics2D.Raycast(from, dir, 30f, _rocketBlockers);
            float length = hit ? hit.distance : 30f;
            float period = _dashLength + _dashGap;
            // Чёрточки бегут от босса к цели; в замершем прицеле мигают
            float scroll = Mathf.Repeat(Time.time * 4f, period);
            float alpha = locked ? (Mathf.Repeat(Time.time * 12f, 1f) > 0.5f ? 1f : 0.3f) : 0.85f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Vector2 pixel = _pixel.bounds.size;

            for (int i = 0; i < _dashes.Length; i++)
            {
                float start = i * period + scroll;
                SpriteRenderer dash = _dashes[i];
                if (start + _dashLength > length)
                {
                    dash.enabled = false;
                    continue;
                }

                dash.enabled = true;
                dash.transform.SetPositionAndRotation(from + dir * (start + _dashLength * 0.5f), Quaternion.Euler(0f, 0f, angle));
                dash.transform.localScale = new Vector3(_dashLength / pixel.x, 0.08f / pixel.y, 1f);
                Color color = i % 2 == 0 ? _dashA : _dashB;
                dash.color = new Color(color.r, color.g, color.b, alpha);
            }
        }

        private void HideDashes()
        {
            if (_dashes == null) return;
            foreach (SpriteRenderer dash in _dashes) dash.enabled = false;
        }
    }
}
