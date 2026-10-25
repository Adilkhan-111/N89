using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace N89.UI
{
    // Затемнение экрана, цветные вспышки и титры поверх чёрного (например, экран смерти).
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField] private Image _black;
        [SerializeField] private Image _flash;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private float _subtitleCharsPerSecond = 30f;

        private Vector2 _titleBasePosition;

        private void Awake()
        {
            _titleBasePosition = _title.rectTransform.anchoredPosition;
            SetAlpha(_flash, 0f);
            _title.alpha = 0f;
            _subtitle.alpha = 0f;
        }

        public void SetBlack(float alpha)
        {
            SetAlpha(_black, alpha);
        }

        public IEnumerator Fade(float from, float to, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                SetAlpha(_black, Mathf.Lerp(from, to, t / duration));
                yield return null;
            }

            SetAlpha(_black, to);
        }

        public void Flash(Color color, float duration)
        {
            StartCoroutine(FlashRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                color.a = 1f - t / duration;
                _flash.color = color;
                yield return null;
            }

            SetAlpha(_flash, 0f);
        }

        public IEnumerator ShowTitle(string title, string subtitle)
        {
            _title.text = title;
            _title.alpha = 1f;
            _subtitle.text = subtitle;
            _subtitle.alpha = 1f;
            _subtitle.maxVisibleCharacters = 0;
            _subtitle.ForceMeshUpdate();
            int total = _subtitle.textInfo.characterCount;
            N89.Core.Sfx.Ui(N89.Core.Sfx.Stinger);

            yield return new WaitForSecondsRealtime(0.8f);
            for (float shown = 0f; shown < total; shown += _subtitleCharsPerSecond * Time.unscaledDeltaTime)
            {
                int visible = Mathf.FloorToInt(shown);
                if (visible > _subtitle.maxVisibleCharacters && visible > 0) N89.Core.Sfx.Typing(_subtitle.textInfo.characterInfo[visible - 1].character);
                _subtitle.maxVisibleCharacters = visible;
                yield return null;
            }

            _subtitle.maxVisibleCharacters = total;
        }

        public IEnumerator HideTitle(float duration)
        {
            float from = _title.alpha;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float alpha = Mathf.Lerp(from, 0f, t / duration);
                _title.alpha = alpha;
                _subtitle.alpha = alpha;
                yield return null;
            }

            _title.alpha = 0f;
            _subtitle.alpha = 0f;
        }

        private void Update()
        {
            // Лёгкий глитч заголовка, пока он на экране
            if (_title.alpha > 0f)
            {
                bool glitch = Random.value < 0.08f;
                _title.rectTransform.anchoredPosition = _titleBasePosition + (glitch ? Random.insideUnitCircle * 8f : Vector2.zero);
            }
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color c = graphic.color;
            c.a = alpha;
            graphic.color = c;
        }
    }
}
