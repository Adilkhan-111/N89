using N89.Combat;
using N89.Hacking;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace N89.UI
{
    // Перекрестие вместо системного курсора: следует за мышью, "подпрыгивает" при выстреле,
    // в режиме сканирования окрашивается в цвет взлома.
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] private RectTransform _canvasRect;
        [SerializeField] private Image _image;
        [SerializeField] private Weapon _weapon;
        [SerializeField] private HackingController _hacking;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _scanColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private float _kickScale = 1.5f;
        [SerializeField] private float _kickRecovery = 8f;

        private float _scale = 1f;

        private void OnEnable()
        {
            Cursor.visible = false;
            if (_weapon != null) _weapon.Fired += OnFired;
        }

        private void OnDisable()
        {
            Cursor.visible = true;
            if (_weapon != null) _weapon.Fired -= OnFired;
        }

        private void OnFired()
        {
            _scale = _kickScale;
        }

        private void LateUpdate()
        {
            if (Mouse.current == null)
            {
                return;
            }

            Vector2 screen = Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screen, null, out Vector2 local);
            _image.rectTransform.anchoredPosition = local;

            _scale = Mathf.Lerp(_scale, 1f, 1f - Mathf.Exp(-_kickRecovery * Time.unscaledDeltaTime));
            _image.rectTransform.localScale = new Vector3(_scale, _scale, 1f);
            _image.color = _hacking != null && _hacking.IsScanning ? _scanColor : _normalColor;
        }
    }
}
