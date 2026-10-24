using System;
using System.Collections;
using UnityEngine;

namespace N89.Combat
{
    // Здоровье для всего, что можно убить: игрок, боты, мишени.
    public class Health : MonoBehaviour
    {
        [SerializeField] private int _maxHealth = 1;
        [SerializeField] private bool _destroyOnDeath = true;
        [SerializeField] private GameObject _deathFxPrefab;
        [Tooltip("Что остаётся на полу после смерти (обломки, пятно)")]
        [SerializeField] private GameObject _remainsPrefab;
        [SerializeField] private SpriteRenderer _flashRenderer;
        [SerializeField] private Color _flashColor = new Color(1f, 0.35f, 0.35f);

        private int _health;
        private Color _baseColor;

        public bool IsDead { get; private set; }
        public event Action<Health> Died;
        // Защита (блок «Рук гориллы»): вернула true — удар поглощён, урона нет
        public Func<bool> Guard { get; set; }

        private void Awake()
        {
            _health = _maxHealth;
            if (_flashRenderer != null)
            {
                _baseColor = _flashRenderer.color;
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDead)
            {
                return;
            }

            if (Guard != null && Guard())
            {
                return;
            }

            _health -= amount;
            if (_health <= 0)
            {
                Die();
            }
            else if (_flashRenderer != null)
            {
                StopAllCoroutines();
                StartCoroutine(Flash());
            }
        }

        // Сценарная смерть — мимо блока
        public void Kill()
        {
            if (IsDead)
            {
                return;
            }

            _health = 0;
            Die();
        }

        private void Die()
        {
            IsDead = true;

            if (_deathFxPrefab != null)
            {
                Instantiate(_deathFxPrefab, transform.position, Quaternion.identity);
            }

            if (_remainsPrefab != null)
            {
                Instantiate(_remainsPrefab, transform.position, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
            }

            Died?.Invoke(this);

            if (_destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator Flash()
        {
            _flashRenderer.color = _flashColor;
            yield return new WaitForSeconds(0.08f);
            _flashRenderer.color = _baseColor;
        }
    }
}
