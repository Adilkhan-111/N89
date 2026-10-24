using System.Collections;
using System.Collections.Generic;
using N89.Combat;
using N89.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace N89.Player
{
    // Киберимплант «Руки-лезвия» (миссия 2).
    // ПКМ — взмах клинком перед собой (туда, куда смотрит корпус). Анимация: клинок выбрасывается из
    // предплечья, корпус подаётся вперёд, лезвие проходит дугу ±_arc градусов, оставляя веер гаснущих копий
    // и вспышку-серп, потом втягивается обратно.
    // Враг в дуге (не за стеной) разрублен пополам: белая вспышка по линии разреза, стоп-кадр, тело
    // разваливается на две половины в синей крови; дрон взрывается, стекло бьётся. Вражеская пуля, долетевшая
    // до дуги, пока лезвие в замахе, отбита туда, куда смотрит герой: кольцо-вспышка, сноп искр, стоп-кадр,
    // пуля светится и тянет голубой хвост — и бьёт уже по врагам. Под героем — полоса перезарядки взмаха.
    public class BladeArms : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private Health _health;
        [SerializeField] private Transform _body;
        [SerializeField] private CameraFollow _cameraFollow;
        [Tooltip("Клинок: дочерний объект корпуса, вращается вокруг плеча во время взмаха")]
        [SerializeField] private Transform _blade;
        [Tooltip("Вспышка-серп во всю дугу в конце взмаха, гаснет")]
        [SerializeField] private SpriteRenderer _trail;
        [Tooltip("Шлейф, который тянется за клинком во время взмаха (передний край — на 0°)")]
        [SerializeField] private SpriteRenderer _smear;
        [Tooltip("Белый пиксель-спрайт для полосы перезарядки")]
        [SerializeField] private Sprite _pixel;
        [SerializeField] private Material _material;

        [Header("Взмах")]
        [SerializeField] private float _reach = 1.35f;
        [Tooltip("Половина дуги, градусов: клинок идёт от -arc до +arc")]
        [SerializeField] private float _arc = 75f;
        [SerializeField] private float _swingTime = 0.13f;
        [Tooltip("Клинок выбрасывается из предплечья перед взмахом")]
        [SerializeField] private float _extendTime = 0.04f;
        [Tooltip("И втягивается обратно после")]
        [SerializeField] private float _retractTime = 0.08f;
        [Tooltip("Корпус подаётся вперёд в середине взмаха, юнитов")]
        [SerializeField] private float _lunge = 0.1f;
        [Tooltip("Как часто за клинком остаётся гаснущая копия")]
        [SerializeField] private float _ghostInterval = 0.016f;
        [Tooltip("Сколько после начала взмаха лезвие ловит пули")]
        [SerializeField] private float _deflectWindow = 0.26f;
        [SerializeField] private float _cooldown = 0.45f;
        [Tooltip("Что можно рубить: враги и стекло (у всего этого есть Health)")]
        [SerializeField] private LayerMask _hitMask;
        [Tooltip("Что заслоняет цель от клинка")]
        [SerializeField] private LayerMask _wallMask;
        [Tooltip("По кому бьёт отбитая пуля")]
        [SerializeField] private LayerMask _deflectMask;
        [SerializeField] private Color _deflectTint = new Color(0.4f, 1f, 1f);
        [Tooltip("Урон клинком по Боссу (у него сотни здоровья, разрубить нельзя)")]
        [SerializeField] private int _bossDamage = 8;

        [Header("Разрубленное тело")]
        [Tooltip("Где тело (труп, голова — +x) разрезается: доля ширины от ног")]
        [SerializeField] private float _cutAt = 0.44f;
        [SerializeField] private GameObject _bloodFxPrefab;
        [SerializeField] private GameObject _bloodPoolPrefab;

        [Header("Эффекты")]
        [Tooltip("Сноп искр при отбивании")]
        [SerializeField] private GameObject _deflectFxPrefab;
        [Tooltip("Кольцо-вспышка при отбивании")]
        [SerializeField] private Sprite _ringSprite;
        [Tooltip("Стоп-кадр при попадании по врагу и при отбивании, секунд реального времени")]
        [SerializeField] private float _hitStop = 0.05f;
        [SerializeField] private float _deflectHitStop = 0.07f;
        [SerializeField] private Vector2 _barOffset = new Vector2(0f, -0.72f);

        private static readonly Color ReadyColor = new Color(0.3f, 1f, 1f);
        private static readonly Color DeniedColor = new Color(1f, 0.2f, 0.25f);

        private float _swingStart = -10f;
        private float _nextSwing;
        private float _side = 1f;
        private readonly HashSet<Health> _struck = new HashSet<Health>();

        private SpriteRenderer _bladeRenderer;
        private bool _hitStopping;

        private Transform _bar;
        private SpriteRenderer _fill;
        private float _barWidth = 0.82f;
        private bool _wasReady = true;
        private float _readyFlash;
        private float _denied;

        private bool Deflecting => Time.time - _swingStart < _deflectWindow;

        private void Awake()
        {
            _bladeRenderer = _blade.GetComponentInChildren<SpriteRenderer>(true);
            _blade.gameObject.SetActive(false);
            _trail.enabled = false;
            _smear.enabled = false;
            BuildBar();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            bool canAct = !_health.IsDead && _player.CanMove && _player.CanAim;
            if (mouse != null && canAct && mouse.rightButton.wasPressedThisFrame)
            {
                if (Time.time >= _nextSwing) StartCoroutine(Swing());
                else _denied = 1f;
            }

            if (Deflecting && !_health.IsDead) DeflectBullets();
            UpdateBar(canAct);
        }

        // ------------------------------------------------------------ Взмах

        private IEnumerator Swing()
        {
            _swingStart = Time.time;
            _nextSwing = Time.time + _cooldown;
            _struck.Clear();
            _player.WeaponLocked = true;
            // Чередуем направление взмаха: справа налево, потом слева направо
            _side = -_side;

            float startAngle = _arc * _side;
            float endAngle = -_arc * _side;
            _trail.transform.localScale = new Vector3(_trail.transform.localScale.x, Mathf.Abs(_trail.transform.localScale.y) * _side, 1f);
            if (_cameraFollow != null) _cameraFollow.Shake(0.05f, 0.08f);

            // 1. Клинок выбрасывается из предплечья, уже отведённый в начало дуги
            _blade.gameObject.SetActive(true);
            _blade.localRotation = Quaternion.Euler(0f, 0f, startAngle);
            for (float t = 0f; t < _extendTime; t += Time.deltaTime)
            {
                SetBladeLength(t / _extendTime);
                StrikeInArc();
                yield return null;
            }
            SetBladeLength(1f);

            // 2. Взмах: лезвие режет дугу, за ним тянется шлейф и веер гаснущих копий, корпус подаётся вперёд.
            // Шлейф лежит позади клинка: при взмахе по часовой — на больших углах, против — зеркально
            _smear.enabled = true;
            Vector3 smearScale = _smear.transform.localScale;
            _smear.transform.localScale = new Vector3(smearScale.x, Mathf.Abs(smearScale.y) * _side, 1f);
            float ghostAt = 0f;
            for (float t = 0f; t < _swingTime; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / _swingTime);
                float angle = Mathf.Lerp(startAngle, endAngle, k);
                _blade.localRotation = Quaternion.Euler(0f, 0f, angle);
                _smear.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                // Пока клинок прошёл мало, шлейфу не за что тянуться — разгорается по ходу
                _smear.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / _swingTime * 3f));
                _body.localPosition = (Vector3)((Vector2)_body.right * (_lunge * Mathf.Sin(k * Mathf.PI)));
                if (t >= ghostAt)
                {
                    ghostAt = t + _ghostInterval;
                    StartCoroutine(Ghost());
                }
                StrikeInArc();
                yield return null;
            }
            _blade.localRotation = Quaternion.Euler(0f, 0f, endAngle);
            _body.localPosition = Vector3.zero;
            _smear.enabled = false;

            // 3. Вспышка-серп во всю пройденную дугу, клинок втягивается
            _trail.enabled = true;
            for (float t = 0f; t < _retractTime; t += Time.deltaTime)
            {
                float k = t / _retractTime;
                SetBladeLength(1f - k);
                _trail.color = new Color(1f, 1f, 1f, 0.85f * (1f - k));
                yield return null;
            }
            _blade.gameObject.SetActive(false);
            _trail.enabled = false;
            _player.WeaponLocked = false;
        }

        // Длина клинка 0..1: растёт от шарнира у предплечья
        private void SetBladeLength(float k)
        {
            _blade.localScale = new Vector3(Mathf.Max(0.01f, k), 1f, 1f);
        }

        // Копия клинка на месте, где он только что был: голубая, быстро гаснет — получается веер-шлейф
        private IEnumerator Ghost()
        {
            if (_bladeRenderer == null) yield break;
            var ghost = new GameObject("BladeGhost").AddComponent<SpriteRenderer>();
            ghost.sprite = _bladeRenderer.sprite;
            ghost.sharedMaterial = _material != null ? _material : _bladeRenderer.sharedMaterial;
            ghost.sortingOrder = _bladeRenderer.sortingOrder - 1;
            Transform source = _bladeRenderer.transform;
            ghost.transform.SetPositionAndRotation(source.position, source.rotation);
            ghost.transform.localScale = source.lossyScale;
            const float life = 0.12f;
            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                ghost.color = new Color(0.45f, 1f, 1f, 0.55f * (1f - t / life));
                yield return null;
            }
            Destroy(ghost.gameObject);
        }

        // Всё в дуге перед героем, не за стеной: живые враги и стекло
        private void StrikeInArc()
        {
            Vector2 from = transform.position;
            Vector2 forward = _body.right;
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(from, _reach, _hitMask))
            {
                Health target = hit.GetComponentInParent<Health>();
                if (target == null || target == _health || target.IsDead || _struck.Contains(target)) continue;

                Vector2 to = hit.ClosestPoint(from) - from;
                if (to.sqrMagnitude > 0.0001f && Vector2.Angle(forward, to) > _arc + 10f) continue;
                if (Physics2D.Linecast(from, hit.ClosestPoint(from), _wallMask)) continue;

                _struck.Add(target);
                StartCoroutine(CutFlash(target.transform.position, forward));
                // Босса пополам не разрубить — клинок лишь царапает броню
                if (target.GetComponent<BossAI>() != null) target.TakeDamage(_bossDamage);
                else if (target.CorpseSprite != null) CutInHalf(target, forward);
                else target.TakeDamage(999);
                if (_cameraFollow != null) _cameraFollow.Shake(0.35f, 0.14f);
                StartCoroutine(HitStop(_hitStop));
            }
        }

        // Белая полоса по линии разреза поперёк удара — гаснет, вытягиваясь
        private IEnumerator CutFlash(Vector2 at, Vector2 dir)
        {
            var line = new GameObject("CutFlash").AddComponent<SpriteRenderer>();
            line.sprite = _pixel;
            if (_material != null) line.sharedMaterial = _material;
            line.sortingOrder = 22;
            Vector2 across = new Vector2(-dir.y, dir.x);
            line.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, Mathf.Atan2(across.y, across.x) * Mathf.Rad2Deg));
            Vector2 pixel = _pixel.bounds.size;
            const float life = 0.16f;
            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                float k = t / life;
                line.transform.localScale = new Vector3(Mathf.Lerp(0.9f, 1.6f, k) / pixel.x, Mathf.Lerp(0.09f, 0.01f, k) / pixel.y, 1f);
                line.color = Color.Lerp(Color.white, new Color(0.4f, 1f, 1f, 0f), k);
                yield return null;
            }
            Destroy(line.gameObject);
        }

        // Стоп-кадр: на долю секунды почти останавливает время — удар ощущается весомым
        private IEnumerator HitStop(float duration)
        {
            if (_hitStopping || duration <= 0f || Time.timeScale <= 0.01f) yield break;
            _hitStopping = true;
            float previous = Time.timeScale;
            const float frozen = 0.05f;
            Time.timeScale = frozen;
            yield return new WaitForSecondsRealtime(duration);
            // Если за это время время поменял кто-то другой (сценарий, смерть) — не трогаем
            if (Mathf.Approximately(Time.timeScale, frozen)) Time.timeScale = previous;
            _hitStopping = false;
        }

        // ------------------------------------------------------------ Отбивание пуль

        private void DeflectBullets()
        {
            Vector2 from = transform.position;
            Vector2 forward = _body.right;
            IReadOnlyList<Bullet> bullets = Bullet.Active;
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = bullets[i];
                if (bullet == null || bullet.Owner == transform) continue;
                Vector2 to = (Vector2)bullet.transform.position - from;
                if (to.sqrMagnitude > _reach * _reach || Vector2.Angle(forward, to) > _arc + 15f) continue;

                bullet.Deflect(forward, _deflectMask, transform, _deflectTint);
                DeflectFx(bullet, forward);
            }
        }

        // Чтобы было видно, что пуля отбита: кольцо-вспышка и искры в точке удара, стоп-кадр,
        // а сама пуля крупнее, светится и тянет за собой голубой хвост
        private void DeflectFx(Bullet bullet, Vector2 dir)
        {
            Vector2 at = bullet.transform.position;
            if (_deflectFxPrefab != null)
            {
                Instantiate(_deflectFxPrefab, at, Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            }
            StartCoroutine(Ring(at));
            StartCoroutine(HitStop(_deflectHitStop));
            if (_cameraFollow != null) _cameraFollow.Shake(0.25f, 0.1f);

            GameObject go = bullet.gameObject;
            go.transform.localScale *= 1.6f;
            TrailRenderer tail = go.AddComponent<TrailRenderer>();
            tail.time = 0.18f;
            tail.widthMultiplier = 0.12f;
            tail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            tail.minVertexDistance = 0.05f;
            if (_material != null) tail.sharedMaterial = _material;
            tail.sortingOrder = 19;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(_deflectTint, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            tail.colorGradient = gradient;

            var glow = new GameObject("Glow").AddComponent<Light2D>();
            glow.transform.SetParent(go.transform, false);
            glow.lightType = Light2D.LightType.Point;
            glow.color = _deflectTint;
            glow.intensity = 1.4f;
            glow.pointLightOuterRadius = 1.3f;
            glow.pointLightInnerRadius = 0.1f;
        }

        // Расходящееся кольцо в точке отбивания: белое, к концу голубое и прозрачное
        private IEnumerator Ring(Vector2 at)
        {
            if (_ringSprite == null) yield break;
            var ring = new GameObject("DeflectRing").AddComponent<SpriteRenderer>();
            ring.sprite = _ringSprite;
            if (_material != null) ring.sharedMaterial = _material;
            ring.sortingOrder = 21;
            ring.transform.position = at;
            const float life = 0.22f;
            for (float t = 0f; t < life; t += Time.unscaledDeltaTime)
            {
                float k = t / life;
                float eased = 1f - (1f - k) * (1f - k);
                ring.transform.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.2f, eased);
                ring.color = Color.Lerp(Color.white, new Color(_deflectTint.r, _deflectTint.g, _deflectTint.b, 0f), k);
                yield return null;
            }
            Destroy(ring.gameObject);
        }

        // ------------------------------------------------------------ Разрубленное тело

        // Труп разрезан поперёк в поясе: верхняя половина (голова, плечи) отлетает вперёд по удару, ноги остаются
        private void CutInHalf(Health victim, Vector2 dir)
        {
            Sprite corpse = victim.CorpseSprite;
            Transform victimBody = victim.transform.Find("Body");
            Material material = victimBody != null && victimBody.TryGetComponent(out SpriteRenderer source) ? source.sharedMaterial : null;
            Vector2 at = victim.transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            Rect rect = corpse.textureRect;
            float cut = Mathf.Round(rect.width * _cutAt);
            var legsRect = new Rect(rect.x, rect.y, cut, rect.height);
            var topRect = new Rect(rect.x + cut, rect.y, rect.width - cut, rect.height);
            float ppu = corpse.pixelsPerUnit;
            // Смещения половин от центра целого трупа (локально, вдоль тела)
            float legsOffset = (cut * 0.5f - rect.width * 0.5f) / ppu;
            float topOffset = ((cut + rect.width) * 0.5f - rect.width * 0.5f) / ppu;

            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            Vector2 axis = rotation * Vector2.right;
            SpriteRenderer legs = Half("Corpse_Legs", corpse.texture, legsRect, ppu, material, at + axis * legsOffset, angle);
            SpriteRenderer top = Half("Corpse_Top", corpse.texture, topRect, ppu, material, at + axis * topOffset, angle);

            StartCoroutine(Slide(legs.transform, -axis * 0.15f, Random.Range(-15f, 15f), 0.2f));
            StartCoroutine(Slide(top.transform, axis * 0.75f + Random.insideUnitCircle * 0.15f, Random.Range(-50f, 50f), 0.3f));

            if (_bloodFxPrefab != null)
            {
                Instantiate(_bloodFxPrefab, at, rotation);
                Instantiate(_bloodFxPrefab, at + axis * 0.4f, rotation);
            }

            victim.SpawnCorpse = false;
            if (victim.TryGetComponent(out EnemyAI ai)) ai.enabled = false;
            victim.Kill();
        }

        private SpriteRenderer Half(string name, Texture2D texture, Rect rect, float ppu, Material material, Vector2 position, float angle)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), ppu);
            if (material != null) sr.sharedMaterial = material;
            sr.sortingOrder = -60;
            sr.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            return sr;
        }

        // Половина съезжает (до стены, не дальше) и чуть поворачивается; на месте остановки — лужа синей крови
        private IEnumerator Slide(Transform half, Vector2 offset, float spin, float duration)
        {
            Vector2 from = half.position;
            float distance = offset.magnitude;
            RaycastHit2D wall = Physics2D.CircleCast(from, 0.25f, offset / distance, distance, _wallMask);
            Vector2 to = from + offset / distance * (wall ? Mathf.Max(0f, wall.distance - 0.05f) : distance);
            float startAngle = half.eulerAngles.z;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / duration, 3f);
                half.position = Vector2.Lerp(from, to, k);
                half.rotation = Quaternion.Euler(0f, 0f, startAngle + spin * k);
                yield return null;
            }
            half.position = to;
            if (_bloodPoolPrefab != null)
            {
                Instantiate(_bloodPoolPrefab, to, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            }
        }

        // ------------------------------------------------------------ Полоса под героем

        private void BuildBar()
        {
            _bar = new GameObject("BladeBar").transform;
            _bar.SetParent(transform, false);
            _bar.localPosition = _barOffset;
            Piece("Back", Vector2.zero, new Vector2(_barWidth + 0.08f, 0.08f), new Color(0f, 0f, 0f, 0.6f), 58);
            _fill = Piece("Fill", Vector2.zero, new Vector2(_barWidth, 0.04f), ReadyColor, 59);
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

        // Заполняется, пока взмах перезаряжается; готов — вспышка, ПКМ раньше времени — мигает красным
        private void UpdateBar(bool visible)
        {
            _bar.gameObject.SetActive(visible);
            if (!visible) return;

            float k = 1f - Mathf.Clamp01((_nextSwing - Time.time) / _cooldown);
            bool ready = k >= 1f;
            if (ready && !_wasReady) _readyFlash = 1f;
            _wasReady = ready;

            float dt = Time.unscaledDeltaTime;
            _readyFlash = Mathf.MoveTowards(_readyFlash, 0f, 4f * dt);
            _denied = Mathf.MoveTowards(_denied, 0f, 4f * dt);

            Vector2 spriteSize = _pixel.bounds.size;
            float w = Mathf.Max(0.001f, _barWidth * k);
            _fill.transform.localScale = new Vector3(w / spriteSize.x, 0.04f / spriteSize.y, 1f);
            _fill.transform.localPosition = new Vector3(-_barWidth * 0.5f + w * 0.5f, 0f, 0f);
            Color color = ready ? Color.Lerp(ReadyColor, Color.white, _readyFlash) : ReadyColor * 0.55f + new Color(0f, 0f, 0f, 0.45f);
            _fill.color = Color.Lerp(color, DeniedColor, _denied);
        }
    }
}
