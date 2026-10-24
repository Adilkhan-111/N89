using System;
using System.Collections.Generic;
using N89.Combat;
using N89.Enemies;
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
        [Tooltip("Человек с имплантами: «Короткое замыкание» не убивает, а оглушает — судороги с помехами (Malfunction), потом идёт к игроку")]
        [SerializeField] private bool _isHuman;
        [Tooltip("Сколько секунд (реального времени) длится оглушение")]
        [SerializeField] private float _malfunctionTime = 2f;
        [Tooltip("Материал для помех и разрядов (Sprite-Unlit-Default — светятся в темноте)")]
        [SerializeField] private Material _glitchMaterial;

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
                    Malfunction malfunction = GetComponent<Malfunction>();
                    return health != null && !health.IsDead && (malfunction == null || !malfunction.IsActive);
                case QuickhackEffect.Disable:
                    SecurityCamera securityCamera = GetComponent<SecurityCamera>();
                    return securityCamera != null && !securityCamera.IsDisabled;
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

                    if (_isHuman && _highlightRenderers.Length > 0)
                    {
                        Malfunction malfunction = GetComponent<Malfunction>();
                        if (malfunction == null)
                        {
                            malfunction = gameObject.AddComponent<Malfunction>();
                        }

                        malfunction.Begin(_malfunctionTime, _highlightRenderers[0], _glitchMaterial, _shortCircuitFxPrefab);
                    }
                    else
                    {
                        GetComponent<Health>()?.TakeDamage(hack.Damage);
                    }
                    break;
                case QuickhackEffect.Disable:
                    if (_shortCircuitFxPrefab != null)
                    {
                        Instantiate(_shortCircuitFxPrefab, transform.position, Quaternion.identity);
                    }
                    GetComponent<SecurityCamera>()?.Disable();
                    break;
            }

            Hacked?.Invoke(this, hack);
        }

        public void SetHighlight(bool scanning, bool hovered)
        {
            // Во время судорог цветом тела управляют помехи
            Malfunction malfunction = _isHuman ? GetComponent<Malfunction>() : null;
            if (malfunction != null && malfunction.IsActive)
            {
                return;
            }

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
