using System;
using System.Collections;
using N89.Combat;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace N89.Player
{
    // Сандевистан — встроенный имплант миссии 3, заменяет взлом (HackingController на герое нет).
    // TAB при полной шкале — мир замедляется, Рэй — нет: ходит и стреляет в прежнем темпе, импланты
    // на руки работают. Включённый не выключить: держится _duration с, пока шкала не опустеет;
    // пустая шкала заполняется за _rechargeTime с. Время шкалы — реальное.
    [RequireComponent(typeof(PlayerController))]
    public class Sandevistan : MonoBehaviour
    {
        [SerializeField] private float _duration = 3f;
        [SerializeField] private float _rechargeTime = 8f;
        [Tooltip("Скорость мира, пока Сандевистан включён")]
        [SerializeField] private float _timeScale = 0.25f;

        [Header("Шлейф")]
        [Tooltip("Корпус героя: с него снимаются копии-призраки")]
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private float _ghostInterval = 0.04f;
        [SerializeField] private float _ghostLife = 0.35f;
        [SerializeField] private Color _ghostA = new Color(0.25f, 0.6f, 1f, 0.6f);
        [SerializeField] private Color _ghostB = new Color(1f, 0.85f, 0.2f, 0.6f);

        [Header("HUD (необязательно)")]
        [Tooltip("Полоска энергии: Image с Fill Method = Horizontal")]
        [SerializeField] private Image _energyFill;
        [SerializeField] private Color _readyColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _chargingColor = new Color(0.25f, 0.6f, 1f, 0.6f);
        [Tooltip("Синий фильтр экрана, пока Сандевистан включён")]
        [SerializeField] private CanvasGroup _overlay;

        private PlayerController _player;
        private Weapon _weapon;
        private float _defaultFixedDeltaTime;
        private float _nextGhost;
        private float _flash;

        // 0..1
        public float Energy { get; private set; } = 1f;
        public bool IsActive { get; private set; }
        public bool IsReady => !IsActive && Energy >= 1f;
        // Сценарий (противостояние в финале миссии 3): шкала сразу полная
        public void Refill() => Energy = 1f;

        // Катсцена (концовка миссии 3): ускорение включается само, с полной шкалой
        public void ForceActivate()
        {
            if (IsActive) return;
            Energy = 1f;
            Activate();
        }

        public event Action Activated;
        public event Action Depleted;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _weapon = GetComponentInChildren<Weapon>();
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void OnDisable()
        {
            if (IsActive) Stop();
        }

        private void Update()
        {
            if (N89.UI.PauseMenu.IsPaused) return;
            float dt = Time.unscaledDeltaTime;

            if (_player.Health != null && _player.Health.IsDead)
            {
                if (IsActive) Stop();
                UpdateHud(dt);
                return;
            }

            if (IsActive)
            {
                Energy = Mathf.Max(0f, Energy - dt / _duration);
                if (Time.unscaledTime >= _nextGhost)
                {
                    _nextGhost = Time.unscaledTime + _ghostInterval;
                    SpawnGhost();
                }
                if (Energy <= 0f) Stop();
            }
            else
            {
                Energy = Mathf.Min(1f, Energy + dt / _rechargeTime);
                Keyboard kb = Keyboard.current;
                if (kb != null && kb.tabKey.wasPressedThisFrame && _player.CanMove)
                {
                    // Не готов — шкала мигает
                    if (IsReady) Activate();
                    else _flash = 1f;
                }
            }

            UpdateHud(dt);
        }

        private void Activate()
        {
            IsActive = true;
            _nextGhost = 0f;
            SetWorldSpeed(_timeScale);
            N89.Core.Sfx.Play(N89.Core.Sfx.SandevistanOn);
            Activated?.Invoke();
        }

        private void Stop()
        {
            IsActive = false;
            SetWorldSpeed(1f);
            N89.Core.Sfx.Play(N89.Core.Sfx.SandevistanOff);
            Depleted?.Invoke();
        }

        private void SetWorldSpeed(float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = _defaultFixedDeltaTime * scale;
            _player.TimeCompensation = 1f / scale;
            if (_weapon != null) _weapon.CooldownScale = scale;
        }

        // Копия героя на месте — гаснет в реальном времени, цвета чередуются: синий и жёлтый
        private void SpawnGhost()
        {
            if (_body == null || _body.sprite == null) return;
            var ghost = new GameObject("SandevistanGhost").AddComponent<SpriteRenderer>();
            ghost.sprite = _body.sprite;
            ghost.sortingLayerID = _body.sortingLayerID;
            ghost.sortingOrder = _body.sortingOrder - 1;
            ghost.transform.SetPositionAndRotation(_body.transform.position, _body.transform.rotation);
            ghost.transform.localScale = _body.transform.lossyScale;
            ghost.color = Mathf.Repeat(Time.unscaledTime * 6f, 1f) > 0.5f ? _ghostA : _ghostB;
            StartCoroutine(FadeGhost(ghost));
        }

        private IEnumerator FadeGhost(SpriteRenderer ghost)
        {
            Color start = ghost.color;
            for (float t = 0f; t < _ghostLife; t += Time.unscaledDeltaTime)
            {
                ghost.color = new Color(start.r, start.g, start.b, start.a * (1f - t / _ghostLife));
                yield return null;
            }
            Destroy(ghost.gameObject);
        }

        private void UpdateHud(float dt)
        {
            if (_overlay != null) _overlay.alpha = Mathf.MoveTowards(_overlay.alpha, IsActive ? 1f : 0f, 6f * dt);
            if (_energyFill == null) return;
            _energyFill.fillAmount = Energy;
            _flash = Mathf.MoveTowards(_flash, 0f, 3f * dt);
            Color color = IsActive || Energy >= 1f ? _readyColor : _chargingColor;
            if (IsActive) color = Color.Lerp(_readyColor, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 20f));
            _energyFill.color = Color.Lerp(color, new Color(1f, 0.3f, 0.35f), _flash);
        }
    }
}
