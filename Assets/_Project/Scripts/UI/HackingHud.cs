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

        private readonly StringBuilder _sb = new StringBuilder();
        private Camera _camera;
        private float _messageUntil;

        public bool RamVisible { get; set; } = true;

        private void Awake()
        {
            _camera = Camera.main;
            _hacking.HackFailed += ShowMessage;
            _message.alpha = 0f;
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            RamPool ram = _hacking.Ram;

            _ramGroup.alpha = Mathf.MoveTowards(_ramGroup.alpha, RamVisible ? 1f : 0f, 4f * dt);
            for (int i = 0; i < _ramSlots.Length; i++)
            {
                float fill = Mathf.Clamp01(ram.Exact - i);
                _ramSlots[i].fillAmount = fill;
                _ramSlots[i].color = fill >= 1f ? _slotFull : _slotCharging;
            }
            _ramLabel.text = $"RAM  {ram.Current}/{ram.Max}";

            _scanOverlay.alpha = Mathf.MoveTowards(_scanOverlay.alpha, _hacking.IsScanning ? 1f : 0f, 8f * dt);

            UpdateHackPanel(ram, dt);
            UpdateUpload(dt);

            if (Time.unscaledTime > _messageUntil)
            {
                _message.alpha = Mathf.MoveTowards(_message.alpha, 0f, 3f * dt);
            }
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
