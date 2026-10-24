using UnityEngine;

namespace N89.EditorTools
{
    // Временный арт миссии 2: охрана «Арка Системс» и обычный дом в центре города — кухня, бассейн с рыбками,
    // гостиные, стекло в проёмах. Настоящий арт — заменить PNG в Generated с теми же именами.
    internal static partial class SpriteFactory
    {
        // Фирменный цвет «Арки» — холодный бирюзовый, броня белая: чистая корпоративная охрана в отличие
        // от тёмной брони Корпорации Е
        private static readonly Color ArkaTeal = new Color(0.25f, 0.95f, 0.85f);
        private static readonly Color ArkaArmor = new Color(0.78f, 0.81f, 0.86f);
        private static readonly Color ArkaArmorShade = new Color(0.52f, 0.56f, 0.64f);
        private static readonly Color ArkaGloves = new Color(0.12f, 0.13f, 0.16f);
        private static readonly Color ArkaHelmet = new Color(0.88f, 0.9f, 0.94f);

        // Охранник «Арки»: белая броня, шлем с бирюзовым визором, автомат; полоса на затылке и нашивка на плече
        public static Sprite ArkaGuard() => Create("arka_guard", 32, 32, c =>
        {
            DrawShooter(c, ArkaArmor, ArkaArmorShade, ArkaGloves, new Color(0.08f, 0.09f, 0.11f), ArkaHelmet, ArkaTeal,
                new Color(0.15f, 0.6f, 0.62f), DrawRifle);
            c.Rect(12, 22, 14, 23, ArkaTeal);
            c.Rect(9, 16, 12, 16, new Color(0.15f, 0.6f, 0.62f));
        });

        public static Sprite ArkaGuardDead() => Create("arka_guard_dead", 48, 32, c =>
        {
            DrawCorpse(c, ArkaArmor, ArkaArmorShade, new Color(0.2f, 0.22f, 0.27f), ArkaGloves, ArkaHelmet);
            c.Rect(24, 19, 26, 20, ArkaTeal);
            c.Rect(38, 13, 39, 15, ArkaTeal);
            c.Outline(Outline);
        });

        // ---------------------------------------------------------------- Руки-лезвия

        // Клинок из предплечья, смотрит вправо (+x): слева механизм крепления, дальше тёмная сталь
        // со светлой зубчатой кромкой и красными метками, как на арте импланта
        public static Sprite BladeArm() => Create("blade_arm", 40, 8, c =>
        {
            Color steel = new Color(0.27f, 0.25f, 0.27f);
            Color edge = new Color(0.82f, 0.8f, 0.7f);
            c.RoundRect(0, 1, 7, 6, 2, new Color(0.12f, 0.12f, 0.15f));
            c.Rect(2, 3, 5, 4, new Color(0.35f, 0.38f, 0.45f));
            c.Line(new Vector2(7f, 4.5f), new Vector2(39f, 5.5f), 3f, steel);
            for (int x = 8; x < 38; x++)
            {
                int teeth = x % 3 == 0 ? 2 : 3;
                c.Rect(x, teeth, x, 3, edge);
            }
            c.Rect(14, 6, 15, 6, new Color(1f, 0.2f, 0.25f));
            c.Rect(24, 6, 25, 6, new Color(1f, 0.2f, 0.25f));
            c.Outline(Outline);
        });

        // След взмаха: серп перед героем (центр спрайта — герой), ярче в середине дуги
        public static Sprite BladeSlash() => Create("blade_slash", 64, 64, c =>
        {
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Vector2 d = new Vector2(x - 31.5f, y - 31.5f);
                float r = d.magnitude;
                float a = Mathf.Abs(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                if (a > 78f || r < 18f || r > 31f) continue;
                float edge = 1f - Mathf.Abs(r - 27f) / 9f;
                float fade = 1f - a / 78f;
                float alpha = Mathf.Clamp01(edge) * Mathf.Clamp01(fade * 1.6f);
                Color col = r > 28f ? Color.white : new Color(0.4f, 1f, 1f);
                c.Replace(x, y, new Color(col.r, col.g, col.b, alpha * 0.9f));
            }
        });

        // Шлейф за клинком: дуга 0..72° (центр спрайта — герой), ярче всего у переднего края (0°, где клинок),
        // к хвосту гаснет. Крутится вместе с клинком
        public static Sprite BladeSmear() => Create("blade_smear", 64, 64, c =>
        {
            for (int y = 32; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Vector2 d = new Vector2(x - 31.5f, y - 31.5f);
                float r = d.magnitude;
                float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                if (a < 0f || a > 72f || r < 16f || r > 31f) continue;
                float edge = Mathf.Clamp01(1f - Mathf.Abs(r - 26f) / 10f);
                float fade = Mathf.Pow(1f - a / 72f, 1.5f);
                Color col = r > 28.5f ? Color.white : new Color(0.4f, 1f, 1f);
                c.Replace(x, y, new Color(col.r, col.g, col.b, edge * fade));
            }
        });

        // Кольцо-вспышка отбитой пули: яркая кромка, мягко гаснет внутрь
        public static Sprite FxRing() => Create("fx_ring", 48, 48, c =>
        {
            for (int y = 0; y < 48; y++)
            for (int x = 0; x < 48; x++)
            {
                float r = new Vector2(x - 23.5f, y - 23.5f).magnitude;
                if (r > 23f || r < 13f) continue;
                float alpha = r > 20f ? 1f : Mathf.Pow((r - 13f) / 7f, 2f) * 0.7f;
                c.Replace(x, y, new Color(1f, 1f, 1f, alpha));
            }
        });

        // ---------------------------------------------------------------- Полы

        // Холл: светло-серый мрамор с прожилками, плиты 16 px
        public static Sprite MarbleFloor() => Create("floor_marble", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float vein = Mathf.Abs(Mathf.Sin(x * 0.35f + y * 0.18f + Mathf.PerlinNoise(x * 0.15f, y * 0.15f) * 4f));
                float n = Hash(x + 7, y + 31) * 0.03f;
                Color col = new Color(0.5f + n, 0.52f + n, 0.57f + n);
                if (vein < 0.08f) col = new Color(0.62f, 0.64f, 0.7f);
                if (x % 16 == 0 || y % 16 == 0) col = new Color(0.36f, 0.37f, 0.42f);
                c.Replace(x, y, col);
            }
        });

        // Тротуар вокруг здания: бетонная плитка со швами
        public static Sprite Sidewalk() => Create("floor_sidewalk", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float n = Hash(x + 51, y + 3) * 0.05f + Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.03f;
                Color col = new Color(0.25f + n, 0.26f + n, 0.29f + n);
                if (x % 8 == 0 || y % 8 == 0) col = new Color(0.17f, 0.17f, 0.2f);
                c.Replace(x, y, col);
            }
        });

        // ---------------------------------------------------------------- Бассейн

        // Бассейн: бортик из плитки, вода темнеет к центру, по воде блики. Рыбки — отдельные объекты
        public static Sprite Pool() => Create("pool", 128, 80, c =>
        {
            Color tile = new Color(0.72f, 0.78f, 0.84f);
            Color grout = new Color(0.5f, 0.56f, 0.64f);
            c.RoundRect(0, 0, 127, 79, 6, tile);
            for (int x = 0; x < 128; x += 6) c.Rect(x, 0, x, 79, grout);
            for (int y = 0; y < 80; y += 6) c.Rect(0, y, 127, y, grout);

            for (int y = 6; y <= 73; y++)
            for (int x = 6; x <= 121; x++)
            {
                float edge = Mathf.Min(Mathf.Min(x - 6, 121 - x), Mathf.Min(y - 6, 73 - y)) / 30f;
                float depth = Mathf.Clamp01(edge);
                Color water = Color.Lerp(new Color(0.12f, 0.55f, 0.66f), new Color(0.03f, 0.2f, 0.32f), depth);
                // Блики: волнистые светлые линии
                float wave = Mathf.Sin(x * 0.22f + Mathf.Sin(y * 0.3f) * 2.2f) + Mathf.Sin(y * 0.27f - x * 0.08f);
                if (wave > 1.75f) water = Color.Lerp(water, new Color(0.55f, 0.95f, 1f), 0.45f);
                c.Replace(x, y, water);
            }

            // Лесенка у бортика
            Color steel = new Color(0.82f, 0.85f, 0.9f);
            c.Rect(110, 10, 111, 22, steel);
            c.Rect(116, 10, 117, 22, steel);
            for (int y = 12; y <= 20; y += 4) c.Rect(110, y, 117, y, steel);
            c.Outline(Outline);
        });

        // Рыбка сверху, смотрит вправо (+x): тело, хвост слева, пятна
        public static Sprite Fish(string name, Color body, Color patch) => Create(name, 14, 8, c =>
        {
            c.Ellipse(7.5f, 4f, 4.5f, 2.3f, body);
            c.Line(new Vector2(3f, 4f), new Vector2(0.5f, 1.5f), 1.6f, body);
            c.Line(new Vector2(3f, 4f), new Vector2(0.5f, 6.5f), 1.6f, body);
            c.Ellipse(8.5f, 4.5f, 1.6f, 1.1f, patch);
            c.Rect(5, 3, 6, 3, patch);
            c.Rect(11, 4, 11, 4, new Color(0.05f, 0.05f, 0.08f));
        });

        // ---------------------------------------------------------------- Мебель

        // Кухонный остров (вертикально): мраморная столешница, сверху варочная панель и кофемашина, снизу мойка
        public static Sprite KitchenIsland() => Create("kitchen_island", 56, 112, c =>
        {
            c.RoundRect(0, 0, 55, 111, 5, new Color(0.2f, 0.2f, 0.25f));
            c.RoundRect(2, 2, 53, 109, 4, new Color(0.8f, 0.82f, 0.86f));
            for (int y = 4; y < 108; y++)
            for (int x = 4; x < 52; x++)
            {
                if (Mathf.Abs(Mathf.Sin(x * 0.3f + y * 0.12f + Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 5f)) < 0.05f)
                    c.Replace(x, y, new Color(0.66f, 0.68f, 0.74f));
            }

            // Варочная панель: чёрное стекло, конфорки с красной подсветкой
            c.RoundRect(5, 86, 28, 106, 3, new Color(0.06f, 0.06f, 0.08f));
            foreach (Vector2 p in new[] { new Vector2(11, 100), new Vector2(21, 100), new Vector2(11, 92), new Vector2(21, 92) })
            {
                c.Ring(p.x, p.y, 2.2f, 3.4f, new Color(0.95f, 0.25f, 0.2f, 0.85f));
            }
            // Кофемашина
            c.RoundRect(33, 92, 49, 106, 3, new Color(0.15f, 0.15f, 0.18f));
            c.Circle(41f, 99f, 3f, new Color(0.35f, 0.35f, 0.4f));
            c.Rect(36, 104, 46, 104, ArkaTeal);
            // Мойка: две чаши и кран
            Color steel = new Color(0.6f, 0.64f, 0.7f);
            c.RoundRect(34, 10, 50, 44, 3, steel);
            c.RoundRect(36, 29, 48, 42, 3, new Color(0.35f, 0.38f, 0.44f));
            c.RoundRect(36, 12, 48, 26, 3, new Color(0.35f, 0.38f, 0.44f));
            c.Rect(30, 26, 34, 28, steel);
            c.Outline(Outline);
        });

        // Барный стул сверху: круглое сиденье
        public static Sprite Stool() => Create("stool", 16, 16, c =>
        {
            c.Circle(7.5f, 7.5f, 6.5f, new Color(0.18f, 0.17f, 0.22f));
            c.Circle(7.5f, 7.5f, 5f, new Color(0.42f, 0.16f, 0.22f));
            c.Circle(6f, 9f, 1.6f, new Color(0.6f, 0.26f, 0.32f));
            c.Outline(Outline);
        });

        // Растение в кадке сверху: тёмный горшок, листья лучами
        public static Sprite Plant() => Create("plant", 24, 24, c =>
        {
            c.Circle(11.5f, 11.5f, 10.5f, new Color(0.14f, 0.14f, 0.17f));
            c.Circle(11.5f, 11.5f, 8.5f, new Color(0.23f, 0.17f, 0.12f));
            Color leaf = new Color(0.16f, 0.5f, 0.27f);
            Color leafLight = new Color(0.3f, 0.72f, 0.38f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                c.Line(new Vector2(11.5f, 11.5f), new Vector2(11.5f, 11.5f) + dir * 10.5f, 2.6f, i % 2 == 0 ? leaf : leafLight);
            }
            c.Circle(11.5f, 11.5f, 2.2f, leafLight);
            c.Outline(Outline);
        });

        // Длинный низкий диван из тёмной кожи с бирюзовой подсветкой снизу
        public static Sprite ModernSofa() => Create("sofa_modern", 128, 32, c =>
        {
            Color leather = new Color(0.16f, 0.16f, 0.2f);
            c.RoundRect(0, 0, 127, 31, 7, new Color(0.08f, 0.08f, 0.1f));
            c.RoundRect(2, 20, 125, 29, 5, new Color(0.11f, 0.11f, 0.14f));
            for (int i = 0; i < 4; i++)
            {
                int x0 = 4 + i * 30;
                c.RoundRect(x0, 4, x0 + 28, 18, 4, leather);
                c.Rect(x0 + 3, 15, x0 + 25, 15, new Color(0.26f, 0.26f, 0.32f));
            }
            c.Rect(8, 1, 119, 1, ArkaTeal);
            c.Outline(Outline);
        });

        // Кресло сверху: спинка, подлокотники, подушка
        public static Sprite Armchair() => Create("armchair", 32, 32, c =>
        {
            c.RoundRect(0, 0, 31, 31, 6, new Color(0.1f, 0.1f, 0.13f));
            c.RoundRect(2, 22, 29, 29, 4, new Color(0.2f, 0.32f, 0.38f));
            c.RoundRect(2, 3, 7, 21, 3, new Color(0.2f, 0.32f, 0.38f));
            c.RoundRect(24, 3, 29, 21, 3, new Color(0.2f, 0.32f, 0.38f));
            c.RoundRect(8, 3, 23, 20, 3, new Color(0.28f, 0.44f, 0.5f));
            c.Outline(Outline);
        });

        // Пульт охраны: стол с тремя мониторами камер (картинка с камер — зелёная)
        public static Sprite SecurityConsole() => Create("security_console", 40, 96, c =>
        {
            c.RoundRect(0, 0, 39, 95, 4, new Color(0.13f, 0.13f, 0.17f));
            c.Rect(2, 2, 14, 93, new Color(0.2f, 0.2f, 0.25f));
            for (int i = 0; i < 3; i++)
            {
                int y0 = 6 + i * 30;
                c.Rect(18, y0, 36, y0 + 24, new Color(0.04f, 0.06f, 0.05f));
                c.Rect(20, y0 + 2, 34, y0 + 22, new Color(0.1f, 0.4f, 0.22f));
                for (int y = y0 + 3; y < y0 + 22; y += 3) c.Rect(20, y, 34, y, new Color(0.2f, 0.7f, 0.4f, 0.5f));
            }
            // Клавиатуры на столе
            c.Rect(4, 20, 11, 34, new Color(0.08f, 0.08f, 0.1f));
            c.Rect(4, 60, 11, 74, new Color(0.08f, 0.08f, 0.1f));
            c.Outline(Outline);
        });

        // Шкаф-гардероб (тайлится по длине): дверцы с ручками
        public static Sprite Wardrobe() => Create("wardrobe", 20, 32, c =>
        {
            c.Rect(0, 0, 19, 31, new Color(0.24f, 0.2f, 0.18f));
            c.Rect(1, 1, 18, 15, new Color(0.32f, 0.26f, 0.22f));
            c.Rect(1, 17, 18, 30, new Color(0.32f, 0.26f, 0.22f));
            c.Rect(14, 6, 15, 10, new Color(0.7f, 0.7f, 0.75f));
            c.Rect(14, 21, 15, 25, new Color(0.7f, 0.7f, 0.75f));
            c.Rect(0, 16, 19, 16, Outline);
        });

        // ---------------------------------------------------------------- Стекло

        // Целое стекло в проёме: тонкая бирюзовая рама, блики
        public static Sprite WindowPane() => Create("window_pane", 32, 20, c =>
        {
            c.Rect(0, 0, 31, 19, new Color(0.35f, 0.85f, 1f, 0.3f));
            c.Rect(0, 0, 31, 2, new Color(0.65f, 0.95f, 1f, 0.95f));
            c.Rect(0, 17, 31, 19, new Color(0.65f, 0.95f, 1f, 0.95f));
            c.Line(new Vector2(5, 4), new Vector2(11, 15), 1.5f, new Color(1f, 1f, 1f, 0.45f));
            c.Line(new Vector2(19, 4), new Vector2(22, 10), 1f, new Color(1f, 1f, 1f, 0.3f));
        });

        // Разбитое: от рамы остались зубья осколков
        public static Sprite WindowBroken() => Create("window_broken", 32, 20, c =>
        {
            Color frame = new Color(0.65f, 0.95f, 1f, 0.95f);
            Color shard = new Color(0.5f, 0.9f, 1f, 0.55f);
            c.Rect(0, 0, 31, 1, frame);
            c.Rect(0, 18, 31, 19, frame);
            for (int x = 0; x < 32; x++)
            {
                int bottom = 1 + (int)(Hash(x, 7) * 5f);
                int top = 18 - (int)(Hash(x, 13) * 5f);
                c.Rect(x, 2, x, bottom, shard);
                c.Rect(x, top, x, 17, shard);
            }
        });

        // Осколки на полу (тайлится): редкие блестящие треугольники на прозрачном
        public static Sprite GlassShards() => Create("glass_shards", 32, 32, c =>
        {
            for (int i = 0; i < 14; i++)
            {
                float x = Hash(i, 3) * 29f + 1.5f;
                float y = Hash(i, 9) * 29f + 1.5f;
                Vector2 d = new Vector2(Hash(i, 17) - 0.5f, Hash(i, 23) - 0.5f).normalized * (1.2f + Hash(i, 29) * 2.2f);
                c.Line(new Vector2(x, y) - d, new Vector2(x, y) + d, 1f, new Color(0.6f, 0.95f, 1f, 0.8f));
            }
        });
    }
}
