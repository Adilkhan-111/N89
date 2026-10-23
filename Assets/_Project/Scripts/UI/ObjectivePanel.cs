using TMPro;
using UnityEngine;

namespace N89.UI
{
    // Текущая задача в левом верхнем углу. При смене текста коротко мигает.
    public class ObjectivePanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _flashColor = new Color(0.3f, 1f, 1f);

        private float _flashUntil;

        public void Set(string text)
        {
            text ??= string.Empty;
            if (text == _label.text)
            {
                return;
            }

            _label.text = text;
            _flashUntil = Time.unscaledTime + 0.4f;
        }

        private void Update()
        {
            bool visible = !string.IsNullOrEmpty(_label.text);
            _group.alpha = Mathf.MoveTowards(_group.alpha, visible ? 1f : 0f, 5f * Time.unscaledDeltaTime);

            bool flashing = Time.unscaledTime < _flashUntil && Mathf.Repeat(Time.unscaledTime * 12f, 1f) > 0.5f;
            _label.color = flashing ? _flashColor : _normalColor;
        }
    }
}
