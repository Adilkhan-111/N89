using System.Collections;
using N89.Combat;
using N89.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace N89.Player
{
    // Киберимплант «Руки гориллы» (миссия 1).
    // Нажать ПКМ — рывок вперёд (туда, куда смотрит корпус) с ударом в конце: удар дробит врага насмерть
    // и отшвыривает тело далеко назад, до стены.
    // Зажать ПКМ — блок: руки закрывают от пуль, держит 3 попадания (каждое — треть шкалы), после третьего
    // блок ломается и восстанавливается 7 секунд. Неполный блок понемногу подзаряжается сам.
    // Шкала блока — под героем. В блоке и в рывке оружие молчит.
    public class GorillaArms : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _health;
        [SerializeField] private Rigidbody2D _rb;
        [SerializeField] private Transform _body;
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private SpriteRenderer _guard;
        [Tooltip("Белый пиксель-спрайт для шкалы блока")]
        [SerializeField] private Sprite _pixel;
        [SerializeField] private Material _material;

        [Header("Рывок")]
        [Tooltip("Короче этого ПКМ — рывок, дольше — блок")]
        [SerializeField] private float _tapTime = 0.18f;
        [SerializeField] private float _dashDistance = 3.2f;
        [SerializeField] private float _dashDuration = 0.16f;
        [SerializeField] private float _dashCooldown = 0.5f;
        [SerializeField] private float _strikeReach = 0.5f;
        [SerializeField] private float _strikeRadius = 0.55f;
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private LayerMask _wallMask;

        [Header("Тело врага")]
        [SerializeField] private float _flingDistance = 5f;
        [SerializeField] private float _flingDuration = 0.45f;
        [SerializeField] private GameObject _bloodFxPrefab;
        [SerializeField] private GameObject _bloodPoolPrefab;

        [Header("Блок")]
        [SerializeField] private int _blockCharges = 3;
        [SerializeField] private float _breakCooldown = 7f;
        [Tooltip("Секунд на одно деление, пока блок не сломан и не поднят")]
        [SerializeField] private float _regenTime = 4f;
        [SerializeField] private float _blockSpeed = 0.45f;
        [SerializeField] private Vector2 _barOffset = new Vector2(0f, -0.72f);
        [SerializeField] private GameObject _blockFxPrefab;

        private static readonly Color ChargeColor = new Color(1f, 0.7f, 0.25f);
        private static readonly Color EmptyColor = new Color(0.15f, 0.12f, 0.12f, 0.9f);
        private static readonly Color BrokenColor = new Color(1f, 0.2f, 0.25f);

        private int _charges;
        private float _regen;
        private float _brokenUntil = -1f;
        private bool _pressed;
        private float _pressStart;
        private bool _blocking;
        private bool _dashing;
        private float _nextDash;
        private float _flash;

        private Transform _bar;
        private SpriteRenderer[] _segments;
        private SpriteRenderer _cooldownFill;

        public bool IsBlocking => _blocking;
        public bool IsBroken => Time.time < _brokenUntil;

        private void Awake()
        {
            _charges = _blockCharges;
            _health.Guard = TryBlockHit;
            if (_guard != null) _guard.enabled = false;
            BuildBar();
        }

        private void OnDestroy()
        {
            if (_health != null && _health.Guard == TryBlockHit) _health.Guard = null;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            bool canAct = !_health.IsDead && _player.CanMove && _player.CanAim;
            if (mouse == null || !canAct)
            {
                _pressed = false;
                SetBlocking(false);
                UpdateBar();
                return;
            }

            if (IsBroken == false && _brokenUntil > 0f)
            {
                // Блок восстановился после поломки
                _brokenUntil = -1f;
                _charges = _blockCharges;
                _flash = 1f;
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                _pressed = true;
                _pressStart = Time.unscaledTime;
            }

            if (_pressed && mouse.rightButton.isPressed && !_dashing && Time.unscaledTime - _pressStart >= _tapTime)
            {
                SetBlocking(!IsBroken && _charges > 0);
            }

            if (_pressed && mouse.rightButton.wasReleasedThisFrame)
            {
                bool wasTap = !_blocking && Time.unscaledTime - _pressStart < _tapTime;
                _pressed = false;
                SetBlocking(false);
                if (wasTap && !_dashing && Time.time >= _nextDash)
                {
                    StartCoroutine(Dash());
                }
            }

            // Неполный, но не сломанный блок подзаряжается, пока руки опущены
            if (!_blocking && !IsBroken && _charges < _blockCharges)
            {
                _regen += Time.deltaTime;
                if (_regen >= _regenTime)
                {
                    _regen = 0f;
                    _charges++;
                }
            }

            UpdateBar();
        }

        // ------------------------------------------------------------ Блок

        private void SetBlocking(bool on)
        {
            if (_blocking == on) return;
            _blocking = on;
            _regen = 0f;
            _player.SpeedMultiplier = on ? _blockSpeed : 1f;
            _player.WeaponLocked = on || _dashing;
            if (_guard != null) _guard.enabled = on;
        }

        // Вызывается из Health перед уроном: в блоке пуля разбивается о руки
        private bool TryBlockHit()
        {
            if (!_blocking || _charges <= 0)
            {
                return false;
            }

            _charges--;
            _flash = 1f;
            if (_blockFxPrefab != null)
            {
                Instantiate(_blockFxPrefab, _body.position + _body.right * 0.4f, Quaternion.identity);
            }
            if (_cameraFollow != null) _cameraFollow.Shake(0.2f, 0.1f);

            if (_charges == 0)
            {
                // Третье попадание: блок ломается и уходит в перезарядку
                _brokenUntil = Time.time + _breakCooldown;
                SetBlocking(false);
                if (_cameraFollow != null) _cameraFollow.Shake(0.4f, 0.2f);
            }
            return true;
        }

        // ------------------------------------------------------------ Рывок и удар

        private IEnumerator Dash()
        {
            _dashing = true;
            _nextDash = Time.time + _dashCooldown;
            _player.ExternalVelocity = true;
            _player.WeaponLocked = true;

            Vector2 dir = _body.right;
            float speed = _dashDistance / _dashDuration;
            float ghostAt = 0f;
            Health victim = null;
            for (float t = 0f; t < _dashDuration && victim == null; t += Time.fixedDeltaTime)
            {
                _rb.linearVelocity = dir * speed;
                if (t >= ghostAt)
                {
                    ghostAt = t + 0.03f;
                    StartCoroutine(Afterimage());
                }

                victim = FindVictim(dir);
                yield return new WaitForFixedUpdate();
            }

            if (victim == null)
            {
                victim = FindVictim(dir);
            }

            _rb.linearVelocity = dir * 2f;
            if (victim != null)
            {
                Strike(victim, dir);
            }

            _player.ExternalVelocity = false;
            _player.WeaponLocked = _blocking;
            _dashing = false;
        }

        // Первый живой враг перед кулаком, не за стеной
        private Health FindVictim(Vector2 dir)
        {
            Vector2 from = _rb.position;
            Vector2 point = from + dir * _strikeReach;
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(point, _strikeRadius, _enemyMask))
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health == null || health == _health || health.IsDead) continue;
                if (Physics2D.Linecast(from, health.transform.position, _wallMask)) continue;
                return health;
            }
            return null;
        }

        private void Strike(Health victim, Vector2 dir)
        {
            if (_cameraFollow != null) _cameraFollow.Shake(0.45f, 0.18f);

            // Тело — копия спрайта врага: отлетает, крутясь, до стены и остаётся лежать
            Vector2 from = victim.transform.position;
            Transform victimBody = victim.transform.Find("Body");
            SpriteRenderer source = victimBody != null ? victimBody.GetComponent<SpriteRenderer>() : null;
            if (source != null)
            {
                var corpse = new GameObject("Corpse").AddComponent<SpriteRenderer>();
                corpse.sprite = source.sprite;
                corpse.sharedMaterial = source.sharedMaterial;
                corpse.sortingOrder = source.sortingOrder - 1;
                corpse.color = new Color(0.7f, 0.6f, 0.65f);
                corpse.transform.SetPositionAndRotation(from, victimBody.rotation);
                StartCoroutine(Fling(corpse.transform, from, dir));
            }

            if (victim.TryGetComponent(out EnemyAI ai)) ai.enabled = false;
            victim.Kill();
        }

        private IEnumerator Fling(Transform corpse, Vector2 from, Vector2 dir)
        {
            RaycastHit2D wall = Physics2D.CircleCast(from, 0.3f, dir, _flingDistance, _wallMask);
            float distance = wall ? Mathf.Max(0f, wall.distance - 0.05f) : _flingDistance;
            Vector2 to = from + dir * distance;
            float spin = Random.Range(360f, 720f) * (Random.value < 0.5f ? -1f : 1f);
            float startAngle = corpse.eulerAngles.z;
            float nextBlood = 0f;

            for (float t = 0f; t < _flingDuration; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / _flingDuration, 3f);
                corpse.position = Vector2.Lerp(from, to, k);
                corpse.rotation = Quaternion.Euler(0f, 0f, startAngle + spin * k);
                if (_bloodFxPrefab != null && t >= nextBlood)
                {
                    nextBlood = t + 0.1f;
                    Instantiate(_bloodFxPrefab, corpse.position, Quaternion.identity);
                }
                yield return null;
            }

            corpse.position = to;
            if (_bloodPoolPrefab != null)
            {
                Instantiate(_bloodPoolPrefab, to, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            }
            if (wall && _cameraFollow != null) _cameraFollow.Shake(0.2f, 0.1f);
        }

        // След рывка: полупрозрачная копия героя быстро гаснет
        private IEnumerator Afterimage()
        {
            if (_bodyRenderer == null) yield break;
            var ghost = new GameObject("Afterimage").AddComponent<SpriteRenderer>();
            ghost.sprite = _bodyRenderer.sprite;
            ghost.sharedMaterial = _material != null ? _material : _bodyRenderer.sharedMaterial;
            ghost.sortingOrder = _bodyRenderer.sortingOrder - 1;
            ghost.transform.SetPositionAndRotation(_bodyRenderer.transform.position, _bodyRenderer.transform.rotation);
            Color color = new Color(1f, 0.6f, 0.2f, 0.5f);
            for (float t = 0f; t < 0.2f; t += Time.deltaTime)
            {
                ghost.color = new Color(color.r, color.g, color.b, color.a * (1f - t / 0.2f));
                yield return null;
            }
            Destroy(ghost.gameObject);
        }

        // ------------------------------------------------------------ Шкала под героем

        private void BuildBar()
        {
            _bar = new GameObject("BlockBar").transform;
            _bar.SetParent(transform, false);
            _bar.localPosition = _barOffset;

            const float segment = 0.24f;
            const float gap = 0.05f;
            float width = _blockCharges * segment + (_blockCharges - 1) * gap;
            Piece("Back", Vector2.zero, new Vector2(width + 0.08f, 0.13f), new Color(0f, 0f, 0f, 0.6f), 58);
            _segments = new SpriteRenderer[_blockCharges];
            for (int i = 0; i < _blockCharges; i++)
            {
                float x = -width * 0.5f + segment * 0.5f + i * (segment + gap);
                _segments[i] = Piece($"Charge{i}", new Vector2(x, 0f), new Vector2(segment, 0.07f), ChargeColor, 59);
            }
            // Полоса перезарядки сломанного блока — растёт слева направо (размер и позицию задаёт UpdateBar)
            _cooldownFill = Piece("Cooldown", Vector2.zero, new Vector2(width, 0.07f), BrokenColor, 60);
            _cooldownFill.enabled = false;
        }

        private SpriteRenderer Piece(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_bar, false);
            go.transform.localPosition = position;
            Vector2 spriteSize = _pixel.bounds.size;
            go.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _pixel;
            if (_material != null) sr.sharedMaterial = _material;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        private void UpdateBar()
        {
            bool visible = !_health.IsDead && _player.CanMove;
            _bar.gameObject.SetActive(visible);
            if (!visible) return;

            _flash = Mathf.MoveTowards(_flash, 0f, 4f * Time.unscaledDeltaTime);
            if (IsBroken)
            {
                // Сломан: деления пустые, красная полоса заполняется до конца перезарядки
                foreach (SpriteRenderer s in _segments) s.color = EmptyColor;
                float k = 1f - (_brokenUntil - Time.time) / _breakCooldown;
                float width = _segments.Length * 0.24f + (_segments.Length - 1) * 0.05f;
                Vector2 spriteSize = _pixel.bounds.size;
                _cooldownFill.enabled = true;
                _cooldownFill.transform.localScale = new Vector3(width * k / spriteSize.x, 0.07f / spriteSize.y, 1f);
                _cooldownFill.transform.localPosition = new Vector3(-width * 0.5f + width * k * 0.5f, 0f, 0f);
                _cooldownFill.color = Mathf.Repeat(Time.unscaledTime * 3f, 1f) > 0.5f ? BrokenColor : BrokenColor * 0.6f;
                return;
            }

            _cooldownFill.enabled = false;
            for (int i = 0; i < _segments.Length; i++)
            {
                Color on = Color.Lerp(ChargeColor, Color.white, _flash);
                _segments[i].color = i < _charges ? (_blocking ? on : on * 0.85f + new Color(0, 0, 0, 0.15f)) : EmptyColor;
            }
        }
    }
}
