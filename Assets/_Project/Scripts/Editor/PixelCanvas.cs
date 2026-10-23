using UnityEngine;

namespace N89.EditorTools
{
    // Мини-"холст" для рисования пиксель-арта кодом: круги, прямоугольники, линии, обводка.
    internal class PixelCanvas
    {
        private readonly Color[] _pixels;

        public int Width { get; }
        public int Height { get; }

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color[width * height];
        }

        public Color Get(int x, int y) => _pixels[y * Width + x];

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return;
            }

            int i = y * Width + x;
            Color dst = _pixels[i];
            float a = c.a + dst.a * (1f - c.a);
            if (a <= 0f)
            {
                _pixels[i] = Color.clear;
                return;
            }

            Color rgb = (c * c.a + dst * dst.a * (1f - c.a)) / a;
            _pixels[i] = new Color(rgb.r, rgb.g, rgb.b, a);
        }

        public void Replace(int x, int y, Color c)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                _pixels[y * Width + x] = c;
            }
        }

        public void Rect(int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                Set(x, y, c);
        }

        public void Circle(float cx, float cy, float r, Color c)
        {
            Ellipse(cx, cy, r, r, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color c)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
            for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float dy = (y + 0.5f - cy) / ry;
                if (dx * dx + dy * dy <= 1f)
                {
                    Set(x, y, c);
                }
            }
        }

        public void Ring(float cx, float cy, float rInner, float rOuter, Color c)
        {
            for (int y = Mathf.FloorToInt(cy - rOuter); y <= Mathf.CeilToInt(cy + rOuter); y++)
            for (int x = Mathf.FloorToInt(cx - rOuter); x <= Mathf.CeilToInt(cx + rOuter); x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                if (d >= rInner && d <= rOuter)
                {
                    Set(x, y, c);
                }
            }
        }

        public void Line(Vector2 a, Vector2 b, float thickness, Color c)
        {
            int x0 = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - thickness);
            int x1 = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + thickness);
            int y0 = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - thickness);
            int y1 = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + thickness);
            Vector2 ab = b - a;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                if (Vector2.Distance(p, a + ab * t) <= thickness * 0.5f)
                {
                    Set(x, y, c);
                }
            }
        }

        public void RoundRect(int x0, int y0, int x1, int y1, float radius, Color c)
        {
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float qx = Mathf.Max(Mathf.Max(x0 + radius - px, px - (x1 + 1 - radius)), 0f);
                float qy = Mathf.Max(Mathf.Max(y0 + radius - py, py - (y1 + 1 - radius)), 0f);
                if (qx * qx + qy * qy <= radius * radius)
                {
                    Set(x, y, c);
                }
            }
        }

        // Обводка: прозрачные пиксели рядом с непрозрачными закрашиваются цветом обводки
        public void Outline(Color c)
        {
            var copy = (Color[])_pixels.Clone();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (copy[y * Width + x].a > 0.1f)
                {
                    continue;
                }

                bool near = (x > 0 && copy[y * Width + x - 1].a > 0.5f)
                            || (x < Width - 1 && copy[y * Width + x + 1].a > 0.5f)
                            || (y > 0 && copy[(y - 1) * Width + x].a > 0.5f)
                            || (y < Height - 1 && copy[(y + 1) * Width + x].a > 0.5f);
                if (near)
                {
                    _pixels[y * Width + x] = c;
                }
            }
        }

        public byte[] EncodePng()
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            tex.SetPixels(_pixels);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return png;
        }
    }
}
