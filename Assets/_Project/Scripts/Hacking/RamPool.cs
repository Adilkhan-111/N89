using UnityEngine;

namespace N89.Hacking
{
    // Оперативная память импланта: слоты тратятся на взломы и сами восстанавливаются со временем.
    public class RamPool : MonoBehaviour
    {
        [SerializeField] private int _maxRam = 4;
        [SerializeField] private float _secondsPerSlot = 5f;

        private float _ram;

        public int Max => _maxRam;
        public int Current => Mathf.FloorToInt(_ram + 0.0001f);
        public float Exact => _ram;

        private void Awake()
        {
            _ram = _maxRam;
        }

        private void Update()
        {
            if (_ram < _maxRam)
            {
                _ram = Mathf.Min(_maxRam, _ram + Time.deltaTime / _secondsPerSlot);
            }
        }

        public bool TrySpend(int cost)
        {
            if (Current < cost)
            {
                return false;
            }

            _ram -= cost;
            return true;
        }

        public void Refill()
        {
            _ram = _maxRam;
        }
    }
}
