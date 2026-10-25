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
        [Tooltip("Труп на полу (вид сверху, голова — +x). Падает навзничь — головой назад от того, куда смотрел")]
        [SerializeField] private Sprite _corpseSprite;

        private int _health;
        private Color _baseColor;

        public bool IsDead { get; private set; }
        // Доля оставшегося здоровья 0..1 (полоса здоровья босса)
        public float Fraction => _maxHealth <= 0 ? 0f : Mathf.Clamp01((float)_health / _maxHealth);
        public Sprite CorpseSprite => _corpseSprite;
        // Выключить, если тело уложит кто-то другой (катсцена, удар «Рук гориллы» отшвыривает его сам)
        public bool SpawnCorpse { get; set; } = true;
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
            if (!IsDead) N89.Core.Sfx.Play(N89.Core.Sfx.Hit, transform.position, 0.6f);
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
            N89.Core.Sfx.Play(N89.Core.Sfx.Death, transform.position);
            IsDead = true;

            if (_deathFxPrefab != null)
            {
                Instantiate(_deathFxPrefab, transform.position, Quaternion.identity);
            }

            if (_remainsPrefab != null)
            {
                Instantiate(_remainsPrefab, transform.position, Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f)));
            }

            if (SpawnCorpse && _corpseSprite != null)
            {
                LeaveCorpse();
            }

            Died?.Invoke(this);

            if (_destroyOnDeath)
            {
                Destroy(gameObject);
            }
        }

        // Тело остаётся на полу поверх лужи крови; живой спрайт (если объект не уничтожается) прячется
        private void LeaveCorpse()
        {
            float facing = _flashRenderer != null ? _flashRenderer.transform.eulerAngles.z : UnityEngine.Random.Range(0f, 360f);
            float angle = facing + 180f + UnityEngine.Random.Range(-25f, 25f);
            Vector2 back = Quaternion.Euler(0f, 0f, angle) * Vector3.right;

            var corpse = new GameObject($"{name}_Corpse").AddComponent<SpriteRenderer>();
            corpse.sprite = _corpseSprite;
            if (_flashRenderer != null) corpse.sharedMaterial = _flashRenderer.sharedMaterial;
            corpse.sortingOrder = -60;
            corpse.transform.SetPositionAndRotation((Vector2)transform.position + back * 0.2f, Quaternion.Euler(0f, 0f, angle));

            if (!_destroyOnDeath && _flashRenderer != null)
            {
                _flashRenderer.enabled = false;
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
