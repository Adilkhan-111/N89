using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace N89.UI
{
    // Экран загрузки миссии: чёрный экран с названием миссии, полоской прогресса и процентами.
    // Сцена грузится в фоне; экран держится не меньше _minDuration, чтобы название успели прочитать.
    public class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _percent;
        [SerializeField] private float _minDuration = 2.5f;
        [SerializeField] private string _fallbackScene = "MainMenu";

        private void Awake()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        public IEnumerator Load(string scene, string title, string subtitle)
        {
            _title.text = title;
            _subtitle.text = subtitle;
            _fill.fillAmount = 0f;
            _percent.text = "ЗАГРУЗКА  0%";
            _group.blocksRaycasts = true;

            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                _group.alpha = t / 0.5f;
                yield return null;
            }
            _group.alpha = 1f;

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                scene = _fallbackScene;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(scene);
            operation.allowSceneActivation = false;
            float shown = 0f;
            for (float t = 0f; shown < 1f; t += Time.unscaledDeltaTime)
            {
                // Unity отдаёт прогресс до 0.9, остальное — активация сцены
                float loaded = Mathf.Clamp01(operation.progress / 0.9f);
                shown = Mathf.Min(loaded, t / _minDuration);
                _fill.fillAmount = shown;
                _percent.text = $"ЗАГРУЗКА  {Mathf.RoundToInt(shown * 100f)}%";
                yield return null;
            }

            operation.allowSceneActivation = true;
        }
    }
}
