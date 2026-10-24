using System;
using UnityEngine;

namespace N89.Combat
{
    // Простое оружие: кулдаун, разброс, пуля из префаба. Используется игроком и турелями.
    // Параметры оружия из арсенала задаёт Configure(WeaponData).
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private Bullet _bulletPrefab;
        [SerializeField] private Transform _muzzle;
        [SerializeField] private float _cooldown = 0.16f;
        [SerializeField] private float _spreadDegrees = 2f;
        [Tooltip("Пуль за выстрел (дробовик — веер дробин)")]
        [SerializeField] private int _pellets = 1;
        [Tooltip("Дальность пули в юнитах; 0 — по времени жизни пули")]
        [SerializeField] private float _range;
        [SerializeField] private LayerMask _hitMask;
        [SerializeField] private GameObject _muzzleFxPrefab;

        private float _nextShotTime;

        public event Action Fired;

        public void Configure(WeaponData data)
        {
            _cooldown = data.Cooldown;
            _spreadDegrees = data.SpreadDegrees;
            _pellets = data.Pellets;
            _range = data.Range;
        }

        public bool TryFire(Vector2 direction)
        {
            if (Time.time < _nextShotTime)
            {
                return false;
            }

            _nextShotTime = Time.time + _cooldown;

            float angle = 0f;
            for (int i = 0; i < Mathf.Max(1, _pellets); i++)
            {
                float spread = UnityEngine.Random.Range(-_spreadDegrees, _spreadDegrees);
                Vector2 dir = Quaternion.Euler(0f, 0f, spread) * direction.normalized;
                angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                Bullet bullet = Instantiate(_bulletPrefab, _muzzle.position, Quaternion.Euler(0f, 0f, angle));
                bullet.Launch(dir, _hitMask, transform, _range);
            }

            if (_muzzleFxPrefab != null)
            {
                Instantiate(_muzzleFxPrefab, _muzzle.position, Quaternion.Euler(0f, 0f, angle));
            }

            Fired?.Invoke();
            return true;
        }
    }
}
