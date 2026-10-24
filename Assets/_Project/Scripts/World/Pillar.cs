using System;
using UnityEngine;

namespace N89.World
{
    // Несущая колонна на арене босса: держит потолок над своей частью зала. Пули её не берут —
    // рушит только ракета босса (Rocket). Разрушена — остаётся обломок без коллайдера, а BossFight
    // начинает обрушение потолка вокруг неё.
    public class Pillar : MonoBehaviour
    {
        [SerializeField] private Collider2D _collider;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite _brokenSprite;
        [SerializeField] private GameObject _breakFxPrefab;

        public bool IsBroken { get; private set; }
        public Vector2 Center => transform.position;
        public event Action<Pillar> Broken;

        public void Break()
        {
            if (IsBroken) return;
            IsBroken = true;
            _collider.enabled = false;
            if (_brokenSprite != null) _renderer.sprite = _brokenSprite;
            if (_breakFxPrefab != null) Instantiate(_breakFxPrefab, transform.position, Quaternion.identity);
            Broken?.Invoke(this);
        }
    }
}
