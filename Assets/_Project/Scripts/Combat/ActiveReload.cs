using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace N89.Combat
{
    // Перезарядка как active reload в Enter the Gungeon: патроны бесконечные, но когда магазин пуст
    // (или нажат R), над героем появляется полоска — бегунок ходит влево-вправо, и нужно нажать ЛКМ,
    // пока он в зелёной зоне (каждый раз в новом месте). Промах — короткая заминка и новая попытка.
    // Время реальное: замедление сканирования (TAB) не помогает.
    public class ActiveReload : MonoBehaviour
    {
        [SerializeField] private Weapon _weapon;
        [SerializeField] private Health _health;
        [Tooltip("Белый пиксель-спрайт, из которого собирается полоска")]
        [SerializeField] private Sprite _pixel;
        [SerializeField] private Material _material;
        [SerializeField] private Vector2 _offset = new Vector2(0f, 0.85f);
        [SerializeField] private float _width = 1.4f;

        [Header("Мини-игра")]
        [Tooltip("Секунд на проход бегунка от края до края")]
        [SerializeField] private float _sweepTime = 0.65f;
        [Tooltip("Ширина зелёной зоны (доля полоски)")]
        [SerializeField, Range(0.05f, 0.6f)] private float _zoneWidth = 0.4f;
        [Tooltip("Заминка после промаха")]
        [SerializeField] private float _failPause = 0.35f;

        private static readonly Color ZoneColor = new Color(0.25f, 0.95f, 0.35f);
        private static readonly Color FailColor = new Color(1f, 0.2f, 0.25f);

        private Transform _root;
        private SpriteRenderer[] _frame;
        private SpriteRenderer _zone;
        private SpriteRenderer _cursor;
        private bool _active;
        private float _time;
        private float _zoneStart;
        private float _pauseUntil;
        private float _hideAt = -1f;
        private bool _waitRelease;

        public bool IsReloading => _active;
        // Удачная перезарядка (для туториала)
        public event Action Reloaded;

        private void Awake()
        {
            BuildBar();
            _root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (N89.UI.PauseMenu.IsPaused) return;
            if (_weapon == null || _weapon.MagazineSize <= 0)
            {
                return;
            }

            if (_health != null && _health.IsDead)
            {
                Stop();
                _root.gameObject.SetActive(false);
                return;
            }

            Mouse mouse = Mouse.current;
            // Клик, которым попали в зону, не должен ещё и выстрелить: ждём, пока ЛКМ отпустят
            if (_waitRelease && (mouse == null || !mouse.leftButton.isPressed))
            {
                _waitRelease = false;
                _weapon.Reloading = false;
            }

            // Полоска ненадолго остаётся после удачной перезарядки — мигнуть
            if (!_active && _hideAt > 0f && Time.unscaledTime >= _hideAt)
            {
                _hideAt = -1f;
                _root.gameObject.SetActive(false);
            }

            Keyboard kb = Keyboard.current;
            bool manual = kb != null && kb.rKey.wasPressedThisFrame && _weapon.Ammo < _weapon.MagazineSize;
            if (!_active && (_weapon.Ammo <= 0 || manual))
            {
                Begin();
            }

            if (!_active)
            {
                return;
            }

            if (Time.unscaledTime < _pauseUntil)
            {
                return;
            }

            if (_pauseUntil > 0f)
            {
                // Заминка после промаха закончилась — новая попытка
                _pauseUntil = 0f;
                NewAttempt();
            }

            _time += Time.unscaledDeltaTime;
            float t = Mathf.PingPong(_time / _sweepTime, 1f);
            PlaceCursor(t);

            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                if (t >= _zoneStart && t <= _zoneStart + _zoneWidth)
                {
                    Succeed();
                }
                else
                {
                    Fail();
                }
            }
        }

        private void Begin()
        {
            _active = true;
            _waitRelease = false;
            _weapon.Reloading = true;
            _hideAt = -1f;
            _root.gameObject.SetActive(true);
            NewAttempt();
        }

        private void NewAttempt()
        {
            _time = 0f;
            // Зона не у самого левого края, чтобы было время среагировать
            _zoneStart = Random.Range(0.25f, 1f - _zoneWidth);
            SetFrameColor(Color.white);
            _cursor.color = Color.white;
            _zone.color = ZoneColor;
            PlaceZone();
            PlaceCursor(0f);
        }

        private void Succeed()
        {
            _weapon.Refill();
            Stop();
            _weapon.Reloading = true;
            _waitRelease = true;
            _zone.color = Color.white;
            SetFrameColor(ZoneColor);
            _hideAt = Time.unscaledTime + 0.12f;
            Reloaded?.Invoke();
        }

        private void Fail()
        {
            _pauseUntil = Time.unscaledTime + _failPause;
            SetFrameColor(FailColor);
            _cursor.color = FailColor;
            _zone.color = ZoneColor * 0.5f;
        }

        private void Stop()
        {
            _active = false;
            _pauseUntil = 0f;
            if (_weapon != null)
            {
                _weapon.Reloading = false;
            }
        }

        // ---------------------------------------------------------------- Полоска

        private void BuildBar()
        {
            _root = new GameObject("ReloadBar").transform;
            _root.SetParent(transform, false);
            _root.localPosition = _offset;

            float half = _width * 0.5f;
            Piece("Back", Vector2.zero, new Vector2(_width + 0.14f, 0.3f), new Color(0f, 0f, 0f, 0.55f), 60);
            Piece("Track", Vector2.zero, new Vector2(_width, 0.05f), new Color(0.35f, 0.35f, 0.42f), 61);
            _zone = Piece("Zone", Vector2.zero, new Vector2(_width * _zoneWidth, 0.08f), ZoneColor, 62);
            _cursor = Piece("Cursor", Vector2.zero, new Vector2(0.045f, 0.24f), Color.white, 63);

            // Скобки по краям: [ ]
            _frame = new[]
            {
                Piece("BracketL", new Vector2(-half, 0f), new Vector2(0.04f, 0.26f), Color.white, 63),
                Piece("BracketLTop", new Vector2(-half + 0.05f, 0.11f), new Vector2(0.1f, 0.04f), Color.white, 63),
                Piece("BracketLBottom", new Vector2(-half + 0.05f, -0.11f), new Vector2(0.1f, 0.04f), Color.white, 63),
                Piece("BracketR", new Vector2(half, 0f), new Vector2(0.04f, 0.26f), Color.white, 63),
                Piece("BracketRTop", new Vector2(half - 0.05f, 0.11f), new Vector2(0.1f, 0.04f), Color.white, 63),
                Piece("BracketRBottom", new Vector2(half - 0.05f, -0.11f), new Vector2(0.1f, 0.04f), Color.white, 63),
            };
        }

        private SpriteRenderer Piece(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root, false);
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

        private void PlaceZone()
        {
            float x = (_zoneStart + _zoneWidth * 0.5f - 0.5f) * _width;
            _zone.transform.localPosition = new Vector3(x, 0f, 0f);
        }

        private void PlaceCursor(float t)
        {
            _cursor.transform.localPosition = new Vector3((t - 0.5f) * _width, 0f, 0f);
        }

        private void SetFrameColor(Color color)
        {
            foreach (SpriteRenderer piece in _frame)
            {
                piece.color = color;
            }
        }
    }
}
