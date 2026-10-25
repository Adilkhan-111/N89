using N89.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace N89.UI
{
    // Главное меню: навигация клавиатурой/геймпадом/мышью, плашка выбора и интро-анимация.
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private MenuItemView[] _items;
        [SerializeField] private RectTransform _highlight;
        [SerializeField] private Color _normalColor = new Color(0.85f, 0.82f, 0.9f, 0.85f);
        [SerializeField] private Color _selectedColor = Color.white;
        [SerializeField] private float _highlightSpeed = 20f;

        [Header("Intro")]
        [SerializeField] private RectTransform _titleGroup;
        [SerializeField] private CanvasGroup _titleCanvasGroup;
        [SerializeField] private CanvasGroup _menuCanvasGroup;
        [SerializeField] private float _titleIntroDuration = 0.5f;
        [SerializeField] private float _menuFadeDelay = 0.35f;
        [SerializeField] private float _menuFadeDuration = 0.4f;

        [Header("Scenes")]
        [SerializeField] private string _newGameScene = "Tutorial";

        private int _selected;
        private float _startTime;

        private void Start()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                _items[i].Init(this, i);
            }

            _startTime = Time.unscaledTime;
            Select(0);
            _highlight.anchoredPosition = new Vector2(_highlight.anchoredPosition.x, _items[0].Rect.anchoredPosition.y);
        }

        private void Update()
        {
            UpdateIntro();
            HandleInput();

            float targetY = _items[_selected].Rect.anchoredPosition.y;
            Vector2 pos = _highlight.anchoredPosition;
            pos.y = Mathf.Lerp(pos.y, targetY, 1f - Mathf.Exp(-_highlightSpeed * Time.unscaledDeltaTime));
            _highlight.anchoredPosition = pos;
        }

        private void UpdateIntro()
        {
            float elapsed = Time.unscaledTime - _startTime;

            // Заголовок "влетает" с большим масштабом и слегка перелетает (ease-out-back)
            float t = Mathf.Clamp01(elapsed / _titleIntroDuration);
            float back = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            float scale = Mathf.LerpUnclamped(2.2f, 1f, back);
            _titleGroup.localScale = new Vector3(scale, scale, 1f);
            _titleCanvasGroup.alpha = t;

            _menuCanvasGroup.alpha = Mathf.Clamp01((elapsed - _menuFadeDelay) / _menuFadeDuration);
            _menuCanvasGroup.interactable = _menuCanvasGroup.alpha > 0.5f;
        }

        private void HandleInput()
        {
            if (!_menuCanvasGroup.interactable)
            {
                return;
            }

            Keyboard kb = Keyboard.current;
            Gamepad pad = Gamepad.current;

            bool up = (kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame))
                      || (pad != null && pad.dpad.up.wasPressedThisFrame);
            bool down = (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame))
                        || (pad != null && pad.dpad.down.wasPressedThisFrame);
            bool submit = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                          || (pad != null && pad.buttonSouth.wasPressedThisFrame);

            if (up)
            {
                Select((_selected - 1 + _items.Length) % _items.Length);
            }
            else if (down)
            {
                Select((_selected + 1) % _items.Length);
            }
            else if (submit)
            {
                ActivateSelected();
            }
        }

        public void Select(int index)
        {
            if (index != _selected) N89.Core.Sfx.Ui(N89.Core.Sfx.UiMove);
            _selected = index;
            for (int i = 0; i < _items.Length; i++)
            {
                _items[i].SetSelected(i == index, _normalColor, _selectedColor);
            }
        }

        public void ActivateSelected()
        {
            if (!_menuCanvasGroup.interactable)
            {
                return;
            }

            N89.Core.Sfx.Ui(N89.Core.Sfx.UiSelect);
            switch (_items[_selected].Action)
            {
                case MenuAction.NewGame:
                    if (Application.CanStreamedLevelBeLoaded(_newGameScene))
                    {
                        Checkpoint.Clear();
                        SceneManager.LoadScene(_newGameScene);
                    }
                    else
                    {
                        Debug.LogWarning($"Сцена '{_newGameScene}' не добавлена в Build Settings.");
                    }
                    break;
                case MenuAction.Options:
                    Debug.Log("Настройки пока не готовы.");
                    break;
                case MenuAction.Exit:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }
    }
}
