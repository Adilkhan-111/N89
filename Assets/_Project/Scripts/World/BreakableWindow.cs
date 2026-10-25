using N89.Combat;
using UnityEngine;

namespace N89.World
{
    // Стеклянная перегородка в проёме: на слое Glass — не пройти и пули не пробивают, но сквозь неё
    // видно (и враги видят героя). Первая же пуля — своя или вражеская — бьёт стекло: рама остаётся,
    // осколки ложатся на пол, проём становится проходом. NavGrid о разбитом окне не знает —
    // враги могут обходить его по старому пути, но напрямую пройдут.
    public class BreakableWindow : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private SpriteRenderer _pane;
        [SerializeField] private Sprite _brokenSprite;
        [Tooltip("Осколки на полу по обе стороны — спрятаны, пока стекло целое")]
        [SerializeField] private SpriteRenderer _shards;

        public bool IsBroken { get; private set; }

        private void Awake()
        {
            _health.Died += _ => Break();
            if (_shards != null) _shards.enabled = false;
        }

        private void Break()
        {
            N89.Core.Sfx.Play(N89.Core.Sfx.Glass, transform.position);
            IsBroken = true;
            _collider.enabled = false;
            _pane.sprite = _brokenSprite;
            if (_shards != null) _shards.enabled = true;
        }
    }
}
