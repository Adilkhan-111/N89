using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace N89.UI
{
    // Мессенджер в духе журнала сообщений Cyberpunk 2077: выезжает слева, собеседник "печатает",
    // сообщения появляются по одному (печатаются по буквам), в конце — прикреплённый файл.
    // Enter (или Space, ЛКМ) — сразу показать всё; когда всё показано, повторное нажатие закрывает панель.
    public class MessengerPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _contactLabel;
        [SerializeField] private TMP_Text _topicLabel;
        [Tooltip("Контейнер ленты (VerticalLayoutGroup). Шаблон сообщения и вложение — его неактивные дети")]
        [SerializeField] private RectTransform _thread;
        [SerializeField] private GameObject _bubbleTemplate;
        [SerializeField] private GameObject _attachment;
        [SerializeField] private TMP_Text _attachmentLabel;
        [Tooltip("Фото во вложении (перед файлом); нет фото — не показывается")]
        [SerializeField] private GameObject _photo;
        [SerializeField] private Image _photoImage;
        [SerializeField] private TMP_Text _photoLabel;
        [SerializeField] private TMP_Text _typingLabel;
        [SerializeField] private CanvasGroup _closeHint;
        [SerializeField] private float _charsPerSecond = 45f;
        [SerializeField] private float _typingPause = 1f;
        [SerializeField] private float _slideDuration = 0.35f;

        private void Awake()
        {
            _group.alpha = 0f;
            _bubbleTemplate.SetActive(false);
            _attachment.SetActive(false);
            if (_photo != null) _photo.SetActive(false);
        }

        public IEnumerator Show(string contact, string topic, string[] messages, string attachment,
            Sprite photo = null, string photoCaption = null)
        {
            foreach (Transform child in _thread)
            {
                if (child.gameObject != _bubbleTemplate && child.gameObject != _attachment && child.gameObject != _photo) Destroy(child.gameObject);
            }
            _attachment.SetActive(false);
            if (_photo != null) _photo.SetActive(false);
            _contactLabel.text = contact;
            _topicLabel.text = topic;
            _typingLabel.text = string.Empty;
            _closeHint.alpha = 0f;

            yield return Slide(true);

            bool skip = false;
            foreach (string message in messages)
            {
                // Собеседник "печатает..."
                for (float t = 0f; t < _typingPause && !skip; t += Time.unscaledDeltaTime)
                {
                    int dots = 1 + (int)(Time.unscaledTime * 3f) % 3;
                    _typingLabel.text = $"{contact} печатает{new string('.', dots)}";
                    skip = EnterPressed();
                    yield return null;
                }
                _typingLabel.text = string.Empty;

                GameObject bubble = Instantiate(_bubbleTemplate, _thread);
                bubble.SetActive(true);
                TMP_Text label = bubble.GetComponentInChildren<TMP_Text>();
                label.text = message;
                label.ForceMeshUpdate();
                int total = label.textInfo.characterCount;
                for (float shown = 0f; shown < total && !skip; shown += _charsPerSecond * Time.unscaledDeltaTime)
                {
                    label.maxVisibleCharacters = Mathf.FloorToInt(shown);
                    skip = EnterPressed();
                    yield return null;
                }
                label.maxVisibleCharacters = total;
            }

            if (photo != null && _photo != null)
            {
                if (!skip) yield return new WaitForSecondsRealtime(0.5f);
                _photoImage.sprite = photo;
                if (_photoLabel != null) _photoLabel.text = photoCaption;
                _photo.transform.SetAsLastSibling();
                _photo.SetActive(true);
            }

            if (!string.IsNullOrEmpty(attachment))
            {
                if (!skip) yield return new WaitForSecondsRealtime(0.5f);
                _attachmentLabel.text = attachment;
                _attachment.transform.SetAsLastSibling();
                _attachment.SetActive(true);
            }

            // Кадр паузы, чтобы нажатие "показать всё" не закрыло панель сразу
            yield return null;
            while (!EnterPressed())
            {
                _closeHint.alpha = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
                yield return null;
            }

            yield return Slide(false);
        }

        private IEnumerator Slide(bool open)
        {
            float width = _panel.rect.width;
            for (float t = 0f; t < _slideDuration; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / _slideDuration);
                if (!open) k = 1f - k;
                _panel.anchoredPosition = new Vector2(Mathf.Lerp(-width, 0f, k), 0f);
                // Неоновое "моргание" при появлении
                _group.alpha = k * (Random.value < 0.15f ? 0.6f : 1f);
                yield return null;
            }

            _panel.anchoredPosition = new Vector2(open ? 0f : -width, 0f);
            _group.alpha = open ? 1f : 0f;
        }

        // Дальше / закрыть: Enter, Space или ЛКМ
        private static bool EnterPressed()
        {
            if (N89.UI.PauseMenu.IsPaused) return false;
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }
    }
}
