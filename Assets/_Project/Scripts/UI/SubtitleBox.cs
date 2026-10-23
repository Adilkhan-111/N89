using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace N89.UI
{
    // Субтитры "голоса": печатаются по буквам, держатся на экране и сами скрываются. Enter — пропустить.
    public class SubtitleBox : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _speakerLabel;
        [SerializeField] private TMP_Text _lineLabel;
        [SerializeField] private float _charsPerSecond = 40f;
        [SerializeField] private float _holdBase = 1.4f;
        [SerializeField] private float _holdPerChar = 0.035f;
        [SerializeField] private float _fadeSpeed = 6f;

        private Coroutine _current;
        private bool _visible;

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
            _visible = true;
            _speakerLabel.text = speaker;
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

        private static bool SkipPressed()
        {
            Keyboard kb = Keyboard.current;
            return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
        }

        private void Update()
        {
            _group.alpha = Mathf.MoveTowards(_group.alpha, _visible ? 1f : 0f, _fadeSpeed * Time.unscaledDeltaTime);
        }
    }
}
