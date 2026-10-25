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

        // Переписка с ответами героя (fromHero) и выбором в конце (choices; W/S или колесо — выбрать,
        // Enter/Space/ЛКМ или цифра — подтвердить). Выбранный вариант — в Choice. Старые сообщения
        // уходят сверху, когда лента переполнена
        public int Choice { get; private set; }

        public IEnumerator ShowDialog(string contact, string topic, string hero, string[] lines, bool[] fromHero, string[] choices)
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
            Choice = 0;

            yield return Slide(true);

            for (int i = 0; i < lines.Length; i++)
            {
                bool isHero = fromHero != null && i < fromHero.Length && fromHero[i];
                string who = isHero ? hero : contact;
                for (float t = 0f; t < _typingPause; t += Time.unscaledDeltaTime)
                {
                    int dots = 1 + (int)(Time.unscaledTime * 3f) % 3;
                    _typingLabel.text = $"{who} печатает{new string('.', dots)}";
                    if (EnterPressed()) break;
                    yield return null;
                }
                _typingLabel.text = string.Empty;

                TMP_Text label = AddBubble(isHero ? $"<color=#4DFFF2>{hero}:</color> {lines[i]}" : lines[i],
                    isHero ? new Color(0.02f, 0.08f, 0.1f, 1f) : (Color?)null, isHero ? new Color(0.3f, 1f, 1f) : (Color?)null);
                label.ForceMeshUpdate();
                int total = label.textInfo.characterCount;
                yield return null;
                for (float shown = 0f; shown < total; shown += _charsPerSecond * Time.unscaledDeltaTime)
                {
                    label.maxVisibleCharacters = Mathf.FloorToInt(shown);
                    if (EnterPressed()) break;
                    yield return null;
                }
                label.maxVisibleCharacters = total;
                yield return new WaitForSecondsRealtime(0.35f);
            }

            if (choices == null || choices.Length == 0)
            {
                yield return null;
                while (!EnterPressed()) yield return null;
                yield return Slide(false);
                yield break;
            }

            // Варианты ответа — отдельные пузыри, выбранный подсвечен жёлтым
            var options = new TMP_Text[choices.Length];
            var frames = new Image[choices.Length];
            for (int i = 0; i < choices.Length; i++)
            {
                options[i] = AddBubble(string.Empty, null, null);
                frames[i] = options[i].GetComponentInParent<Image>();
            }

            int selected = 0;
            yield return null;
            while (true)
            {
                Keyboard kb = Keyboard.current;
                Mouse mouse = Mouse.current;
                if (kb != null)
                {
                    if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) selected = (selected + choices.Length - 1) % choices.Length;
                    if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) selected = (selected + 1) % choices.Length;
                    if (kb.digit1Key.wasPressedThisFrame) { selected = 0; break; }
                    if (kb.digit2Key.wasPressedThisFrame && choices.Length > 1) { selected = 1; break; }
                }
                if (mouse != null && Mathf.Abs(mouse.scroll.ReadValue().y) > 0.01f) selected = (selected + 1) % choices.Length;
                if (EnterPressed()) break;

                for (int i = 0; i < choices.Length; i++)
                {
                    bool on = i == selected;
                    options[i].text = on ? $"<color=#FFE04D>> {i + 1}. {choices[i]}</color>" : $"  {i + 1}. {choices[i]}";
                    frames[i].color = on ? new Color(0.22f, 0.16f, 0.02f, 1f) : new Color(0.06f, 0.05f, 0.05f, 1f);
                }
                _closeHint.alpha = 0f;
                yield return null;
            }

            Choice = selected;
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Slide(false);
        }

        // Новый пузырь в ленте (цвет фона и рамки — по желанию); переполнена — самый старый уходит
        private TMP_Text AddBubble(string text, Color? background, Color? outline)
        {
            GameObject bubble = Instantiate(_bubbleTemplate, _thread);
            bubble.SetActive(true);
            if (background.HasValue) bubble.GetComponent<Image>().color = background.Value;
            if (outline.HasValue && bubble.TryGetComponent(out Outline frame)) frame.effectColor = outline.Value;
            TMP_Text label = bubble.GetComponentInChildren<TMP_Text>();
            label.text = text;

            LayoutRebuilder.ForceRebuildLayoutImmediate(_thread);
            while (LayoutUtility.GetPreferredHeight(_thread) > _thread.rect.height)
            {
                GameObject oldest = null;
                foreach (Transform child in _thread)
                {
                    if (child.gameObject.activeSelf && child.gameObject != bubble && child.gameObject != _bubbleTemplate) { oldest = child.gameObject; break; }
                }
                if (oldest == null) break;
                oldest.SetActive(false);
                Destroy(oldest);
                LayoutRebuilder.ForceRebuildLayoutImmediate(_thread);
            }
            return label;
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
