using UnityEngine;

namespace N89.EditorTools
{
    // Временный арт подвала миссии 2: Босс (огромный, в имплантах: пулемёт вместо правой руки, ракетница
    // на левой, ранец с лентой за спиной), колонны, бетонные укрытия, обломки, газовые баллоны, ракета, лампы.
    // Настоящий арт — заменить PNG в Generated с теми же именами.
    internal static partial class SpriteFactory
    {
        private static readonly Color BruteSkin = new Color(0.55f, 0.38f, 0.3f);
        private static readonly Color BruteSkinShade = new Color(0.42f, 0.28f, 0.22f);
        private static readonly Color BruteTattoo = new Color(0.16f, 0.1f, 0.1f);
        private static readonly Color BruteSteel = new Color(0.3f, 0.32f, 0.36f);
        private static readonly Color BruteSteelDark = new Color(0.16f, 0.17f, 0.2f);
        private static readonly Color BruteSteelLight = new Color(0.55f, 0.58f, 0.64f);
        private static readonly Color BruteNeon = new Color(1f, 0.3f, 0.15f);
        private static readonly Color BrutePants = new Color(0.27f, 0.3f, 0.2f);
        private static readonly Color CyberGlow = new Color(0.25f, 0.9f, 1f);

        // Вид сверху, смотрит вправо (+x), 2 x 2 юнита. Правая рука (снизу) — пулемёт, ствол на (62, 12);
        // левая (сверху) — ракетница, срез на (58, 51); за спиной ранец с лентой
        public static Sprite Boss() => Create("boss", 64, 64, c =>
        {
            DrawBossBody(c, true);
            c.Outline(Outline);
        });

        // Повержен: стоит на коленях, оружие опущено, голова свесилась вперёд
        public static Sprite BossDown() => Create("boss_down", 64, 64, c =>
        {
            DrawBossPack(c);
            // Руки повисли вдоль тела, оружие упирается в пол
            c.Line(new Vector2(30f, 15f), new Vector2(40f, 6f), 6f, BruteSteel);
            c.RoundRect(38, 1, 54, 9, 2, BruteSteelDark);
            c.Line(new Vector2(30f, 49f), new Vector2(40f, 57f), 6f, BruteSkinShade);
            c.RoundRect(38, 54, 52, 62, 2, BruteSteelDark);
            DrawBossTorso(c);
            // Голова опущена — видна макушка с татуировкой
            c.Circle(37f, 32f, 6.5f, BruteSkinShade);
            c.Line(new Vector2(32f, 35f), new Vector2(42f, 37f), 1.4f, BruteTattoo);
            c.RoundRect(31, 26, 43, 31, 2, BruteSteelDark);
            c.Rect(33, 28, 33, 28, BruteSteelLight);
            c.Rect(37, 28, 37, 28, BruteSteelLight);
            c.Rect(42, 29, 43, 30, new Color(0.45f, 0.08f, 0.05f));
            c.Line(new Vector2(23f, 32f), new Vector2(30f, 32f), 1f, new Color(0.1f, 0.35f, 0.4f));
            c.Outline(Outline);
        });

        // Лежит на спине, голова — +x: руки раскинуты, пулемёт отброшен в сторону
        public static Sprite BossDead() => Create("boss_dead", 96, 64, c =>
        {
            c.Line(new Vector2(36f, 26f), new Vector2(10f, 20f), 8f, BrutePants);
            c.Line(new Vector2(36f, 38f), new Vector2(12f, 46f), 8f, BrutePants);
            c.Circle(8f, 19f, 4f, new Color(0.06f, 0.06f, 0.07f));
            c.Circle(10f, 47f, 4f, new Color(0.06f, 0.06f, 0.07f));
            c.Line(new Vector2(36f, 32f), new Vector2(66f, 32f), 22f, BruteSkin);
            c.Line(new Vector2(40f, 24f), new Vector2(62f, 40f), 3f, new Color(0.12f, 0.1f, 0.08f));
            for (int i = 0; i < 4; i++) c.Line(new Vector2(44f + i * 5, 32f), new Vector2(46f + i * 5, 42f), 1.4f, BruteTattoo);
            // Броня правой половины торса и погасшие швы имплантов
            c.RoundRect(38, 21, 66, 31, 3, BruteSteel);
            c.Line(new Vector2(40f, 30f), new Vector2(64f, 30f), 1f, new Color(0.1f, 0.35f, 0.4f));
            c.Line(new Vector2(48f, 21f), new Vector2(48f, 31f), 1f, BruteSteelDark);
            c.Line(new Vector2(56f, 21f), new Vector2(56f, 31f), 1f, BruteSteelDark);
            c.Line(new Vector2(62f, 44f), new Vector2(74f, 58f), 7f, BruteSkinShade);
            c.RoundRect(70, 52, 88, 62, 2, BruteSteelDark);
            c.Line(new Vector2(62f, 20f), new Vector2(70f, 8f), 7f, BruteSteel);
            c.RoundRect(74, 1, 94, 11, 2, BruteSteelDark);
            for (int i = 0; i < 3; i++) c.Rect(78, 3 + i * 3, 94, 3 + i * 3, BruteSteelLight);
            c.Circle(76f, 32f, 8f, BruteSkin);
            c.Line(new Vector2(70f, 34f), new Vector2(82f, 37f), 1.6f, BruteTattoo);
            c.RoundRect(69, 25, 83, 31, 2, BruteSteel);
            c.Rect(72, 27, 72, 27, BruteSteelLight);
            c.Rect(81, 28, 82, 29, new Color(0.4f, 0.06f, 0.04f));
            c.Outline(Outline);
        });

        private static void DrawBossBody(PixelCanvas c, bool eyeLit)
        {
            DrawBossPack(c);
            // Правая рука — целиком механическая: плечевой шарнир, сегменты с поршнем, локоть-сфера, пулемёт
            c.Line(new Vector2(30f, 14f), new Vector2(38f, 12f), 6f, BruteSteelDark);
            c.Line(new Vector2(30f, 15f), new Vector2(38f, 13f), 1.4f, CyberGlow);
            c.Circle(39f, 12f, 3.6f, BruteSteel);
            c.Circle(39f, 12f, 1.4f, CyberGlow);
            c.Line(new Vector2(39f, 12f), new Vector2(45f, 12f), 5f, BruteSteel);
            c.Line(new Vector2(33f, 9f), new Vector2(44f, 8f), 1.2f, BruteSteelLight);
            c.RoundRect(43, 5, 62, 19, 3, BruteSteelDark);
            for (int i = 0; i < 3; i++) c.Rect(47, 8 + i * 4, 63, 9 + i * 4, BruteSteelLight);
            c.Rect(43, 5, 46, 19, BruteSteel);
            c.Rect(44, 7, 45, 17, CyberGlow);
            c.Rect(60, 11, 61, 13, BruteNeon);
            // Левая рука — живой татуированный бицепс, ниже локтя — стальное предплечье-ракетница
            c.Line(new Vector2(30f, 50f), new Vector2(39f, 52f), 7f, BruteSkin);
            c.Line(new Vector2(31f, 52f), new Vector2(38f, 53.5f), 2f, BruteSkinShade);
            c.Line(new Vector2(32f, 50f), new Vector2(36f, 51f), 1.4f, BruteTattoo);
            c.Line(new Vector2(36f, 49f), new Vector2(40f, 50f), 1f, CyberGlow);
            c.Ring(40f, 51f, 3f, 4f, BruteSteelLight);
            c.RoundRect(40, 44, 58, 58, 3, BruteSteel);
            c.Rect(41, 45, 57, 46, BruteSteelLight);
            c.Ring(55f, 48.5f, 1.2f, 2.6f, BruteSteelDark);
            c.Ring(55f, 54f, 1.2f, 2.6f, BruteSteelDark);
            c.Circle(55f, 48.5f, 1.2f, BruteNeon);
            c.Circle(55f, 54f, 1.2f, BruteNeon);
            c.Rect(42, 50, 50, 51, new Color(1f, 0.55f, 0.15f));
            DrawBossTorso(c);
            DrawBossHead(c, eyeLit);
        }

        // Лысая голова: левая половина — кожа в татуировках, правая — стальная черепная пластина с заклёпками,
        // большой красный кибер-глаз, железная челюсть, кабели от затылка к позвоночнику
        private static void DrawBossHead(PixelCanvas c, bool eyeLit)
        {
            c.Line(new Vector2(22f, 29f), new Vector2(29f, 30f), 1.6f, BruteSteelDark);
            c.Line(new Vector2(22f, 35f), new Vector2(29f, 34f), 1.6f, BruteSteelDark);
            c.Line(new Vector2(23f, 32f), new Vector2(28f, 32f), 1f, CyberGlow);
            c.Circle(34f, 32f, 7f, BruteSkin);
            c.Line(new Vector2(28f, 36f), new Vector2(36f, 38.5f), 1.6f, BruteTattoo);
            c.Line(new Vector2(29f, 33.5f), new Vector2(35f, 35f), 1f, BruteTattoo);
            // Стальная половина черепа (правая сторона — к пулемёту)
            for (int x = 27; x <= 41; x++)
            for (int y = 25; y <= 31; y++)
            {
                float dx = x + 0.5f - 34f, dy = y + 0.5f - 32f;
                if (dx * dx + dy * dy <= 49f) c.Rect(x, y, x, y, BruteSteel);
            }
            c.Line(new Vector2(28f, 31.5f), new Vector2(40f, 31.5f), 1f, BruteSteelDark);
            c.Rect(30, 27, 30, 27, BruteSteelLight);
            c.Rect(34, 26, 34, 26, BruteSteelLight);
            c.Rect(32, 29, 32, 29, CyberGlow);
            // Нижняя челюсть-протез и глаза: живой слева, кибер-глаз справа со свечением
            c.Rect(39, 31, 42, 34, BruteSteelDark);
            c.Rect(41, 32, 42, 33, BruteSteelLight);
            c.Rect(38, 34, 39, 35, BruteTattoo);
            Color eye = eyeLit ? new Color(1f, 0.15f, 0.1f) : new Color(0.4f, 0.06f, 0.04f);
            c.Circle(38.5f, 28.5f, 2f, BruteSteelDark);
            c.Circle(38.5f, 28.5f, 1.3f, eye);
            if (eyeLit) c.Rect(40, 28, 41, 28, new Color(1f, 0.45f, 0.35f));
        }

        // Позвоночный имплант вместо ранца: стальные позвонки вдоль спины, светящиеся разъёмы, барабан с лентой к пулемёту
        private static void DrawBossPack(PixelCanvas c)
        {
            c.RoundRect(9, 20, 18, 30, 3, BruteSteelDark);
            c.Circle(13.5f, 25f, 3f, BruteSteel);
            c.Circle(13.5f, 25f, 1.2f, new Color(1f, 0.55f, 0.15f));
            c.Line(new Vector2(13f, 20f), new Vector2(24f, 9f), 2.6f, new Color(0.65f, 0.55f, 0.2f));
            c.Line(new Vector2(24f, 9f), new Vector2(40f, 8f), 2.6f, new Color(0.65f, 0.55f, 0.2f));
            for (int i = 0; i < 6; i++)
            {
                int y = 17 + i * 6;
                c.RoundRect(16, y, 21, y + 4, 1, BruteSteel);
                c.Rect(18, y + 2, 19, y + 2, CyberGlow);
            }
        }

        // Широкие плечи: правое — массивный стальной наплечник с болтами, левое — живое, татуированное;
        // по коже грудной клетки — светящиеся швы имплантов и стальные рёбра-пластины
        private static void DrawBossTorso(PixelCanvas c)
        {
            c.Line(new Vector2(29f, 52f), new Vector2(29f, 12f), 20f, BruteSkin);
            c.Line(new Vector2(21f, 50f), new Vector2(21f, 14f), 3f, BruteSkinShade);
            // Правая половина торса затянута бронёй
            c.RoundRect(20, 10, 39, 30, 4, BruteSteel);
            c.Line(new Vector2(22f, 18f), new Vector2(38f, 18f), 1f, BruteSteelDark);
            c.Line(new Vector2(22f, 24f), new Vector2(38f, 24f), 1f, BruteSteelDark);
            c.Line(new Vector2(22f, 29f), new Vector2(38f, 29f), 1.4f, CyberGlow);
            // Татуировки на живой половине и швы, где железо вживлено в кожу
            for (int i = 0; i < 3; i++)
            {
                float y = 35f + i * 5f;
                c.Line(new Vector2(25f, y), new Vector2(36f, y + 2f), 1.4f, BruteTattoo);
            }
            c.Line(new Vector2(23f, 33f), new Vector2(23f, 46f), 1f, CyberGlow);
            c.Rect(36, 41, 38, 46, BruteSteelLight);
            // Наплечники
            c.RoundRect(21, 4, 37, 15, 3, BruteSteelDark);
            c.RoundRect(23, 6, 35, 13, 2, BruteSteel);
            c.Rect(25, 9, 33, 10, BruteNeon);
            c.Rect(23, 6, 23, 6, BruteSteelLight);
            c.Rect(35, 6, 35, 6, BruteSteelLight);
            c.RoundRect(24, 51, 34, 57, 2, BruteSkin);
            c.Line(new Vector2(26f, 53f), new Vector2(33f, 55f), 1.4f, BruteTattoo);
            c.Rect(24, 51, 34, 52, BruteSteelLight);
        }

        // ---------------------------------------------------------------- Арена

        // Несущая колонна сверху: квадратное основание с фаской и жёлтой разметкой, круглый ствол с обручами
        public static Sprite Pillar() => Create("pillar", 64, 64, c =>
        {
            c.RoundRect(1, 1, 62, 62, 5, new Color(0.24f, 0.24f, 0.27f));
            c.RoundRect(4, 4, 59, 59, 4, new Color(0.32f, 0.32f, 0.35f));
            for (int i = 0; i < 64; i += 8)
            {
                c.Rect(i, 1, i + 3, 3, new Color(0.95f, 0.75f, 0.15f));
                c.Rect(i, 60, i + 3, 62, new Color(0.95f, 0.75f, 0.15f));
            }
            c.Circle(31.5f, 31.5f, 23f, new Color(0.42f, 0.42f, 0.45f));
            c.Ring(31.5f, 31.5f, 19f, 21f, new Color(0.25f, 0.25f, 0.28f));
            c.Ring(31.5f, 31.5f, 12f, 13.5f, new Color(0.3f, 0.3f, 0.33f));
            c.Circle(26f, 37f, 5f, new Color(0.5f, 0.5f, 0.53f));
            c.Outline(Outline);
        });

        // Разрушенная колонна: основание в трещинах, обломок ствола, куски бетона
        public static Sprite PillarBroken() => Create("pillar_broken", 64, 64, c =>
        {
            c.RoundRect(1, 1, 62, 62, 5, new Color(0.2f, 0.2f, 0.22f));
            c.Circle(31.5f, 31.5f, 14f, new Color(0.35f, 0.35f, 0.37f));
            c.Circle(31.5f, 31.5f, 9f, new Color(0.18f, 0.18f, 0.2f));
            c.Line(new Vector2(10f, 8f), new Vector2(28f, 26f), 1.4f, new Color(0.08f, 0.08f, 0.1f));
            c.Line(new Vector2(50f, 12f), new Vector2(38f, 28f), 1.4f, new Color(0.08f, 0.08f, 0.1f));
            c.Line(new Vector2(44f, 56f), new Vector2(36f, 40f), 1.4f, new Color(0.08f, 0.08f, 0.1f));
            foreach (Vector2 p in new[] { new Vector2(12f, 46f), new Vector2(50f, 44f), new Vector2(18f, 16f), new Vector2(46f, 20f) })
            {
                c.Circle(p.x, p.y, 4f, new Color(0.38f, 0.38f, 0.4f));
            }
            c.Outline(Outline);
        });

        // Бетонное укрытие (тянется по длине): плиты, жёлто-чёрная полоса по верху
        public static Sprite Barrier() => Create("barrier", 96, 24, c =>
        {
            c.RoundRect(0, 0, 95, 23, 5, new Color(0.3f, 0.3f, 0.33f));
            c.RoundRect(3, 3, 92, 20, 4, new Color(0.38f, 0.38f, 0.41f));
            for (int x = 6; x < 92; x += 6)
            {
                c.Rect(x, 10, x + 2, 13, (x / 6) % 2 == 0 ? new Color(0.95f, 0.75f, 0.15f) : new Color(0.08f, 0.08f, 0.1f));
            }
            c.Rect(32, 3, 32, 20, new Color(0.25f, 0.25f, 0.28f));
            c.Rect(64, 3, 64, 20, new Color(0.25f, 0.25f, 0.28f));
            c.Outline(Outline);
        });

        // Зона обрушения на полу: красное кольцо с засечками и трещины к центру (цвет и мигание — в BossFight)
        public static Sprite CollapseZone() => Create("collapse_zone", 64, 64, c =>
        {
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Vector2 d = new Vector2(x - 31.5f, y - 31.5f);
                float r = d.magnitude;
                if (r > 31.5f) continue;
                float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 180f;
                bool edge = r > 28.5f && Mathf.Repeat(angle, 20f) < 13f;
                c.Replace(x, y, edge ? Color.white : new Color(1f, 1f, 1f, 0.22f));
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad + 0.3f;
                c.Line(new Vector2(31.5f, 31.5f), new Vector2(31.5f + Mathf.Cos(a) * 24f, 31.5f + Mathf.Sin(a) * 24f), 1f, new Color(1f, 1f, 1f, 0.5f));
            }
        });

        public static Sprite Rubble(string name, int seed) => Create(name, 32, 32, c =>
        {
            for (int i = 0; i < 7; i++)
            {
                float x = 6f + Hash(i, seed) * 20f;
                float y = 6f + Hash(i + 7, seed) * 20f;
                float r = 2.5f + Hash(i + 13, seed) * 4f;
                float shade = 0.3f + Hash(i + 21, seed) * 0.15f;
                c.Circle(x, y, r, new Color(shade, shade, shade + 0.02f));
            }
            c.Line(new Vector2(6f, 24f), new Vector2(18f, 20f), 1.4f, new Color(0.45f, 0.3f, 0.2f));
            c.Outline(Outline);
        });

        // Газовый баллон сверху: красный корпус, жёлтая полоса, вентиль
        public static Sprite GasCylinder() => Create("gas_cylinder", 20, 20, c =>
        {
            c.Circle(9.5f, 9.5f, 8.5f, new Color(0.75f, 0.12f, 0.1f));
            c.Ring(9.5f, 9.5f, 5.5f, 7f, new Color(0.95f, 0.75f, 0.15f));
            c.Circle(9.5f, 9.5f, 3f, BruteSteel);
            c.Rect(8, 8, 11, 11, BruteSteelLight);
            c.Rect(5, 13, 6, 14, Color.white);
            c.Outline(Outline);
        });

        public static Sprite SoftShadow() => Create("soft_shadow", 24, 24, c =>
        {
            for (int y = 0; y < 24; y++)
            for (int x = 0; x < 24; x++)
            {
                float r = new Vector2(x - 11.5f, y - 11.5f).magnitude / 11.5f;
                if (r < 1f) c.Replace(x, y, new Color(1f, 1f, 1f, 1f - r * r));
            }
        });

        // Ракета: корпус, красная боеголовка, стабилизаторы
        public static Sprite Rocket() => Create("rocket", 16, 6, c =>
        {
            c.Rect(2, 2, 12, 3, BruteSteelLight);
            c.Rect(12, 2, 15, 3, BruteNeon);
            c.Rect(0, 0, 2, 5, BruteSteelDark);
        });

        // Промышленная лампа под потолком: решётка, светящийся круг
        public static Sprite CeilingLamp() => Create("ceiling_lamp", 24, 24, c =>
        {
            c.Circle(11.5f, 11.5f, 11f, new Color(0.2f, 0.2f, 0.22f));
            c.Circle(11.5f, 11.5f, 8f, new Color(1f, 0.97f, 0.85f));
            c.Rect(3, 11, 20, 12, new Color(0.2f, 0.2f, 0.22f));
            c.Rect(11, 3, 12, 20, new Color(0.2f, 0.2f, 0.22f));
            c.Outline(Outline);
        });
    }
}
