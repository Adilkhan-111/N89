using System;
using UnityEngine;
using UnityEngine.UI;

namespace N89.UI
{
    // Вертикальный барабан с эффектом "рыбьего глаза": элементы лежат на цилиндре, центральный —
    // крупный и яркий, соседние сжимаются по высоте, уменьшаются и тускнеют. Крутится по кругу.
    public class DrumSelector : MonoBehaviour
    {
        [Tooltip("Элементы барабана; у каждого CanvasGroup")]
        [SerializeField] private RectTransform[] _items;
        [Tooltip("Рамка барабана — ярче, когда барабан активен")]
        [SerializeField] private Image _frame;
        [Tooltip("Полоски вокруг центрального окна и стрелки — тоже подсвечиваются")]
        [SerializeField] private Image[] _accents;
        [SerializeField] private float _radius = 300f;
        [Tooltip("Угол между соседними элементами на цилиндре, в градусах")]
        [SerializeField] private float _angleStep = 52f;
        [SerializeField] private float _spinSpeed = 12f;
        [SerializeField] private Color _activeColor = new Color(0.3f, 1f, 1f);
        [SerializeField] private Color _idleColor = new Color(0.3f, 1f, 1f, 0.35f);

        private float _target;
        private float _scroll;

        public int Index => Mod(Mathf.RoundToInt(_target), _items.Length);
        public RectTransform[] Items => _items;

        public event Action<int> Changed;

        public bool Active
        {
            set
            {
                Color color = value ? _activeColor : _idleColor;
                _frame.color = color;
                foreach (Image accent in _accents) accent.color = color;
            }
        }

        public void Step(int delta)
        {
            _target += delta;
            Changed?.Invoke(Index);
        }

        public void SetIndex(int index)
        {
            _target = _scroll = index;
            Layout();
            Changed?.Invoke(Index);
        }

        private void Update()
        {
            _scroll = Mathf.Lerp(_scroll, _target, 1f - Mathf.Exp(-_spinSpeed * Time.unscaledDeltaTime));
            Layout();
        }

        private void Layout()
        {
            int count = _items.Length;
            for (int i = 0; i < count; i++)
            {
                // Кратчайшее смещение по кругу: элемент, ушедший за низ, появляется сверху
                float offset = Mathf.Repeat(i - _scroll + count * 0.5f, count) - count * 0.5f;
                float angle = offset * _angleStep;
                RectTransform item = _items[i];
                CanvasGroup group = item.GetComponent<CanvasGroup>();
                if (Mathf.Abs(angle) >= 89f)
                {
                    group.alpha = 0f;
                    continue;
                }

                float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
                float size = Mathf.Lerp(0.45f, 1.1f, cos * cos * cos);
                item.anchoredPosition = new Vector2(0f, -Mathf.Sin(angle * Mathf.Deg2Rad) * _radius);
                item.localScale = new Vector3(size, size * cos, 1f);
                group.alpha = Mathf.Lerp(0.15f, 1f, cos * cos * cos * cos);
            }
        }

        private static int Mod(int value, int count) => ((value % count) + count) % count;
    }
}
