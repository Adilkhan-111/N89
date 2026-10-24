using UnityEngine;

namespace N89.UI
{
    // Указатель «куда идти»: стрелка кружит у героя и смотрит на цель, а над самой целью
    // подпрыгивает маркер-стрелка вниз. Когда герой рядом с целью, стрелка у героя прячется.
    public class WaypointArrow : MonoBehaviour
    {
        [SerializeField] private Transform _follow;
        [SerializeField] private SpriteRenderer _arrow;
        [SerializeField] private SpriteRenderer _beacon;
        [SerializeField] private float _radius = 1.1f;
        [SerializeField] private float _hideDistance = 2f;

        private Vector2 _target;
        private bool _active;

        private void Awake()
        {
            Hide();
        }

        public void Show(Vector2 target)
        {
            _target = target;
            _active = true;
            _beacon.enabled = true;
        }

        public void Hide()
        {
            _active = false;
            _arrow.enabled = false;
            _beacon.enabled = false;
        }

        private void LateUpdate()
        {
            if (!_active || _follow == null)
            {
                return;
            }

            float time = Time.unscaledTime;
            float pulse = 0.6f + 0.4f * Mathf.Sin(time * 6f);

            Vector2 from = _follow.position;
            Vector2 delta = _target - from;
            _arrow.enabled = delta.magnitude > _hideDistance;
            if (_arrow.enabled)
            {
                Vector2 dir = delta.normalized;
                // Стрелка чуть «толкается» в сторону цели
                _arrow.transform.position = from + dir * (_radius + 0.12f * Mathf.Sin(time * 8f));
                _arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                _arrow.color = new Color(_arrow.color.r, _arrow.color.g, _arrow.color.b, pulse);
            }

            _beacon.transform.position = _target + new Vector2(0f, 0.9f + 0.2f * Mathf.Abs(Mathf.Sin(time * 4f)));
            _beacon.transform.rotation = Quaternion.Euler(0f, 0f, -90f);
            _beacon.color = new Color(_beacon.color.r, _beacon.color.g, _beacon.color.b, pulse);
        }
    }
}
