using N89.Combat;
using N89.Hacking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace N89.Player
{
    // Top-down управление: WASD — движение, мышь — прицел, ЛКМ — стрельба.
    // Оружие и здоровье необязательны: дома (в интерлюдиях) герой ходит и целится, но не стреляет.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 6.5f;
        [SerializeField] private float _acceleration = 70f;
        [SerializeField] private Transform _body;
        [SerializeField] private Weapon _weapon;
        [SerializeField] private Health _health;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private SpriteRenderer _bodyRenderer;

        [Header("Recoil")]
        [SerializeField] private float _shakeStrength = 0.18f;
        [SerializeField] private float _shakeDuration = 0.08f;
        [Tooltip("Насколько спрайт откатывается назад при выстреле (в юнитах)")]
        [SerializeField] private float _recoilDistance = 0.12f;
        [SerializeField] private float _recoilRecovery = 1.2f;

        private Rigidbody2D _rb;
        private HackingController _hacking;
        private Camera _camera;
        private Vector2 _moveInput;
        private float _recoil;

        public bool CanMove { get; set; } = true;
        public bool CanShoot { get; set; } = true;
        // Выключается в катсценах, чтобы 89 не крутился за курсором
        public bool CanAim { get; set; } = true;
        // Импланты: замедление (блок), своё движение (рывок) и занятые руки (оружие молчит)
        public float SpeedMultiplier { get; set; } = 1f;
        // Отдельно от SpeedMultiplier (его держит блок «Рук гориллы»): ставит HackingController на время скана
        public float ScanSpeedMultiplier { get; set; } = 1f;
        public bool ExternalVelocity { get; set; }
        public bool WeaponLocked { get; set; }
        public Vector2 AimPoint { get; private set; }
        public Health Health => _health;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _hacking = GetComponent<HackingController>();
            _camera = Camera.main;
            if (_health != null) _health.Died += OnDied;
            if (_weapon != null) _weapon.Fired += OnFired;
        }

        private void Update()
        {
            if (N89.UI.PauseMenu.IsPaused) return;
            if (_health != null && _health.IsDead)
            {
                return;
            }

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;

            _moveInput = Vector2.zero;
            if (kb != null && CanMove)
            {
                if (kb.wKey.isPressed) _moveInput.y += 1f;
                if (kb.sKey.isPressed) _moveInput.y -= 1f;
                if (kb.dKey.isPressed) _moveInput.x += 1f;
                if (kb.aKey.isPressed) _moveInput.x -= 1f;
                _moveInput = Vector2.ClampMagnitude(_moveInput, 1f);
            }

            if (mouse != null && CanAim)
            {
                Vector3 world = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
                AimPoint = world;
                Vector2 aim = AimPoint - (Vector2)transform.position;
                if (aim.sqrMagnitude > 0.01f)
                {
                    float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                    _body.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                bool hackingBlocks = _hacking != null && _hacking.BlocksShooting;
                if (_weapon != null && CanShoot && !WeaponLocked && !hackingBlocks && mouse.leftButton.isPressed)
                {
                    _weapon.TryFire(aim);
                }
            }

            // Откат спрайта назад по линии ствола и плавный возврат
            _recoil = Mathf.MoveTowards(_recoil, 0f, _recoilRecovery * Time.deltaTime);
            _body.localPosition = -(_body.localRotation * Vector3.right) * _recoil;
        }

        private void FixedUpdate()
        {
            if (ExternalVelocity)
            {
                return;
            }

            Vector2 target = _moveInput * _moveSpeed * SpeedMultiplier * ScanSpeedMultiplier;
            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, target, _acceleration * Time.fixedDeltaTime);
        }

        private void OnFired()
        {
            _recoil = _recoilDistance;
            if (_cameraFollow != null)
            {
                _cameraFollow.Shake(_shakeStrength, _shakeDuration);
            }
        }

        private void OnDied(Health health)
        {
            CanMove = false;
            CanShoot = false;
            _moveInput = Vector2.zero;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = new Color(0.55f, 0.15f, 0.2f);
            }

            if (_cameraFollow != null)
            {
                _cameraFollow.Shake(0.5f, 0.35f);
            }
        }
    }
}
