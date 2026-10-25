using System.Collections;
using TMPro;
using UnityEngine;

namespace N89.UI
{
    // Крупная надпись посреди экрана на пару секунд: «ЭТАЖ ЗАЧИЩЕН / ПОДНИМИСЬ НА ВТОРОЙ ЭТАЖ».
    // Выстреливает с увеличением, подзаголовок мигает, потом гаснет. Время реальное.
    public class Banner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _box;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;

        private Coroutine _current;

        private void Awake()
        {
            _group.alpha = 0f;
        }

        public void Show(string title, string subtitle, float duration)
        {
            if (_current != null) StopCoroutine(_current);
            _current = StartCoroutine(Run(title, subtitle, duration));
        }

        private IEnumerator Run(string title, string subtitle, float duration)
        {
            _title.text = title;
            _subtitle.text = subtitle;
            N89.Core.Sfx.Ui(N89.Core.Sfx.Banner, 0.8f);

            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.25f;
                _group.alpha = k;
                float scale = Mathf.Lerp(1.4f, 1f, k);
                _box.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            _group.alpha = 1f;
            _box.localScale = Vector3.one;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _subtitle.alpha = Mathf.Repeat(t * 2.5f, 1f) > 0.3f ? 1f : 0.35f;
                yield return null;
            }

            _subtitle.alpha = 1f;
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                _group.alpha = 1f - t / 0.5f;
                yield return null;
            }

            _group.alpha = 0f;
            _current = null;
        }
    }
}
