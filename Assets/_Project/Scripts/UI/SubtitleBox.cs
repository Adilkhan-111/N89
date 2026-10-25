using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace N89.UI
{
    // Субтитры "голоса": печатаются по буквам, держатся на экране и сами скрываются. Enter (Space, ЛКМ) — пропустить.
    // У каждого говорящего свой цвет (имя, акцентная полоса, оттенок текста). Когда говорящий меняется,
    // плашка "выстреливает", а имя расшифровывается из случайных символов.
    public class SubtitleBox : MonoBehaviour
    {
        [Serializable]
        private struct SpeakerStyle
        {
            public string Name;
            public Color NameColor;
            public Color TextColor;
        }

        private const string Glyphs = "0123456789#%&*+=/<>";

        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _speakerLabel;
        [SerializeField] private TMP_Text _lineLabel;
        [SerializeField] private Image _accent;
        [SerializeField] private float _charsPerSecond = 40f;
        [SerializeField] private float _holdBase = 1.4f;
        [SerializeField] private float _holdPerChar = 0.035f;
        [SerializeField] private float _fadeSpeed = 6f;

        [Header("Speakers")]
        [SerializeField] private SpeakerStyle[] _speakers =
        {
            new SpeakerStyle { Name = "ОПЕРАТОР", NameColor = new Color(1f, 0.3f, 0.45f), TextColor = new Color(1f, 0.88f, 0.9f) },
            new SpeakerStyle { Name = "89", NameColor = new Color(0.3f, 1f, 1f), TextColor = new Color(0.86f, 1f, 1f) },
        };
        [SerializeField] private Color _defaultNameColor = Color.white;

        private Coroutine _current;
        private bool _visible;
        private string _lastSpeaker;
        private float _popStart = -100f;
        private Color _accentColor = Color.white;

        public bool IsSpeaking => _current != null;

        public Coroutine Say(string speaker, string line)
        {
            if (_current != null)
            {
                StopCoroutine(_current);
            }

            _current = StartCoroutine(Run(speaker, line));
            return _current;
        }

        private IEnumerator Run(string speaker, string line)
        {
            SpeakerStyle style = StyleFor(speaker);
            _speakerLabel.color = style.NameColor;
            _lineLabel.color = style.TextColor;
            _accentColor = style.NameColor;

            // Новый говорящий (или плашка была скрыта) — плашка выстреливает, имя расшифровывается
            bool fresh = !_visible || speaker != _lastSpeaker;
            _lastSpeaker = speaker;
            _visible = true;
            if (fresh)
            {
                _popStart = Time.unscaledTime;
                StartCoroutine(Decode(speaker));
            }
            else
            {
                _speakerLabel.text = speaker;
            }

            _lineLabel.text = line;
            _lineLabel.maxVisibleCharacters = 0;
            _lineLabel.ForceMeshUpdate();
            int total = _lineLabel.textInfo.characterCount;

            float shown = 0f;
            while (shown < total)
            {
                yield return null;
                shown += _charsPerSecond * Time.unscaledDeltaTime;
                if (SkipPressed())
                {
                    shown = total;
                }

                _lineLabel.maxVisibleCharacters = Mathf.FloorToInt(shown);
            }

            float until = Time.unscaledTime + _holdBase + _holdPerChar * total;
            while (Time.unscaledTime < until)
            {
                yield return null;
                if (SkipPressed())
                {
                    break;
                }
            }

            _visible = false;
            _current = null;
        }

        private SpeakerStyle StyleFor(string speaker)
        {
            foreach (SpeakerStyle style in _speakers)
            {
                if (style.Name == speaker) return style;
            }
            return new SpeakerStyle { Name = speaker, NameColor = _defaultNameColor, TextColor = Color.white };
        }

        // Имя проявляется слева направо, пока остальное — случайные символы
        private IEnumerator Decode(string speaker)
        {
            const float duration = 0.25f;
            var chars = new char[speaker.Length];
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                int revealed = Mathf.FloorToInt(speaker.Length * t / duration);
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = i < revealed ? speaker[i] : Glyphs[UnityEngine.Random.Range(0, Glyphs.Length)];
                }
                _speakerLabel.text = new string(chars);
                yield return null;
            }
            _speakerLabel.text = speaker;
        }

        // Пропустить реплику: Enter, Space или ЛКМ
        private static bool SkipPressed()
        {
            if (N89.UI.PauseMenu.IsPaused) return false;
            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                   || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        }

        private void Update()
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, _visible ? 1f : 0f, _fadeSpeed * Time.unscaledDeltaTime);

            // Плашка раскрывается по вертикали с лёгким перелётом
            float pop = Mathf.Clamp01((Time.unscaledTime - _popStart) / 0.22f);
            float overshoot = 1f + Mathf.Sin(pop * Mathf.PI) * 0.06f;
            transform.localScale = new Vector3(1f, Mathf.Lerp(0.6f, 1f, pop) * overshoot, 1f);

            if (_accent != null)
            {
                // Акцентная полоса "мерцает" как неон
                float flicker = 0.75f + 0.25f * Mathf.PerlinNoise(Time.unscaledTime * 6f, 0f);
                _accent.color = new Color(_accentColor.r, _accentColor.g, _accentColor.b, flicker);
            }
        }
    }
}
