using System.Text;
using N89.Hacking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace N89.UI
{
    // HUD взлома: шкала RAM, затемнение в режиме сканирования, список взломов у цели и полоса загрузки.
    public class HackingHud : MonoBehaviour
    {
        [SerializeField] private HackingController _hacking;
        [SerializeField] private RectTransform _canvasRect;

        [Header("RAM")]
        [SerializeField] private CanvasGroup _ramGroup;
        [SerializeField] private Image[] _ramSlots;
        [SerializeField] private TMP_Text _ramLabel;
        [SerializeField] private Color _slotFull = new Color(0.25f, 1f, 1f);
        [SerializeField] private Color _slotCharging = new Color(0.25f, 1f, 1f, 0.35f);
        [Tooltip("Цвет заполненных слотов, когда памяти мало")]
        [SerializeField] private Color _slotLow = new Color(1f, 0.3f, 0.4f);
        [Tooltip("Слоты, которые съест взлом цели под курсором, мигают этим цветом")]
        [SerializeField] private Color _slotPreview = new Color(1f, 0.85f, 0.3f);
        [Tooltip("Меньше этого — шкала краснеет")]
        [SerializeField] private int _lowRam = 4;

        [Header("Scan")]
        [SerializeField] private CanvasGroup _scanOverlay;
        [SerializeField] private RectTransform _hackPanel;
        [SerializeField] private CanvasGroup _hackPanelGroup;
        [SerializeField] private TMP_Text _hackTitle;
        [SerializeField] private TMP_Text _hackList;

        [Header("Upload")]
        [SerializeField] private RectTransform _uploadPanel;
        [SerializeField] private CanvasGroup _uploadGroup;
        [SerializeField] private Image _uploadFill;
        [SerializeField] private TMP_Text _uploadLabel;

        [SerializeField] private TMP_Text _message;

        [Header("Warning")]
        [Tooltip("Окно «нельзя стрелять во время взлома»")]
        [SerializeField] private RectTransform _warningPanel;
        [SerializeField] private CanvasGroup _warningGroup;
        [SerializeField] private TMP_Text _warningText;
        [SerializeField] private float _warningDuration = 1.6f;

        private readonly StringBuilder _sb = new StringBuilder();
        private Camera _camera;
        private float _messageUntil;
        private float _warningShownAt = -100f;
        private Vector2 _warningHome;

        // Анимация шкалы RAM
        private RectTransform _ramPanel;
        private Vector2 _ramHome;
        private float[] _slotFlash;
        private bool[] _slotSpent;
        private int _lastRam = -1;
        private float _kick;
        private float _failFlash;

        public bool RamVisible { get; set; } = true;

        private void Awake()
        {
            _camera = Camera.main;
            _ramPanel = (RectTransform)_ramGroup.transform;
            _ramHome = _ramPanel.anchoredPosition;
            _slotFlash = new float[_ramSlots.Length];
            _slotSpent = new bool[_ramSlots.Length];
            _hacking.HackFailed += ShowMessage;
            _hacking.HackFailed += _ => { _kick = 1f; _failFlash = 1f; };
            _hacking.ScanAborted += ShowWarning;
            _message.alpha = 0f;
            if (_warningPanel != null)
            {
                _warningHome = _warningPanel.anchoredPosition;
                _warningGroup.alpha = 0f;
            }
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            RamPool ram = _hacking.Ram;

            UpdateRam(ram, dt);

            _scanOverlay.alpha = Mathf.MoveTowards(_scanOverlay.alpha, _hacking.IsScanning ? 1f : 0f, 8f * dt);

            UpdateHackPanel(ram, dt);
            UpdateUpload(dt);

            if (Time.unscaledTime > _messageUntil)
            {
                _message.alpha = Mathf.MoveTowards(_message.alpha, 0f, 3f * dt);
            }

            UpdateWarning(dt);
        }

        private void UpdateWarning(float dt)
        {
            if (_warningPanel == null)
            {
                return;
            }

            float age = Time.unscaledTime - _warningShownAt;
            if (age > _warningDuration)
            {
                _warningGroup.alpha = Mathf.MoveTowards(_warningGroup.alpha, 0f, 4f * dt);
            }

            // Первые доли секунды окно трясётся — как сбой системы
            Vector2 shake = age < 0.25f ? Random.insideUnitCircle * 10f * (1f - age / 0.25f) : Vector2.zero;
            _warningPanel.anchoredPosition = _warningHome + shake;
        }

        private void ShowWarning(string text)
        {
            if (_warningPanel == null)
            {
                ShowMessage(text);
                return;
            }

            _warningText.text = text;
            _warningGroup.alpha = 1f;
            _warningShownAt = Time.unscaledTime;
        }

        // Шкала RAM: потраченные слоты вспыхивают и гаснут, панель вздрагивает; заряженный слот
        // подпрыгивает; заряжающийся мерцает; стоимость взлома под курсором мигает заранее
        private void UpdateRam(RamPool ram, float dt)
        {
            _ramGroup.alpha = Mathf.MoveTowards(_ramGroup.alpha, RamVisible ? 1f : 0f, 4f * dt);

            int current = ram.Current;
            if (_lastRam >= 0 && current != _lastRam)
            {
                bool spent = current < _lastRam;
                for (int i = Mathf.Min(current, _lastRam); i < Mathf.Max(current, _lastRam) && i < _ramSlots.Length; i++)
                {
                    _slotFlash[i] = 1f;
                    _slotSpent[i] = spent;
                }
                if (spent) _kick = Mathf.Max(_kick, 0.6f);
            }
            _lastRam = current;

            int preview = PreviewCost();
            bool low = current < _lowRam;
            float time = Time.unscaledTime;
            float blink = 0.5f + 0.5f * Mathf.Sin(time * 14f);
            _failFlash = Mathf.MoveTowards(_failFlash, 0f, 2.5f * dt);

            for (int i = 0; i < _ramSlots.Length; i++)
            {
                Image slot = _ramSlots[i];
                float fill = Mathf.Clamp01(ram.Exact - i);
                Color color;
                if (fill >= 1f)
                {
                    color = low ? _slotLow : _slotFull;
                    if (preview > 0 && i >= current - preview && i < current)
                    {
                        color = Color.Lerp(color, _slotPreview, blink);
                    }
                }
                else
                {
                    // Заряжающийся слот переливается
                    float shimmer = 0.6f + 0.4f * Mathf.Sin(time * 8f + i * 0.7f);
                    color = new Color(_slotCharging.r, _slotCharging.g, _slotCharging.b, _slotCharging.a * shimmer);
                }

                _slotFlash[i] = Mathf.MoveTowards(_slotFlash[i], 0f, 3.5f * dt);
                float flash = _slotFlash[i];
                if (flash > 0f && _slotSpent[i])
                {
                    // Потраченный слот: белая вспышка, гаснущая в ноль
                    slot.fillAmount = 1f;
                    color = new Color(1f, 1f, 1f, flash);
                }
                else
                {
                    slot.fillAmount = fill;
                    color = Color.Lerp(color, Color.white, flash);
                }

                color = Color.Lerp(color, _slotLow, _failFlash);
                slot.color = color;
                slot.transform.parent.localScale = new Vector3(1f, 1f + 0.5f * flash, 1f);
            }

            _ramLabel.text = preview > 0 && current >= preview
                ? $"RAM  {current}/{ram.Max}  <color=#FFD84A>-{preview}</color>"
                : $"RAM  {current}/{ram.Max}";
            Color labelColor = low ? Color.Lerp(_slotLow, Color.white, blink * 0.4f) : _slotFull;
            _ramLabel.color = Color.Lerp(labelColor, _slotLow, _failFlash);
            _ramLabel.transform.localScale = Vector3.one * (1f + 0.15f * _kick);

            // Встряска панели
            _kick = Mathf.MoveTowards(_kick, 0f, 3f * dt);
            _ramPanel.anchoredPosition = _ramHome + Random.insideUnitCircle * 7f * _kick;
        }

        // Сколько RAM съест первый доступный взлом цели под курсором
        private int PreviewCost()
        {
            HackableDevice device = _hacking.Hovered;
            if (device == null || _hacking.UploadTarget != null)
            {
                return 0;
            }

            foreach (QuickhackData hack in device.Quickhacks)
            {
                if (hack != null && device.IsAvailable(hack)) return hack.RamCost;
            }
            return 0;
        }

        private void UpdateHackPanel(RamPool ram, float dt)
        {
            HackableDevice device = _hacking.Hovered;
            _hackPanelGroup.alpha = Mathf.MoveTowards(_hackPanelGroup.alpha, device != null ? 1f : 0f, 12f * dt);
            if (device == null)
            {
                return;
            }

            _hackPanel.anchoredPosition = WorldToCanvas(device.transform.position) + new Vector2(70f, 30f);
            _hackTitle.text = device.DeviceName;

            _sb.Clear();
            for (int i = 0; i < device.Quickhacks.Count; i++)
            {
                QuickhackData hack = device.Quickhacks[i];
                if (hack == null)
                {
                    continue;
                }

                bool usable = device.IsAvailable(hack) && ram.Current >= hack.RamCost;
                string color = usable ? "#FFFFFF" : "#6E6E80";
                _sb.Append($"<color=#FFD84A>[{i + 1}]</color> <color={color}>{hack.DisplayName}</color>");
                _sb.Append($"  <color=#3FF2FF>{hack.RamCost} RAM</color>\n");
            }
            _hackList.text = _sb.ToString();
        }

        private void UpdateUpload(float dt)
        {
            HackableDevice target = _hacking.UploadTarget;
            _uploadGroup.alpha = Mathf.MoveTowards(_uploadGroup.alpha, target != null ? 1f : 0f, 10f * dt);
            if (target == null)
            {
                return;
            }

            _uploadPanel.anchoredPosition = WorldToCanvas(target.transform.position) + new Vector2(0f, -70f);
            _uploadFill.fillAmount = _hacking.UploadProgress;
            _uploadLabel.text = $"ЗАГРУЗКА: {_hacking.UploadHack.DisplayName}";
        }

        private void ShowMessage(string text)
        {
            _message.text = text;
            _message.alpha = 1f;
            _messageUntil = Time.unscaledTime + 1.2f;
        }

        private Vector2 WorldToCanvas(Vector3 world)
        {
            Vector2 screen = _camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out Vector2 local);
            return local;
        }
    }
}
