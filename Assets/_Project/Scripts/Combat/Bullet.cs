using UnityEngine;

namespace N89.Combat
{
    // Пуля летит рейкастами, чтобы не пролетать сквозь тонкие стены на большой скорости.
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float _speed = 30f;
        [SerializeField] private float _lifetime = 2f;
        [SerializeField] private int _damage = 1;
        [SerializeField] private GameObject _impactFxPrefab;

        private Vector2 _direction;
        private LayerMask _hitMask;
        private Transform _owner;
        private float _dieAt;

        // range > 0 — пуля пролетает не дальше range юнитов (дробь, ПП), иначе живёт _lifetime секунд
        public void Launch(Vector2 direction, LayerMask hitMask, Transform owner, float range = 0f)
        {
            _direction = direction.normalized;
            _hitMask = hitMask;
            _owner = owner;
            _dieAt = Time.time + (range > 0f ? range / _speed : _lifetime);
        }

        private void Update()
        {
            if (Time.time >= _dieAt)
            {
                Destroy(gameObject);
                return;
            }

            float step = _speed * Time.deltaTime;
            Vector2 position = transform.position;
            RaycastHit2D[] hits = Physics2D.RaycastAll(position, _direction, step, _hitMask);
            foreach (RaycastHit2D hit in hits)
            {
                if (_owner != null && hit.collider.transform.IsChildOf(_owner))
                {
                    continue;
                }

                if (hit.collider.isTrigger)
                {
                    continue;
                }

                Health health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.TakeDamage(_damage);
                }

                if (_impactFxPrefab != null)
                {
                    Instantiate(_impactFxPrefab, hit.point, Quaternion.identity);
                }

                Destroy(gameObject);
                return;
            }

            transform.position = position + _direction * step;
        }
    }
}
