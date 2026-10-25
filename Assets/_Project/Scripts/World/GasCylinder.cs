using System.Collections;
using System.Collections.Generic;
using N89.Combat;
using N89.Enemies;
using N89.Player;
using UnityEngine;

namespace N89.World
{
    // Газовый баллон на арене босса: падает сверху (тень растёт, баллон «опускается» и бьётся об пол),
    // потом стоит, пока его не подстрелят. Взрыв: босс в радиусе теряет _bossDamage (1/8 здоровья),
    // герой погибает, соседние баллоны детонируют следом.
    public class GasCylinder : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private float _fallTime = 0.8f;
        [SerializeField] private float _blastRadius = 2.4f;
        [SerializeField] private int _bossDamage = 50;
        [SerializeField] private LayerMask _blastMask;
        [SerializeField] private GameObject _explosionFxPrefab;
        [SerializeField] private GameObject _remainsPrefab;

        public bool IsGone { get; private set; }

        private void Awake()
        {
            _health.Died += _ => Explode();
        }

        private IEnumerator Start()
        {
            // Пока падает — не мешает и не взрывается
            _collider.enabled = false;
            Transform body = _renderer.transform;
            for (float t = 0f; t < _fallTime; t += Time.deltaTime)
            {
                float k = t / _fallTime;
                body.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, k * k);
                _renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 2f));
                _shadow.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
                _shadow.color = new Color(0f, 0f, 0f, 0.5f * k);
                yield return null;
            }

            body.localScale = Vector3.one;
            _renderer.color = Color.white;
            _collider.enabled = true;
            CameraFollow follow = FindAnyObjectByType<CameraFollow>();
            if (follow != null) follow.Shake(0.12f, 0.1f);
        }

        private void Explode()
        {
            N89.Core.Sfx.Play(N89.Core.Sfx.Explosion, transform.position);
            if (IsGone) return;
            IsGone = true;
            Vector2 at = transform.position;
            if (_explosionFxPrefab != null) Instantiate(_explosionFxPrefab, at, Quaternion.identity);
            if (_remainsPrefab != null) Instantiate(_remainsPrefab, at, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            CameraFollow follow = FindAnyObjectByType<CameraFollow>();
            if (follow != null) follow.Shake(0.4f, 0.25f);

            var done = new HashSet<Health> { _health };
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(at, _blastRadius, _blastMask))
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health == null || health.IsDead || !done.Add(health)) continue;
                health.TakeDamage(health.GetComponent<BossAI>() != null ? _bossDamage : 999);
            }

            Destroy(gameObject);
        }
    }
}
