using UnityEngine;

namespace N89.EditorTools
{
    // Временный арт арсенала: иконки оружия (вид сбоку, ствол вправо), заглушки иконок имплантов
    // (заменятся импортированными PNG с теми же именами), рамка барабана и стрелка для UI.
    internal static partial class SpriteFactory
    {
        private static readonly Color GunDark = new Color(0.13f, 0.13f, 0.17f);
        private static readonly Color GunMid = new Color(0.22f, 0.22f, 0.27f);
        private static readonly Color GunEdge = new Color(0.4f, 0.4f, 0.48f);

        // Кибер-ПП: короткий, длинный магазин, складной приклад, голоприцел и бирюзовая полоса
        public static Sprite IconSmg() => Create("weapon_smg", 64, 32, c =>
        {
            c.Rect(4, 13, 6, 20, GunDark);
            c.Rect(6, 15, 13, 18, GunMid);
            c.Rect(14, 13, 44, 21, GunDark);
            c.Rect(16, 22, 40, 23, GunMid);
            c.Rect(45, 15, 54, 19, GunMid);
            c.Rect(55, 14, 58, 20, GunDark);
            c.Line(new Vector2(19f, 14f), new Vector2(16f, 5f), 5f, GunDark);
            c.Rect(26, 2, 30, 13, GunMid);
            c.Rect(27, 2, 27, 12, GunEdge);
            c.Rect(39, 9, 43, 13, GunDark);
            c.Rect(18, 17, 42, 17, Neon);
            c.Rect(31, 24, 39, 28, GunDark);
            c.Rect(32, 25, 38, 27, new Color(0.3f, 1f, 1f, 0.55f));
            c.Rect(14, 21, 44, 21, GunEdge);
            c.Outline(Outline);
        });

        // Автомат: длинный ствол с дульным тормозом, изогнутый магазин, оптика, жёлтая полоса
        public static Sprite IconRifle() => Create("weapon_rifle", 64, 32, c =>
        {
            c.Line(new Vector2(2f, 16f), new Vector2(13f, 17.5f), 7f, GunDark);
            c.Rect(13, 14, 40, 21, GunDark);
            c.Rect(36, 14, 50, 20, GunMid);
            for (int x = 38; x < 50; x += 3) c.Rect(x, 17, x, 18, GunDark);
            c.Rect(51, 16, 58, 18, GunMid);
            c.Rect(59, 15, 62, 19, GunDark);
            c.Line(new Vector2(18f, 14f), new Vector2(16f, 5f), 4.5f, GunDark);
            c.Line(new Vector2(27f, 13f), new Vector2(30f, 3f), 5f, GunMid);
            c.Rect(19, 23, 34, 26, GunDark);
            c.Rect(34, 23, 35, 26, CpYellow);
            c.Rect(14, 18, 32, 18, CpYellow);
            c.Rect(13, 21, 40, 21, GunEdge);
            c.Outline(Outline);
        });

        // Дробовик: длинный ствол над трубчатым магазином, рифлёное цевьё, красный акцент
        public static Sprite IconShotgun() => Create("weapon_shotgun", 64, 32, c =>
        {
            c.Line(new Vector2(2f, 13f), new Vector2(14f, 18f), 7f, GunDark);
            c.Rect(14, 14, 30, 21, GunDark);
            c.Rect(31, 18, 61, 21, GunMid);
            c.Rect(31, 14, 54, 17, GunDark);
            c.Rect(38, 12, 50, 17, new Color(0.09f, 0.09f, 0.12f));
            for (int x = 39; x < 50; x += 2) c.Rect(x, 12, x, 16, GunMid);
            c.Line(new Vector2(17f, 14f), new Vector2(15f, 6f), 4.5f, GunDark);
            c.Rect(15, 19, 29, 19, Red);
            c.Rect(31, 21, 61, 21, GunEdge);
            c.Rect(59, 18, 61, 21, GunDark);
            c.Outline(Outline);
        });

        // Заглушка "Gorilla hands": механический кулак с жёлтыми гидравлическими поршнями
        public static Sprite IconGorillaArms() => Create("implant_gorilla_arms", 48, 48, c =>
        {
            Color steel = new Color(0.45f, 0.46f, 0.52f);
            Color steelDark = new Color(0.25f, 0.26f, 0.31f);
            c.Line(new Vector2(6f, 6f), new Vector2(22f, 22f), 11f, steelDark);
            c.Line(new Vector2(9f, 12f), new Vector2(22f, 25f), 2f, CpYellow);
            c.Line(new Vector2(12f, 6f), new Vector2(25f, 19f), 2f, CpYellow);
            c.RoundRect(18, 18, 42, 42, 5, steel);
            for (int i = 0; i < 4; i++)
            {
                c.RoundRect(22 + i * 5, 36, 25 + i * 5, 42, 1, steelDark);
            }
            c.Rect(20, 24, 40, 25, steelDark);
            c.Rect(24, 28, 34, 30, new Color(1f, 0.5f, 0.15f));
            c.Outline(Outline);
        });

        // E.N.A: позвоночник из сегментов, по нему бежит зелёный свет
        public static Sprite IconSandevistan() => Create("implant_sandevistan", 48, 48, c =>
        {
            Color metal = new Color(0.55f, 0.57f, 0.65f);
            Color dark = new Color(0.16f, 0.17f, 0.22f);
            Color glow = new Color(0.25f, 1f, 0.55f);
            c.Rect(22, 4, 25, 43, glow * 0.6f);
            for (int y = 5; y <= 39; y += 6)
            {
                int half = 6 + (y > 15 && y < 33 ? 2 : 0);
                c.RoundRect(24 - half, y, 23 + half, y + 3, 1.5f, metal);
                c.Rect(22, y + 1, 25, y + 2, dark);
                c.Rect(23, y + 1, 24, y + 2, glow);
                c.Rect(24 - half - 3, y + 1, 24 - half - 1, y + 2, metal);
                c.Rect(23 + half + 1, y + 1, 23 + half + 3, y + 2, metal);
            }
            c.Outline(new Color(0.04f, 0.05f, 0.06f));
        });

        // Заглушка засекреченного импланта: чип с вопросом
        public static Sprite IconLockedImplant(string name) => Create(name, 48, 48, c =>
        {
            Color pin = new Color(0.55f, 0.55f, 0.6f);
            for (int i = 12; i <= 34; i += 6)
            {
                c.Rect(i, 3, i + 1, 8, pin);
                c.Rect(i, 39, i + 1, 44, pin);
                c.Rect(3, i, 8, i + 1, pin);
                c.Rect(39, i, 44, i + 1, pin);
            }
            c.RoundRect(8, 8, 39, 39, 3, new Color(0.08f, 0.16f, 0.12f));
            c.Rect(11, 11, 36, 36, new Color(0.05f, 0.1f, 0.08f));
            // "?"
            Color q = new Color(0.3f, 1f, 1f);
            c.Rect(19, 29, 28, 31, q);
            c.Rect(26, 24, 28, 29, q);
            c.Rect(22, 22, 27, 24, q);
            c.Rect(22, 19, 24, 22, q);
            c.Rect(22, 14, 24, 16, q);
            c.Outline(Outline);
        });

        // Барабан выбора: вертикальный эллипс; к верху и низу темнеет — "рыбий глаз"
        public static Sprite DrumFrame() => Create("ui_drum", 120, 300, c =>
        {
            float cx = 60f, cy = 150f, rx = 58f, ry = 148f;
            for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float dx = x + 0.5f - cx;
                float dy = y + 0.5f - cy;
                bool outer = (dx * dx) / (rx * rx) + (dy * dy) / (ry * ry) <= 1f;
                if (!outer) continue;
                bool inner = (dx * dx) / ((rx - 2f) * (rx - 2f)) + (dy * dy) / ((ry - 2f) * (ry - 2f)) <= 1f;
                if (!inner)
                {
                    c.Replace(x, y, new Color(0.3f, 1f, 1f, 0.9f));
                    continue;
                }
                float edge = Mathf.Abs(dy) / ry;
                c.Replace(x, y, new Color(0.02f, 0.01f, 0.05f, 0.15f + 0.6f * edge * edge));
            }
        });

        public static Sprite ArrowUp() => Create("ui_arrow", 15, 9, c =>
        {
            for (int y = 0; y < 9; y++)
            {
                int half = 7 - y * 7 / 8;
                c.Rect(7 - half, y, 7 + half, y, Color.white);
            }
        });
    }
}
