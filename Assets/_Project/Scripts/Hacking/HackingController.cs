using System;
using N89.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace N89.Hacking
{
    // Режим сканирования (зажать TAB): время замедляется, техника подсвечивается,
    // наведи курсор и нажми 1–3, чтобы загрузить взлом. Взлом тратит RAM и грузится какое-то время.
    public class HackingController : MonoBehaviour
    {
        [SerializeField] private RamPool _ram;
        [SerializeField] private Health _health;
        [SerializeField] private float _range = 16f;
        [SerializeField] private float _hoverRadius = 0.8f;
        [SerializeField] private float _scanTimeScale = 0.25f;
        [Tooltip("Что закрывает обзор для взлома. Стекло сюда не входит — сквозь него взламывать можно")]
        [SerializeField] private LayerMask _sightBlockers;

        private Camera _camera;
        private float _defaultFixedDeltaTime;

        public bool HackingEnabled { get; set; } = true;
        public bool IsScanning { get; private set; }
        // Скан сорван выстрелом: пока TAB не отпущен, сканирование не включится и стрелять нельзя
        public bool ScanLocked { get; private set; }
        public bool BlocksShooting => IsScanning || ScanLocked;
        public HackableDevice Hovered { get; private set; }
        public HackableDevice UploadTarget { get; private set; }
        public QuickhackData UploadHack { get; private set; }
        public float UploadProgress { get; private set; }
        public RamPool Ram => _ram;

        public event Action<string> HackFailed;
        public event Action<HackableDevice, QuickhackData> HackCompleted;
        public event Action<string> ScanAborted;

        private void Awake()
        {
            _camera = Camera.main;
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void OnDisable()
        {
            SetScanning(false);
        }

        private void Update()
        {
            if (N89.UI.PauseMenu.IsPaused) return;
            Keyboard kb = Keyboard.current;
            bool tabHeld = kb != null && kb.tabKey.isPressed;
            if (!tabHeld)
            {
                ScanLocked = false;
            }

            bool wantScan = HackingEnabled && !_health.IsDead && tabHeld && !ScanLocked;
            if (wantScan != IsScanning)
            {
                SetScanning(wantScan);
            }

            // Замедление — не для перестрелок: попытка выстрелить срывает скан и текущий взлом
            if (IsScanning && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                AbortScan();
            }

            Hovered = IsScanning ? FindHovered() : null;

            foreach (HackableDevice device in HackableDevice.All)
            {
                device.SetHighlight(IsScanning && device.HasAnyAvailable(), device == Hovered);
            }

            if (Hovered != null && kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) TryStartHack(Hovered, 0);
                else if (kb.digit2Key.wasPressedThisFrame) TryStartHack(Hovered, 1);
                else if (kb.digit3Key.wasPressedThisFrame) TryStartHack(Hovered, 2);
            }

            UpdateUpload();
        }

        private void SetScanning(bool scanning)
        {
            IsScanning = scanning;
            Time.timeScale = scanning ? _scanTimeScale : 1f;
            Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;
        }

        private void AbortScan()
        {
            ScanLocked = true;
            SetScanning(false);

            // RAM за сорванный взлом не возвращается
            UploadTarget = null;
            UploadHack = null;
            UploadProgress = 0f;

            ScanAborted?.Invoke("НЕЛЬЗЯ СТРЕЛЯТЬ ВО ВРЕМЯ ВЗЛОМА");
        }

        private HackableDevice FindHovered()
        {
            if (Mouse.current == null)
            {
                return null;
            }

            Vector2 mouseWorld = _camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            HackableDevice best = null;
            float bestDistance = _hoverRadius;

            foreach (HackableDevice device in HackableDevice.All)
            {
                if (device.Collider == null || !device.HasAnyAvailable())
                {
                    continue;
                }

                float distance = Vector2.Distance(device.Collider.ClosestPoint(mouseWorld), mouseWorld);
                if (distance <= bestDistance && IsReachable(device))
                {
                    best = device;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public bool IsReachable(HackableDevice device)
        {
            Vector2 from = transform.position;
            Vector2 to = device.transform.position;
            if (Vector2.Distance(from, to) > _range)
            {
                return false;
            }

            // Первое препятствие на линии должно быть самим устройством (например, дверь в стене)
            RaycastHit2D[] hits = Physics2D.LinecastAll(from, to, _sightBlockers);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.isTrigger)
                {
                    continue;
                }

                return hit.collider.GetComponentInParent<HackableDevice>() == device;
            }

            return true;
        }

        public bool TryStartHack(HackableDevice device, int index)
        {
            if (index >= device.Quickhacks.Count || UploadTarget != null)
            {
                return false;
            }

            QuickhackData hack = device.Quickhacks[index];
            if (!device.IsAvailable(hack))
            {
                return false;
            }

            if (!_ram.TrySpend(hack.RamCost))
            {
                HackFailed?.Invoke("НЕДОСТАТОЧНО ПАМЯТИ");
                return false;
            }

            UploadTarget = device;
            UploadHack = hack;
            UploadProgress = 0f;
            return true;
        }

        private void UpdateUpload()
        {
            if (UploadTarget == null)
            {
                UploadHack = null;
                return;
            }

            // Загрузка идёт в реальном времени, чтобы в замедлении она не тянулась вечность
            UploadProgress += Time.unscaledDeltaTime / Mathf.Max(UploadHack.UploadTime, 0.01f);
            if (UploadProgress < 1f)
            {
                return;
            }

            HackableDevice target = UploadTarget;
            QuickhackData hack = UploadHack;
            UploadTarget = null;
            UploadHack = null;
            UploadProgress = 0f;

            target.Apply(hack);
            HackCompleted?.Invoke(target, hack);
        }
    }
}
