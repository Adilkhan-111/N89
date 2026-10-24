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
            c.Line(new Vector2(32f, 30f), new Vector2(42f, 34f), 1.4f, BruteTattoo);
            c.Rect(42, 31, 43, 32, new Color(0.45f, 0.08f, 0.05f));
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
            for (int i = 0; i < 4; i++) c.Line(new Vector2(44f + i * 5, 22f), new Vector2(46f + i * 5, 42f), 1.4f, BruteTattoo);
            c.Line(new Vector2(62f, 44f), new Vector2(74f, 58f), 7f, BruteSkinShade);
            c.RoundRect(70, 52, 88, 62, 2, BruteSteelDark);
            c.Line(new Vector2(62f, 20f), new Vector2(70f, 8f), 7f, BruteSteel);
            c.RoundRect(74, 1, 94, 11, 2, BruteSteelDark);
            for (int i = 0; i < 3; i++) c.Rect(78, 3 + i * 3, 94, 3 + i * 3, BruteSteelLight);
            c.Circle(76f, 32f, 8f, BruteSkin);
            c.Line(new Vector2(70f, 30f), new Vector2(82f, 34f), 1.6f, BruteTattoo);
            c.Rect(81, 35, 82, 36, new Color(0.4f, 0.06f, 0.04f));
            c.Outline(Outline);
        });

        private static void DrawBossBody(PixelCanvas c, bool eyeLit)
        {
            DrawBossPack(c);
            // Правая рука — кибер-рука с пулемётом: шесть стволов в кожухе
            c.Line(new Vector2(31f, 15f), new Vector2(44f, 12f), 7f, BruteSteel);
            c.Line(new Vector2(33f, 13f), new Vector2(42f, 11f), 2f, BruteSteelLight);
            c.RoundRect(42, 5, 62, 19, 3, BruteSteelDark);
            for (int i = 0; i < 3; i++) c.Rect(46, 8 + i * 4, 63, 9 + i * 4, BruteSteelLight);
            c.Rect(42, 5, 45, 19, BruteSteel);
            c.Rect(60, 11, 61, 13, BruteNeon);
            // Левая рука — живая, татуированная, на предплечье ракетница с двумя трубами
            c.Line(new Vector2(31f, 49f), new Vector2(42f, 52f), 7f, BruteSkin);
            c.Line(new Vector2(34f, 51f), new Vector2(37f, 52f), 1.6f, BruteTattoo);
            c.RoundRect(40, 44, 58, 58, 3, BruteSteel);
            c.Ring(55f, 48.5f, 1.2f, 2.6f, BruteSteelDark);
            c.Ring(55f, 54f, 1.2f, 2.6f, BruteSteelDark);
            c.Circle(55f, 48.5f, 1.2f, BruteNeon);
            c.Circle(55f, 54f, 1.2f, BruteNeon);
            c.Rect(42, 50, 50, 51, new Color(1f, 0.55f, 0.15f));
            DrawBossTorso(c);
            // Лысая голова в татуировках, красный кибер-глаз
            c.Circle(34f, 32f, 7f, BruteSkin);
            c.Line(new Vector2(28f, 28f), new Vector2(36f, 25.5f), 1.6f, BruteTattoo);
            c.Line(new Vector2(28f, 36f), new Vector2(36f, 38.5f), 1.6f, BruteTattoo);
            c.Rect(38, 30, 41, 35, BruteSkinShade);
            c.Rect(39, 34, 41, 35, eyeLit ? new Color(1f, 0.15f, 0.1f) : new Color(0.4f, 0.06f, 0.04f));
            c.Rect(30, 37, 33, 39, BruteSteelLight);
        }

        // Ранец за спиной: тёмный металл, оранжевая полоса, лента к пулемёту
        private static void DrawBossPack(PixelCanvas c)
        {
            c.RoundRect(5, 17, 21, 47, 4, BruteSteelDark);
            c.RoundRect(7, 19, 19, 45, 3, BruteSteel);
            c.Rect(8, 30, 18, 33, new Color(1f, 0.55f, 0.15f));
            c.Line(new Vector2(14f, 18f), new Vector2(24f, 9f), 2.6f, new Color(0.65f, 0.55f, 0.2f));
            c.Line(new Vector2(24f, 9f), new Vector2(40f, 8f), 2.6f, new Color(0.65f, 0.55f, 0.2f));
        }

        // Широкие плечи и голая грудь в татуировках, ремень портупеи наискосок, стальные пластины на плечах
        private static void DrawBossTorso(PixelCanvas c)
        {
            c.Line(new Vector2(29f, 52f), new Vector2(29f, 12f), 20f, BruteSkin);
            c.Line(new Vector2(22f, 50f), new Vector2(22f, 14f), 4f, BruteSkinShade);
            for (int i = 0; i < 4; i++)
            {
                float y = 18f + i * 8f;
                c.Line(new Vector2(25f, y), new Vector2(36f, y + 3f), 1.6f, BruteTattoo);
            }
            c.Line(new Vector2(22f, 48f), new Vector2(37f, 16f), 2.6f, new Color(0.12f, 0.1f, 0.08f));
            c.RoundRect(23, 6, 35, 14, 2, BruteSteel);
            c.RoundRect(23, 50, 35, 58, 2, BruteSteelDark);
            c.Rect(26, 9, 32, 10, BruteNeon);
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
