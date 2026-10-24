using UnityEngine;

namespace N89.EditorTools
{
    // Временный арт логова банды: грязь и мусор внутри, закрытая база во дворе, крыши города вокруг.
    internal static partial class SpriteFactory
    {
        // ---------------------------------------------------------------- Автодверь

        // Створка раздвижной двери (тайлится вдоль проёма): тёмный металл, жёлто-чёрная разметка по краю
        public static Sprite DoorPanelSci() => Create("door_sci", 32, 20, c =>
        {
            c.Rect(0, 0, 31, 19, new Color(0.08f, 0.08f, 0.1f));
            c.Rect(1, 2, 30, 17, new Color(0.22f, 0.23f, 0.27f));
            for (int x = 1; x < 31; x++)
            {
                Color stripe = (x / 3) % 2 == 0 ? CpYellow * 0.85f : new Color(0.06f, 0.06f, 0.06f);
                c.Rect(x, 2, x, 4, stripe);
                c.Rect(x, 15, x, 17, stripe);
            }
            c.Rect(3, 8, 28, 11, new Color(0.15f, 0.15f, 0.18f));
            c.Rect(4, 9, 27, 10, new Color(0.3f, 0.32f, 0.37f));
        });

        // ---------------------------------------------------------------- Грязь и мусор (пол)

        // Мешки и банки кучей
        public static Sprite TrashPile() => Create("trash_pile", 40, 32, c =>
        {
            c.Ellipse(14f, 14f, 11f, 9f, new Color(0.06f, 0.06f, 0.07f));
            c.Ellipse(26f, 18f, 10f, 8f, new Color(0.09f, 0.09f, 0.1f));
            c.Ellipse(16f, 22f, 7f, 6f, new Color(0.12f, 0.11f, 0.1f));
            c.Line(new Vector2(10f, 18f), new Vector2(15f, 21f), 1f, new Color(0.2f, 0.2f, 0.22f));
            c.Circle(31f, 8f, 2.4f, Red);
            c.Circle(7f, 6f, 2f, new Color(0.5f, 0.5f, 0.55f));
            c.Rect(34, 23, 38, 27, new Color(0.7f, 0.6f, 0.35f));
            c.Circle(24f, 6f, 1.8f, CpYellow);
            c.Outline(Outline);
        });

        public static Sprite PizzaBox() => Create("pizza_box", 24, 24, c =>
        {
            c.Rect(2, 2, 21, 21, new Color(0.55f, 0.42f, 0.25f));
            c.Rect(4, 4, 19, 19, new Color(0.62f, 0.48f, 0.3f));
            c.Circle(12f, 12f, 6f, new Color(0.7f, 0.35f, 0.15f));
            c.Circle(10f, 13f, 1.2f, new Color(0.5f, 0.1f, 0.1f));
            c.Circle(14f, 10f, 1.2f, new Color(0.5f, 0.1f, 0.1f));
            c.Rect(11, 4, 19, 7, new Color(0.55f, 0.42f, 0.25f));
            c.Outline(Outline);
        });

        // Пустые бутылки, одна разбита
        public static Sprite Bottles() => Create("bottles", 24, 20, c =>
        {
            c.Line(new Vector2(3f, 5f), new Vector2(12f, 8f), 2.6f, new Color(0.2f, 0.45f, 0.25f));
            c.Rect(12, 7, 14, 8, new Color(0.15f, 0.3f, 0.18f));
            c.Line(new Vector2(14f, 15f), new Vector2(20f, 9f), 2.6f, new Color(0.45f, 0.28f, 0.12f));
            c.Circle(7f, 15f, 1.6f, new Color(0.7f, 0.85f, 0.9f, 0.7f));
            c.Rect(18, 16, 18, 16, new Color(0.7f, 0.85f, 0.9f));
            c.Rect(20, 17, 20, 17, new Color(0.7f, 0.85f, 0.9f));
            c.Outline(Outline);
        });

        // Окурки и пепел
        public static Sprite Butts() => Create("butts", 16, 16, c =>
        {
            c.Ellipse(8f, 8f, 6f, 4f, new Color(0.18f, 0.17f, 0.16f, 0.6f));
            c.Rect(3, 6, 6, 6, new Color(0.85f, 0.8f, 0.7f));
            c.Rect(9, 10, 12, 10, new Color(0.85f, 0.8f, 0.7f));
            c.Rect(7, 4, 7, 7, new Color(0.85f, 0.8f, 0.7f));
            c.Rect(6, 6, 6, 6, new Color(0.9f, 0.5f, 0.2f));
        });

        // Тёмное пятно масла / чего похуже
        public static Sprite OilStain() => Create("oil_stain", 48, 40, c =>
        {
            var rng = new System.Random(17);
            for (int i = 0; i < 9; i++)
            {
                c.Ellipse(14f + rng.Next(20), 12f + rng.Next(16), 6f + rng.Next(6), 4f + rng.Next(5), new Color(0.02f, 0.02f, 0.03f, 0.55f));
            }
        });

        // Неоновое граффити баллончиком на полу: тег банды
        public static Sprite Graffiti(string name, Color paint) => Create(name, 64, 40, c =>
        {
            Color drip = new Color(paint.r, paint.g, paint.b, 0.55f);
            c.Line(new Vector2(6f, 8f), new Vector2(14f, 32f), 2.5f, paint);
            c.Line(new Vector2(14f, 32f), new Vector2(20f, 10f), 2.5f, paint);
            c.Line(new Vector2(24f, 30f), new Vector2(26f, 9f), 2.5f, paint);
            c.Line(new Vector2(26f, 9f), new Vector2(36f, 14f), 2.5f, paint);
            c.Line(new Vector2(40f, 28f), new Vector2(50f, 30f), 2.5f, paint);
            c.Line(new Vector2(45f, 29f), new Vector2(44f, 8f), 2.5f, paint);
            c.Ellipse(55f, 20f, 5f, 9f, drip);
            c.Line(new Vector2(4f, 4f), new Vector2(60f, 6f), 1.5f, drip);
        });

        // Продавленный матрас с пятнами и подушкой
        public static Sprite Mattress() => Create("mattress", 56, 36, c =>
        {
            c.RoundRect(1, 1, 54, 34, 4, new Color(0.45f, 0.42f, 0.36f));
            c.RoundRect(3, 3, 52, 32, 3, new Color(0.55f, 0.52f, 0.45f));
            for (int x = 8; x < 52; x += 8) c.Rect(x, 4, x, 31, new Color(0.47f, 0.44f, 0.38f));
            c.Ellipse(20f, 14f, 7f, 5f, new Color(0.42f, 0.36f, 0.22f));
            c.Ellipse(36f, 22f, 5f, 4f, new Color(0.38f, 0.32f, 0.2f));
            c.RoundRect(42, 8, 51, 27, 3, new Color(0.7f, 0.68f, 0.62f));
            c.Outline(Outline);
        });

        // Гантели и блины на полу качалки
        public static Sprite Dumbbells() => Create("dumbbells", 32, 24, c =>
        {
            foreach (int y in new[] { 6, 16 })
            {
                c.Rect(6, y, 22, y + 1, new Color(0.6f, 0.6f, 0.65f));
                c.Rect(3, y - 3, 6, y + 4, new Color(0.08f, 0.08f, 0.1f));
                c.Rect(22, y - 3, 25, y + 4, new Color(0.08f, 0.08f, 0.1f));
            }
            c.Circle(28f, 20f, 3.5f, new Color(0.1f, 0.1f, 0.12f));
            c.Circle(28f, 20f, 1f, new Color(0.4f, 0.4f, 0.45f));
            c.Outline(Outline);
        });

        // ---------------------------------------------------------------- Закрытая база (двор)

        // Бочка с огнём внутри — греются бандиты
        public static Sprite FireBarrel() => Create("fire_barrel", 24, 24, c =>
        {
            c.Circle(11.5f, 11.5f, 10.5f, Rust);
            c.Circle(11.5f, 11.5f, 9f, new Color(0.25f, 0.13f, 0.08f));
            c.Ring(11.5f, 11.5f, 7.5f, 8.5f, new Color(0.18f, 0.1f, 0.06f));
            c.Circle(11.5f, 11.5f, 6.5f, new Color(1f, 0.45f, 0.1f));
            c.Circle(10f, 12.5f, 4f, new Color(1f, 0.75f, 0.2f));
            c.Circle(12.5f, 10f, 2f, new Color(1f, 0.95f, 0.6f));
            c.Outline(Outline);
        });

        // Баррикада из мешков с песком (горизонтальная)
        public static Sprite Sandbags() => Create("sandbags", 64, 24, c =>
        {
            Color bag = new Color(0.45f, 0.4f, 0.28f);
            Color bagShade = new Color(0.34f, 0.3f, 0.2f);
            for (int row = 0; row < 2; row++)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = 7f + i * 12.5f + (row == 1 ? 6f : 0f);
                    if (x > 58f) continue;
                    c.Ellipse(x, 7f + row * 10f, 6.5f, 5f, row == 0 ? bagShade : bag);
                    c.Line(new Vector2(x - 4f, 7f + row * 10f), new Vector2(x + 4f, 7f + row * 10f), 1f, bagShade * 0.8f);
                }
            }
            c.Outline(Outline);
        });

        // Морской контейнер сверху: гофрированная крыша, ржавчина, граффити
        public static Sprite ShippingContainer() => Create("container", 40, 96, c =>
        {
            Color paint = new Color(0.18f, 0.32f, 0.36f);
            c.Rect(0, 0, 39, 95, new Color(0.08f, 0.1f, 0.11f));
            c.Rect(2, 2, 37, 93, paint);
            for (int y = 4; y < 92; y += 4) c.Rect(3, y, 36, y, paint * 0.75f + new Color(0, 0, 0, 0.25f));
            c.Rect(5, 20, 14, 30, Rust);
            c.Rect(25, 62, 33, 70, Rust);
            c.Line(new Vector2(8f, 74f), new Vector2(30f, 86f), 2f, Magenta * 0.9f);
            c.Outline(Outline);
        });

        // Дизель-генератор с выхлопом и мигающей лампой
        public static Sprite Generator() => Create("generator", 48, 36, c =>
        {
            c.RoundRect(1, 1, 46, 34, 3, new Color(0.08f, 0.08f, 0.06f));
            c.RoundRect(3, 3, 44, 32, 2, new Color(0.45f, 0.38f, 0.12f));
            for (int x = 6; x < 26; x += 3) c.Rect(x, 8, x + 1, 27, new Color(0.25f, 0.2f, 0.06f));
            c.Circle(35f, 18f, 6f, new Color(0.15f, 0.15f, 0.15f));
            c.Circle(35f, 18f, 3f, new Color(0.3f, 0.3f, 0.3f));
            c.Rect(40, 28, 43, 31, Red);
            c.Outline(Outline);
        });

        // Прожектор на мачте (вид сверху)
        public static Sprite Floodlight() => Create("floodlight", 24, 24, c =>
        {
            c.Circle(11.5f, 11.5f, 5f, new Color(0.2f, 0.2f, 0.24f));
            c.RoundRect(10, 4, 22, 19, 2, new Color(0.12f, 0.12f, 0.15f));
            c.Rect(19, 6, 21, 17, new Color(1f, 0.97f, 0.85f));
            c.Outline(Outline);
        });

        // Табличка «ВХОД ЗАПРЕЩЁН» (латиницей — шрифтов с кириллицей в спрайтах нет)
        public static Sprite WarningSign() => Create("sign_warning", 32, 20, c =>
        {
            c.Rect(0, 0, 31, 19, new Color(0.08f, 0.08f, 0.08f));
            c.Rect(1, 1, 30, 18, CpYellow);
            c.Line(new Vector2(4f, 4f), new Vector2(9f, 15f), 2f, new Color(0.08f, 0.08f, 0.08f));
            c.Line(new Vector2(9f, 15f), new Vector2(14f, 4f), 2f, new Color(0.08f, 0.08f, 0.08f));
            c.Rect(8, 7, 10, 11, Red);
            for (int x = 17; x < 29; x += 3) c.Rect(x, 6, x + 1, 13, new Color(0.08f, 0.08f, 0.08f));
        });

        // Забор: сетка-рабица на столбах и спираль колючей проволоки поверху (тайлится)
        public static Sprite Fence() => Create("fence", 32, 20, c =>
        {
            Color metal = new Color(0.3f, 0.31f, 0.34f);
            for (int x = 0; x < 32; x++)
            for (int y = 3; y < 17; y++)
            {
                if ((x + y) % 4 == 0 || (x - y + 32) % 4 == 0) c.Set(x, y, metal * 0.8f + new Color(0, 0, 0, 0.2f));
            }
            c.Rect(0, 1, 2, 18, new Color(0.18f, 0.18f, 0.2f));
            c.Rect(0, 2, 31, 3, metal);
            c.Rect(0, 16, 31, 17, metal);
            // Колючая спираль
            for (int x = 0; x < 32; x += 4)
            {
                c.Ring(x + 2f, 10f, 2.5f, 3.4f, new Color(0.62f, 0.62f, 0.66f));
                c.Set(x, 13, Color.white);
            }
        });

        // ---------------------------------------------------------------- Трупы

        // Лежит лицом вниз, головой вправо (+x): ноги раскинуты, одна рука выброшена вперёд, ствол выпал рядом
        private static void DrawCorpse(PixelCanvas c, Color jacket, Color shade, Color pants, Color skin, Color head)
        {
            Color boots = new Color(0.06f, 0.06f, 0.07f);
            c.Line(new Vector2(19f, 14f), new Vector2(7f, 10.5f), 3.4f, pants);
            c.Line(new Vector2(19f, 18f), new Vector2(12.5f, 23f), 3.4f, pants);
            c.Line(new Vector2(12.5f, 23f), new Vector2(6.5f, 22f), 3.2f, pants);
            c.Circle(6f, 10.2f, 2f, boots);
            c.Circle(5.5f, 22f, 2f, boots);
            c.Line(new Vector2(18f, 16f), new Vector2(30f, 16f), 9.5f, jacket);
            c.Line(new Vector2(18f, 12.3f), new Vector2(30f, 12.3f), 2f, shade);
            c.Line(new Vector2(29f, 20.5f), new Vector2(37f, 27f), 2.8f, jacket);
            c.Circle(38f, 28f, 2f, skin);
            c.Line(new Vector2(29f, 11.5f), new Vector2(23f, 5.5f), 2.8f, shade);
            c.Circle(22f, 4.6f, 2f, skin);
            c.Circle(35f, 16f, 4.6f, head);
            // Выпавший ствол
            c.Rect(40, 6, 47, 8, Gun);
            c.Rect(40, 4, 42, 5, Gun);
        }

        public static Sprite BanditDead() => Create("bandit_dead", 48, 32, c =>
        {
            Color toxic = new Color(0.45f, 1f, 0.3f);
            DrawCorpse(c, new Color(0.2f, 0.23f, 0.17f), new Color(0.12f, 0.14f, 0.1f), new Color(0.13f, 0.13f, 0.14f),
                new Color(0.62f, 0.45f, 0.35f), new Color(0.09f, 0.08f, 0.07f));
            // Ирокез вдоль затылка и бандана на плече
            c.Rect(32, 16, 39, 16, toxic);
            c.Rect(24, 19, 26, 20, toxic);
            c.Outline(Outline);
        });

        public static Sprite GuardDead() => Create("guard_dead", 48, 32, c =>
        {
            DrawCorpse(c, new Color(0.22f, 0.24f, 0.32f), new Color(0.13f, 0.14f, 0.2f), new Color(0.1f, 0.1f, 0.13f),
                new Color(0.1f, 0.1f, 0.12f), new Color(0.11f, 0.11f, 0.15f));
            // Нашивка корпорации и красный визор сбоку шлема
            c.Rect(24, 19, 26, 20, new Color(1f, 0.45f, 0.1f));
            c.Rect(38, 13, 39, 15, Red);
            c.Outline(Outline);
        });

        // Рэй: бордовая куртка, чёрные волосы, неоновый имплант на виске
        public static Sprite HeroDead() => Create("hero_dead", 48, 32, c =>
        {
            DrawCorpse(c, HeroJacket, HeroJacketShade, HeroPants, HeroSkin, HeroHair);
            c.Circle(34f, 17.5f, 1.6f, HeroHairLight);
            c.Rect(33, 12, 35, 12, Magenta);
            c.Outline(Outline);
        });

        // ---------------------------------------------------------------- Город вокруг

        // Крыша мегаблока (тайлится): бетонные плиты, швы, грязь
        public static Sprite RoofTile(string name, Color tint) => Create(name, 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float n = Hash(x + 3, y + 101) * 0.05f + Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.05f;
                Color col = new Color(tint.r + n, tint.g + n, tint.b + n);
                if (x % 16 == 0 || y % 16 == 0) col = tint * 0.6f + new Color(0, 0, 0, 0.4f);
                c.Replace(x, y, col);
            }
        });

        // Кондиционер / вентблок на крыше
        public static Sprite RoofUnit() => Create("roof_unit", 24, 24, c =>
        {
            c.RoundRect(1, 1, 22, 22, 2, new Color(0.08f, 0.08f, 0.1f));
            c.RoundRect(3, 3, 20, 20, 2, new Color(0.32f, 0.33f, 0.37f));
            c.Circle(11.5f, 11.5f, 6f, new Color(0.12f, 0.12f, 0.14f));
            c.Line(new Vector2(7f, 11.5f), new Vector2(16f, 11.5f), 1f, new Color(0.4f, 0.4f, 0.45f));
            c.Line(new Vector2(11.5f, 7f), new Vector2(11.5f, 16f), 1f, new Color(0.4f, 0.4f, 0.45f));
            c.Outline(Outline);
        });

        // Неоновая вывеска на крыше: светящиеся трубки «иероглифами» (цвет задаётся SpriteRenderer)
        public static Sprite RoofNeon() => Create("roof_neon", 64, 16, c =>
        {
            c.Rect(0, 0, 63, 15, new Color(0.05f, 0.05f, 0.06f));
            Color tube = Color.white;
            for (int i = 0; i < 6; i++)
            {
                int x = 4 + i * 10;
                c.Rect(x, 3, x + 1, 12, tube);
                c.Rect(x, 12, x + 6, 13, tube);
                if (i % 2 == 0) c.Rect(x + 5, 3, x + 6, 8, tube);
                else c.Rect(x + 2, 7, x + 6, 8, tube);
            }
        });
    }
}
