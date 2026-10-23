using System;
using System.Collections.Generic;
using N89.Combat;
using N89.World;
using UnityEngine;

namespace N89.Hacking
{
    // Всё, что можно взломать: двери, дроны, турели. Подсвечивается в режиме сканирования.
    public class HackableDevice : MonoBehaviour
    {
        private static readonly List<HackableDevice> s_all = new List<HackableDevice>();

        [SerializeField] private string _deviceName = "УСТРОЙСТВО";
        [SerializeField] private QuickhackData[] _quickhacks;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private SpriteRenderer[] _highlightRenderers;
        [SerializeField] private Color _scanColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField] private GameObject _shortCircuitFxPrefab;

        private Color[] _baseColors;
        private float _lastHighlight;

        public static IReadOnlyList<HackableDevice> All => s_all;

        // Перезагрузка домена при входе в Play Mode выключена — чистим список вручную
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => s_all.Clear();
        public string DeviceName => _deviceName;
        public IReadOnlyList<QuickhackData> Quickhacks => _quickhacks;
        public Collider2D Collider => _collider;

        public event Action<HackableDevice, QuickhackData> Hacked;

        private void Awake()
        {
            _baseColors = new Color[_highlightRenderers.Length];
            for (int i = 0; i < _highlightRenderers.Length; i++)
            {
                _baseColors[i] = _highlightRenderers[i].color;
            }
        }

        private void OnEnable()
        {
            s_all.Add(this);
        }

        private void OnDisable()
        {
            s_all.Remove(this);
        }

        public bool IsAvailable(QuickhackData hack)
        {
            if (hack == null)
            {
                return false;
            }

            switch (hack.Effect)
            {
                case QuickhackEffect.Unlock:
                    Door door = GetComponent<Door>();
                    return door != null && !door.IsOpen;
                case QuickhackEffect.ShortCircuit:
                    Health health = GetComponent<Health>();
                    return health != null && !health.IsDead;
                default:
                    return true;
            }
        }

        public bool HasAnyAvailable()
        {
            foreach (QuickhackData hack in _quickhacks)
            {
                if (IsAvailable(hack))
                {
                    return true;
                }
            }

            return false;
        }

        public void Apply(QuickhackData hack)
        {
            switch (hack.Effect)
            {
                case QuickhackEffect.Unlock:
                    GetComponent<Door>()?.Open();
                    break;
                case QuickhackEffect.ShortCircuit:
                    if (_shortCircuitFxPrefab != null)
                    {
                        Instantiate(_shortCircuitFxPrefab, transform.position, Quaternion.identity);
                    }
                    GetComponent<Health>()?.TakeDamage(hack.Damage);
                    break;
            }

            Hacked?.Invoke(this, hack);
        }

        public void SetHighlight(bool scanning, bool hovered)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
            float amount = !scanning ? 0f : hovered ? 0.85f : 0.35f + 0.25f * pulse;
            // Вне сканирования не трогаем цвет каждый кадр, чтобы не перебивать вспышку урона
            if (amount == 0f && _lastHighlight == 0f)
            {
                return;
            }

            _lastHighlight = amount;
            for (int i = 0; i < _highlightRenderers.Length; i++)
            {
                if (_highlightRenderers[i] != null)
                {
                    _highlightRenderers[i].color = Color.Lerp(_baseColors[i], _scanColor, amount);
                }
            }
        }
    }
}
