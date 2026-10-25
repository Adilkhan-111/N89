using System.Collections.Generic;
using N89.World;
using UnityEngine;

namespace N89.Combat
{
    // Ракета босса: летит по прямой (её траекторию заранее показывает пунктир), рвётся о первое препятствие.
    // Прямое попадание в несущую колонну (Pillar) рушит её. Взрыв убивает героя в радиусе и подрывает
    // всё, у чего есть Health, — стекло и газовые баллоны (того, кто выпустил ракету, не задевает).
    public class Rocket : MonoBehaviour
    {
        [SerializeField] private float _speed = 9f;
        [SerializeField] private float _radius = 0.16f;
        [SerializeField] private float _lifetime = 4f;
        [SerializeField] private float _blastRadius = 1.3f;
        [Tooltip("Обо что рвётся")]
        [SerializeField] private LayerMask _hitMask;
        [Tooltip("Кого задевает взрыв")]
        [SerializeField] private LayerMask _blastMask;
        [SerializeField] private GameObject _explosionFxPrefab;
        [SerializeField] private GameObject _remainsPrefab;

        private Vector2 _direction;
        private Transform _owner;
        private float _dieAt;
        private readonly HashSet<Health> _blasted = new HashSet<Health>();

        public float Speed => _speed;

        public void Launch(Vector2 direction, Transform owner)
        {
            _direction = direction.normalized;
            _owner = owner;
            _dieAt = Time.time + _lifetime;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);
        }

        private void Update()
        {
            if (Time.time >= _dieAt)
            {
                Explode(transform.position);
                return;
            }

            float step = _speed * Time.deltaTime;
            Vector2 position = transform.position;
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(position, _radius, _direction, step, _hitMask))
            {
                if (hit.collider.isTrigger) continue;
                if (_owner != null && hit.collider.transform.IsChildOf(_owner)) continue;

                Pillar pillar = hit.collider.GetComponentInParent<Pillar>();
                if (pillar != null) pillar.Break();
                Explode(hit.point);
                return;
            }

            transform.position = position + _direction * step;
        }

        private void Explode(Vector2 at)
        {
            N89.Core.Sfx.Play(N89.Core.Sfx.Explosion, at);
            if (_explosionFxPrefab != null) Instantiate(_explosionFxPrefab, at, Quaternion.identity);
            if (_remainsPrefab != null) Instantiate(_remainsPrefab, at, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));

            foreach (Collider2D hit in Physics2D.OverlapCircleAll(at, _blastRadius, _blastMask))
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || _blasted.Contains(health)) continue;
                if (_owner != null && health.transform.IsChildOf(_owner)) continue;
                _blasted.Add(health);
                health.TakeDamage(999);
            }

            Destroy(gameObject);
        }
    }
}
