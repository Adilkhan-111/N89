using N89.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace N89.UI
{
    // Пауза по Esc (в туториале, интерлюдиях и миссиях): время стоит, ввод игры отключён (скрипты проверяют
    // IsPaused), виден системный курсор. Кнопки: «Продолжить» (или снова Esc), «Заново» — перезапуск сцены
    // с последнего чекпоинта, «Главное меню». Всё на нескалированном времени.
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _menuButton;
        [SerializeField] private string _menuScene = "MainMenu";

        public static bool IsPaused { get; private set; }

        private float _timeScale = 1f;
        private bool _cursorVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsPaused = false;
            // Вышли из Play Mode на паузе — звук остался бы выключенным в следующем запуске
            AudioListener.pause = false;
        }

        private void Awake()
        {
            IsPaused = false;
            SetVisible(false);
            _resumeButton.onClick.AddListener(Resume);
            _restartButton.onClick.AddListener(Restart);
            _menuButton.onClick.AddListener(ToMenu);
        }

        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = _timeScale;
            IsPaused = false;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (IsPaused) Resume();
            else Pause();
        }

        private void Pause()
        {
            IsPaused = true;
            _timeScale = Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            _cursorVisible = Cursor.visible;
            Cursor.visible = true;
            SetVisible(true);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_resumeButton.gameObject);
        }

        private void Resume()
        {
            if (!IsPaused) return;
            SetVisible(false);
            Time.timeScale = _timeScale;
            AudioListener.pause = false;
            Cursor.visible = _cursorVisible;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            IsPaused = false;
        }

        // Как после смерти: с последнего чекпоинта (второй этаж, подвал, побег), если он есть
        private void Restart()
        {
            Leave();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void ToMenu()
        {
            Leave();
            Checkpoint.Clear();
            SceneManager.LoadScene(_menuScene);
        }

        private void Leave()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            IsPaused = false;
        }

        private void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }
    }
}
