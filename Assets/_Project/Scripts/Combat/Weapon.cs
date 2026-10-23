using System;
using UnityEngine;

namespace N89.Combat
{
    // Простое оружие: кулдаун, разброс, пуля из префаба. Используется игроком и турелями.
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private Bullet _bulletPrefab;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _cooldown = 0.16f;
        [SerializeField] private float _spreadDegrees = 2f;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private GameObject _muzzleFxPrefab;

        private float _nextShotTime;

        public event Action Fired;

        public bool TryFire(Vector2 direction)
        {
            if (Time.time < _nextShotTime)
            {
                return false;
            }

            _nextShotTime = Time.time + _cooldown;

            float spread = UnityEngine.Random.Range(-_spreadDegrees, _spreadDegrees);
            Vector2 dir = Quaternion.Euler(0f, 0f, spread) * direction.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            Bullet bullet = Instantiate(_bulletPrefab, _muzzle.position, Quaternion.Euler(0f, 0f, angle));
            bullet.Launch(dir, _hitMask, transform);

            if (_muzzleFxPrefab != null)
            {
                Instantiate(_muzzleFxPrefab, _muzzle.position, Quaternion.Euler(0f, 0f, angle));
            }

            Fired?.Invoke();
            return true;
        }
    }
}
