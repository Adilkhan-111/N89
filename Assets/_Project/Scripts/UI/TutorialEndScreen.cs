using System.Collections;
using N89.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace N89.UI
{
    // Итоговый экран уровня: "ТУТОРИАЛ ОКОНЧЕН", рейтинг/очки и кнопка "ВПЕРЁД".
    // Система очков появится в миссиях — в туториале там написано "не оценено".
    public class TutorialEndScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _continueButton;
        [SerializeField] private GameObject _crosshair;
        [SerializeField] private float _fadeDuration = 1f;
        [Tooltip("Следующая сцена. Если её нет в Build Settings — грузится запасная")]
        [SerializeField] private string _nextScene = "Mission01";
        [SerializeField] private string _fallbackScene = "MainMenu";

        private bool _shown;

        private void Awake()
        {
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
            _continueButton.onClick.AddListener(Continue);
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
            _group.interactable = true;
            _group.blocksRaycasts = true;
            _shown = true;
            EventSystem.current?.SetSelectedGameObject(_continueButton.gameObject);
        }

        private void Update()
        {
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
