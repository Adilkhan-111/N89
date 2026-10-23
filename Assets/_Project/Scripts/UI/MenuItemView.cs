using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace N89.UI
{
    public enum MenuAction
    {
        NewGame,
        Options,
        Exit,
    }

    // Один пункт главного меню. Наведение мышью выбирает пункт, клик — активирует.
    public class MenuItemView : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] private MenuAction _action;
        [SerializeField] private TMP_Text _label;

        private MainMenuController _menu;
        private int _index;
        private float _jitterUntil;
        private Vector2 _labelBasePosition;

        public MenuAction Action => _action;
        public RectTransform Rect => (RectTransform)transform;

        public void Init(MainMenuController menu, int index)
        {
            _menu = menu;
            _index = index;
            _labelBasePosition = _label.rectTransform.anchoredPosition;
        }

        public void SetSelected(bool selected, Color normal, Color highlighted)
        {
            _label.color = selected ? highlighted : normal;
            if (selected)
            {
                _jitterUntil = Time.unscaledTime + 0.12f;
            }
        }

        private void Update()
        {
            // Короткая дрожь текста при выборе — "глитч" как в Katana Zero
            Vector2 jitter = Time.unscaledTime < _jitterUntil ? Random.insideUnitCircle * 3f : Vector2.zero;
            _label.rectTransform.anchoredPosition = _labelBasePosition + jitter;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _menu.Select(_index);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _menu.Select(_index);
            _menu.ActivateSelected();
        }
    }
}
