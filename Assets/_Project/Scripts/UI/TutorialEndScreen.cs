using System.Collections;
using N89.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace N89.UI
{
    // Итоговый экран уровня: заголовок, набегающий счёт, убийства и лучшая серия, ранг S–D
    // (из ScoreCounter) и кнопка "ВПЕРЁД".
    public class TutorialEndScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _continueButton;
        [SerializeField] private GameObject _crosshair;
        [SerializeField] private float _fadeDuration = 1f;
        [Tooltip("Следующая сцена. Если её нет в Build Settings — грузится запасная")]
        [SerializeField] private string _nextScene = "Mission01";
        [SerializeField] private string _fallbackScene = "MainMenu";

        [Header("Очки и ранг (ScoreCounter)")]
        [SerializeField] private TMP_Text _scoreLabel;
        [SerializeField] private TMP_Text _scoreShadow;
        [SerializeField] private TMP_Text _statsLabel;
        [SerializeField] private TMP_Text _rankLabel;
        [SerializeField] private TMP_Text _rankShadow;
        [SerializeField] private CanvasGroup _rankGroup;

        private bool _shown;

        private void Awake()
        {
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _continueButton.onClick.AddListener(Continue);
            if (_rankGroup != null) _rankGroup.alpha = 0f;
        }

        public IEnumerator Show()
        {
            if (_crosshair != null)
            {
                // Перекрестие прячем — возвращается обычный курсор для кнопки
                _crosshair.SetActive(false);
            }
            Cursor.visible = true;

            for (float t = 0f; t < _fadeDuration; t += Time.unscaledDeltaTime)
            {
                _group.alpha = t / _fadeDuration;
                yield return null;
            }

            _group.alpha = 1f;
            yield return ShowScore();
            _group.interactable = true;
            _group.blocksRaycasts = true;
            _shown = true;
            EventSystem.current?.SetSelectedGameObject(_continueButton.gameObject);
        }

        private void SetScore(int value)
        {
            _scoreLabel.text = ScoreCounter.Format(value);
            if (_scoreShadow != null) _scoreShadow.text = _scoreLabel.text;
        }

        // Счёт набегает, ниже статистика, затем ранг с размаху «впечатывается» в экран
        private IEnumerator ShowScore()
        {
            ScoreCounter score = ScoreCounter.Instance;
            if (score == null || _scoreLabel == null) yield break;
            score.Finish();
            score.Visible = false;

            _statsLabel.text = $"KILLS  {score.Kills}/{score.TotalEnemies}     MAX COMBO  {score.MaxCombo}x";
            for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / 1.2f, 3f);
                SetScore(Mathf.RoundToInt(score.Score * k));
                if (score.Score > 0) N89.Core.Sfx.Ui(N89.Core.Sfx.ScoreTick, 1f, 1f + 0.5f * k);
                yield return null;
            }
            SetScore(score.Score);
            yield return new WaitForSecondsRealtime(0.35f);

            string rank = score.Rank;
            Color color = ScoreCounter.RankColor(rank);
            _rankLabel.text = rank;
            _rankShadow.text = rank;
            _rankLabel.color = color;
            Transform rankRoot = _rankGroup.transform;
            for (float t = 0f; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.22f;
                _rankGroup.alpha = k;
                rankRoot.localScale = Vector3.one * Mathf.Lerp(3.2f, 1f, k * k);
                rankRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-30f, -8f, k));
                yield return null;
            }
            _rankGroup.alpha = 1f;
            rankRoot.localScale = Vector3.one;
            rankRoot.localRotation = Quaternion.Euler(0f, 0f, -8f);
            // Удар: короткая тряска печати
            N89.Core.Sfx.Ui(N89.Core.Sfx.RankStamp);
            Vector3 home = rankRoot.localPosition;
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                rankRoot.localPosition = home + (Vector3)(Random.insideUnitCircle * 14f * (1f - t / 0.25f));
                yield return null;
            }
            rankRoot.localPosition = home;
        }

        private void Update()
        {
            if (N89.UI.PauseMenu.IsPaused) return;
            Keyboard kb = Keyboard.current;
            if (_shown && kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            {
                Continue();
            }
        }

        private void Continue()
        {
            if (!_shown)
            {
                return;
            }

            _shown = false;
            Checkpoint.Clear();
            Time.timeScale = 1f;
            string scene = Application.CanStreamedLevelBeLoaded(_nextScene) ? _nextScene : _fallbackScene;
            SceneManager.LoadScene(scene);
        }
    }
}
