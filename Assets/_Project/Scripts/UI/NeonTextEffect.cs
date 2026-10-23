using TMPro;
using UnityEngine;

namespace N89.UI
{
    // Неоновый текст: волна по буквам, бегущий градиент по палитре и мерцание "перегорающей" лампы.
    [RequireComponent(typeof(TMP_Text))]
    public class NeonTextEffect : MonoBehaviour
    {
        [Header("Wave")]
        [SerializeField] private float _waveAmplitude = 10f;
        [SerializeField] private float _waveSpeed = 3f;
        [SerializeField] private float _waveFrequency = 0.9f;

        [Header("Color")]
        [SerializeField] private bool _animateColors = true;
        [SerializeField] private Color[] _palette =
        {
            new Color(0.1f, 0.95f, 1f),
            new Color(0.65f, 0.25f, 1f),
            new Color(1f, 0.2f, 0.8f),
        };
        [SerializeField] private float _colorSpeed = 0.25f;
        [Tooltip("Насколько градиент растянут по буквам (0 — все буквы одного цвета)")]
        [SerializeField] private float _colorSpread = 0.35f;

        [Header("Flicker")]
        [SerializeField] private float _flickerChancePerSecond = 0.8f;
        [SerializeField] private float _flickerDuration = 0.06f;
        [Range(0f, 1f)]
        [SerializeField] private float _flickerDim = 0.35f;

        private TMP_Text _text;
        private int _flickerChar = -1;
        private float _flickerUntil;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void LateUpdate()
        {
            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            float t = Time.unscaledTime;

            UpdateFlicker(info.characterCount);

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo ch = info.characterInfo[i];
                if (!ch.isVisible)
                {
                    continue;
                }

                int mat = ch.materialReferenceIndex;
                int v = ch.vertexIndex;
                Vector3[] verts = info.meshInfo[mat].vertices;
                Color32[] colors = info.meshInfo[mat].colors32;

                Vector3 offset = new Vector3(0f, Mathf.Sin(t * _waveSpeed + i * _waveFrequency) * _waveAmplitude, 0f);
                for (int k = 0; k < 4; k++)
                {
                    verts[v + k] += offset;
                }

                if (_animateColors && _palette.Length > 0)
                {
                    // Левые вершины (0,1) и правые (2,3) берут немного разные цвета — градиент внутри буквы
                    Color left = SamplePalette(t * _colorSpeed + i * _colorSpread);
                    Color right = SamplePalette(t * _colorSpeed + (i + 0.6f) * _colorSpread);
                    colors[v + 0] = left;
                    colors[v + 1] = left;
                    colors[v + 2] = right;
                    colors[v + 3] = right;
                }

                if (i == _flickerChar)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        Color32 c = colors[v + k];
                        colors[v + k] = new Color32((byte)(c.r * _flickerDim), (byte)(c.g * _flickerDim), (byte)(c.b * _flickerDim), c.a);
                    }
                }
            }

            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }

        private void UpdateFlicker(int charCount)
        {
            float now = Time.unscaledTime;
            if (now < _flickerUntil)
            {
                return;
            }

            _flickerChar = -1;
            if (charCount > 0 && Random.value < _flickerChancePerSecond * Time.unscaledDeltaTime)
            {
                _flickerChar = Random.Range(0, charCount);
                _flickerUntil = now + _flickerDuration * Random.Range(1f, 3f);
            }
        }

        private Color SamplePalette(float x)
        {
            float p = Mathf.Repeat(x, 1f) * _palette.Length;
            int a = Mathf.FloorToInt(p) % _palette.Length;
            int b = (a + 1) % _palette.Length;
            return Color.Lerp(_palette[a], _palette[b], p - Mathf.Floor(p));
        }
    }
}
