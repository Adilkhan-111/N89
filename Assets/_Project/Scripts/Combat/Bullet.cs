using System.Collections.Generic;
using UnityEngine;

namespace N89.Combat
{
    // Пуля летит рейкастами, чтобы не пролетать сквозь тонкие стены на большой скорости.
    public class Bullet : MonoBehaviour
    {
        private static readonly List<Bullet> s_active = new List<Bullet>();

        [SerializeField] private float _speed = 30f;
        [SerializeField] private float _lifetime = 2f;
        [SerializeField] private int _damage = 1;
        [SerializeField] private GameObject _impactFxPrefab;

        private Vector2 _direction;
        private LayerMask _hitMask;
        private Transform _owner;
        private float _dieAt;

        // Все летящие пули — для «Blade hands», которые их отбивают
        public static IReadOnlyList<Bullet> Active => s_active;
        public Transform Owner => _owner;

        private void OnEnable() => s_active.Add(this);
        private void OnDisable() => s_active.Remove(this);

        // Отбили: летит в новую сторону, теперь — пуля нового владельца (бьёт по его целям), срок жизни заново
        public void Deflect(Vector2 direction, LayerMask hitMask, Transform owner, Color tint)
        {
            Launch(direction, hitMask, owner);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            if (TryGetComponent(out SpriteRenderer sr)) sr.color = tint;
        }

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

                if (hit.collider.isTrigger || hit.collider.GetComponent<ShootThrough>() != null)
                {
                    continue;
                }

                Health health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    health.TakeDamage(_damage);
                }
                else
                {
                    N89.Core.Sfx.Play(N89.Core.Sfx.Impact, hit.point, 0.6f);
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
