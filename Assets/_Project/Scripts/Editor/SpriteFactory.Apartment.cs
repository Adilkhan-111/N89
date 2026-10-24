using UnityEngine;

namespace N89.EditorTools
{
    // Временный арт для интерлюдий: главный герой (не 89!) и его квартира.
    internal static partial class SpriteFactory
    {
        // Герой — временный вид: бордовая куртка, чёрные растрёпанные волосы, неоновый имплант на виске
        private static readonly Color HeroJacket = new Color(0.46f, 0.11f, 0.16f);
        private static readonly Color HeroJacketShade = new Color(0.3f, 0.07f, 0.11f);
        private static readonly Color HeroShirt = new Color(0.17f, 0.17f, 0.21f);
        private static readonly Color HeroPants = new Color(0.16f, 0.17f, 0.26f);
        private static readonly Color HeroBoots = new Color(0.06f, 0.06f, 0.08f);
        private static readonly Color HeroSkin = new Color(0.8f, 0.62f, 0.52f);
        private static readonly Color HeroSkinShade = new Color(0.64f, 0.47f, 0.39f);
        private static readonly Color HeroFaceLine = new Color(0.3f, 0.19f, 0.17f);
        private static readonly Color HeroHair = new Color(0.07f, 0.06f, 0.09f);
        private static readonly Color HeroHairLight = new Color(0.2f, 0.18f, 0.25f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.8f);

        // Стоит без оружия, вид сверху, смотрит вправо (+x): руки опущены, кисти чуть впереди плеч
        public static Sprite Hero() => Create("hero", 32, 32, c =>
        {
            DrawHeroTorso(c, 14f);
            c.Circle(17.5f, 25.5f, 1.6f, HeroSkin);
            c.Circle(17.5f, 6.5f, 1.6f, HeroSkinShade);
            c.Outline(Outline);
        });

        // Спит на спине, головой влево (-x): видно лицо с закрытыми глазами, одна рука на груди,
        // другая свесилась вдоль тела
        public static Sprite HeroSleeping() => Create("hero_sleeping", 72, 32, c =>
        {
            // Ноги и ботинки
            c.Line(new Vector2(38f, 13f), new Vector2(59f, 12f), 5f, HeroPants);
            c.Line(new Vector2(38f, 19f), new Vector2(58f, 21f), 5f, HeroPants);
            c.Ellipse(61.5f, 12f, 2.4f, 2.8f, HeroBoots);
            c.Ellipse(60.5f, 21f, 2.4f, 2.8f, HeroBoots);
            // Корпус: распахнутая куртка, под ней футболка
            c.Line(new Vector2(16f, 16f), new Vector2(38f, 16f), 13f, HeroJacket);
            c.Ellipse(17.5f, 16f, 4f, 9.5f, HeroJacket);
            c.Line(new Vector2(20f, 16f), new Vector2(37f, 16f), 4f, HeroShirt);
            c.Line(new Vector2(17f, 9.5f), new Vector2(37f, 10.5f), 2f, HeroJacketShade);
            // Рука вдоль тела
            c.Line(new Vector2(18f, 7f), new Vector2(31f, 5.5f), 3.2f, HeroJacketShade);
            c.Circle(33f, 5.5f, 1.7f, HeroSkinShade);
            // Рука на груди
            c.Line(new Vector2(18f, 25f), new Vector2(26f, 24f), 3.2f, HeroJacket);
            c.Line(new Vector2(26f, 24f), new Vector2(29f, 18.5f), 3.2f, HeroJacket);
            c.Circle(29.5f, 17f, 1.7f, HeroSkin);
            // Голова: волосы к макушке (влево), лицо вверх, закрытые глаза, имплант на виске
            c.Circle(8.5f, 16f, 4.6f, HeroHair);
            c.Rect(5, 17, 6, 18, HeroHairLight);
            c.Ellipse(10.4f, 16f, 3.1f, 3.5f, HeroSkin);
            c.Rect(8, 14, 9, 18, HeroHair);
            c.Rect(10, 13, 10, 14, HeroFaceLine);
            c.Rect(10, 18, 10, 19, HeroFaceLine);
            c.Rect(12, 16, 12, 16, HeroSkinShade);
            c.Rect(10, 12, 11, 12, Magenta);
            c.Outline(Outline);
        });

        // Сидит на диване, вид сверху, смотрит вправо (+x): бёдра вытянуты вперёд, локти на коленях
        public static Sprite HeroSitting() => Create("hero_sitting", 48, 32, c =>
        {
            c.Line(new Vector2(17f, 12.5f), new Vector2(31f, 12f), 5f, HeroPants);
            c.Line(new Vector2(17f, 19.5f), new Vector2(31f, 20f), 5f, HeroPants);
            c.Ellipse(34f, 12f, 2.6f, 2.8f, HeroBoots);
            c.Ellipse(34f, 20f, 2.6f, 2.8f, HeroBoots);
            DrawHeroTorso(c, 13f);
            c.Line(new Vector2(14f, 25f), new Vector2(25f, 22.5f), 3f, HeroJacket);
            c.Line(new Vector2(14f, 7f), new Vector2(25f, 9.5f), 3f, HeroJacketShade);
            c.Circle(26.5f, 22f, 1.7f, HeroSkin);
            c.Circle(26.5f, 10f, 1.7f, HeroSkinShade);
            c.Outline(Outline);
        });

        // Рэй в миссии: боевая стойка с выбранным в арсенале оружием (вид сверху, ствол вправо)
        public static Sprite HeroSmg() => Create("hero_smg", 32, 32, c => DrawHeroArmed(c, gun =>
        {
            gun.Rect(21, 15, 29, 16, GunDark);
            gun.Rect(22, 14, 26, 17, GunMid);
            gun.Rect(23, 16, 24, 16, Red);
            gun.Rect(29, 15, 31, 16, GunEdge);
        }));

        public static Sprite HeroRifle() => Create("hero_rifle", 32, 32, c => DrawHeroArmed(c, gun =>
        {
            gun.Rect(19, 15, 31, 16, GunDark);
            gun.Rect(24, 14, 29, 17, new Color(0.55f, 0.37f, 0.22f));
            gun.Rect(25, 16, 28, 16, new Color(0.7f, 0.5f, 0.3f));
            gun.Rect(30, 15, 31, 16, GunEdge);
        }));

        public static Sprite HeroShotgun() => Create("hero_shotgun", 32, 32, c => DrawHeroArmed(c, gun =>
        {
            gun.Rect(18, 15, 31, 16, new Color(0.2f, 0.2f, 0.24f));
            gun.Rect(20, 16, 30, 16, GunEdge);
            gun.Rect(24, 14, 28, 17, GunDark);
            gun.Rect(31, 14, 31, 17, GunDark);
        }));

        private static void DrawHeroArmed(PixelCanvas c, System.Action<PixelCanvas> drawGun)
        {
            DrawShooter(c, HeroJacket, HeroJacketShade, HeroSkin, HeroSkinShade, HeroHair, HeroSkin, HeroHairLight, drawGun);
            c.Circle(10.8f, 17.4f, 1.6f, HeroHairLight);
            c.Rect(12, 12, 14, 12, Magenta);
        }

        // Ноги для анимации ходьбы (как в Hotline Miami — отдельный спрайт под корпусом), смотрят вправо (+x).
        // stride > 0 — левая нога (сверху) впереди, < 0 — правая; 0 — ноги вместе
        public static Sprite HeroLegs(string name, float stride) => Create(name, 32, 32, c =>
        {
            DrawLeg(c, new Vector2(14f, 19.5f), new Vector2(15f + stride, 20.5f), 4.5f, HeroPants, HeroBoots, 3f, 2.2f);
            DrawLeg(c, new Vector2(14f, 12.5f), new Vector2(15f - stride, 11.5f), 4.5f, HeroPants, HeroBoots, 3f, 2.2f);
            c.Outline(Outline);
        });

        // Нога сверху: штанина от бедра к стопе, ботинок носком вперёд (+x) с бликом
        private static void DrawLeg(PixelCanvas c, Vector2 hip, Vector2 foot, float thickness, Color pants, Color boots, float bootLength, float bootWidth)
        {
            c.Line(hip, foot, thickness, pants);
            c.Ellipse(foot.x + bootLength * 0.5f, foot.y, bootLength, bootWidth, boots);
            int tipX = Mathf.RoundToInt(foot.x + bootLength);
            int tipY = Mathf.FloorToInt(foot.y);
            c.Rect(tipX, tipY, tipX, tipY, new Color(0.24f, 0.24f, 0.3f));
        }

        // Плечи в куртке с поднятым воротником и голова сверху; x — линия плеч
        private static void DrawHeroTorso(PixelCanvas c, float x)
        {
            Vector2 left = new Vector2(x, 24f);
            Vector2 right = new Vector2(x, 8f);
            c.Line(left, right, 7f, HeroJacket);
            c.Line(left + new Vector2(-2f, 0f), right + new Vector2(-2f, 0f), 2.5f, HeroJacketShade);
            c.Circle(x, 24.5f, 3.2f, HeroJacket);
            c.Circle(x, 7.5f, 3.2f, HeroJacketShade);
            c.Circle(x, 16f, 5f, HeroJacketShade);
            c.Circle(x, 16f, 4.2f, HeroHair);
            c.Circle(x - 0.6f, 17.2f, 2f, HeroHairLight);
            c.Rect((int)x - 4, 18, (int)x - 4, 18, HeroHair);
            c.Rect((int)x - 5, 15, (int)x - 5, 15, HeroHair);
            c.Rect((int)x + 4, 15, (int)x + 4, 17, HeroSkin);
            c.Rect((int)x + 1, 12, (int)x + 3, 12, Magenta);
        }

        // ------------------------------------------------------------ Квартира
        // Стиль — жилой мегаблок как в Cyberpunk 2077: тёмный металл, бетон, кабели,
        // жёлтые LED-полосы, голоэкраны и неон.

        private static readonly Color MetalDark = new Color(0.06f, 0.06f, 0.09f);
        private static readonly Color MetalLight = new Color(0.24f, 0.24f, 0.3f);
        private static readonly Color CpYellow = new Color(0.98f, 0.88f, 0.12f);
        private static readonly Color HoloCyan = new Color(0.3f, 1f, 1f, 0.4f);

        // Пол из стальных плит 16x16 с заклёпками, в одной плите — вентиляционная решётка
        public static Sprite ApartmentFloor() => Create("floor_apartment", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                int lx = x % 16;
                int ly = y % 16;
                float tone = 0.1f + Hash(x / 16, y / 16 + 30) * 0.025f + Hash(x, y) * 0.012f;
                Color col = new Color(tone, tone, tone + 0.035f);
                if (lx == 0 || ly == 0)
                {
                    col = MetalDark;
                }
                else if (lx == 1 || ly == 15)
                {
                    col = new Color(tone + 0.05f, tone + 0.05f, tone + 0.08f);
                }
                else if ((lx == 3 || lx == 13) && (ly == 3 || ly == 13))
                {
                    col = MetalLight;
                }
                else if (x >= 16 && y < 16 && lx >= 4 && lx <= 12 && ly >= 5 && ly <= 11 && ly % 2 == 1)
                {
                    col = new Color(0.03f, 0.03f, 0.05f);
                }
                c.Replace(x, y, col);
            }
        });

        // Тёмная плитка ванной с бирюзовыми бликами
        public static Sprite BathroomFloor() => Create("floor_bathroom", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                bool seam = x % 8 == 0 || y % 8 == 0;
                float tone = 0.12f + Hash(x / 8, y / 8 + 50) * 0.04f;
                Color col = seam ? new Color(0.04f, 0.06f, 0.07f) : new Color(tone - 0.02f, tone + 0.03f, tone + 0.05f);
                if (!seam && x % 8 == 2 && y % 8 == 6)
                {
                    col = new Color(0.3f, 0.6f, 0.65f);
                }
                c.Replace(x, y, col);
            }
        });

        // Стена: металлические панели, труба с болтами, по краям пунктир жёлтых LED
        public static Sprite HomeWall() => Create("wall_home", 32, 20, c =>
        {
            c.Rect(0, 0, 31, 19, new Color(0.1f, 0.1f, 0.13f));
            c.Rect(0, 0, 31, 1, MetalDark);
            c.Rect(0, 18, 31, 19, MetalDark);
            for (int x = 0; x < 32; x++)
            {
                Color led = x % 8 < 5 ? CpYellow * 0.85f : MetalDark;
                led.a = 1f;
                c.Replace(x, 2, led);
                c.Replace(x, 17, led);
            }
            c.Rect(0, 8, 31, 11, new Color(0.17f, 0.17f, 0.21f));
            c.Rect(0, 10, 31, 10, new Color(0.28f, 0.28f, 0.34f));
            c.Rect(4, 8, 5, 11, MetalLight);
            c.Rect(20, 8, 21, 11, MetalLight);
            c.Rect(0, 3, 0, 16, MetalDark);
            c.Rect(16, 3, 16, 7, MetalDark);
            c.Rect(16, 12, 16, 16, MetalDark);
        });

        // Окно во всю стену: жалюзи, за ними силуэты мегаблоков, огни и розовая голореклама
        public static Sprite Window() => Create("window", 32, 20, c =>
        {
            var rng = new System.Random(7);
            c.Rect(0, 0, 31, 19, MetalDark);
            c.Rect(1, 3, 30, 16, new Color(0.05f, 0.03f, 0.13f));
            c.Ellipse(22f, 12f, 6f, 3f, new Color(1f, 0.2f, 0.7f, 0.45f));
            for (int x = 1; x < 31; x += 4)
            {
                int h = rng.Next(5, 11);
                c.Rect(x, 3, x + 2, 3 + h, new Color(0.08f, 0.06f, 0.15f));
                if (rng.Next(2) == 0) c.Rect(x + 1, 3 + rng.Next(1, h), x + 1, 3 + rng.Next(1, h), CpYellow);
                if (rng.Next(2) == 0) c.Rect(x, 4 + rng.Next(h - 1), x, 4 + rng.Next(h - 1), Neon);
            }
            for (int y = 4; y < 16; y += 3)
            {
                c.Rect(1, y, 30, y, new Color(0.02f, 0.02f, 0.04f, 0.75f));
            }
            c.Rect(1, 16, 30, 16, new Color(0.3f, 0.95f, 1f, 0.8f));
            c.Rect(16, 3, 16, 16, MetalDark);
        });

        // Тяжёлая гермодверь: жёлто-чёрная полоса, бирюзовый индикатор замка
        public static Sprite HomeDoor() => Create("door_home", 32, 20, c =>
        {
            c.Rect(0, 0, 31, 19, new Color(0.17f, 0.17f, 0.21f));
            c.Rect(0, 0, 31, 2, MetalDark);
            c.Rect(0, 17, 31, 19, MetalDark);
            for (int x = 0; x < 32; x++)
            for (int y = 7; y <= 12; y++)
            {
                c.Replace(x, y, (x + y) / 3 % 2 == 0 ? CpYellow * 0.9f + new Color(0, 0, 0, 0.1f) : MetalDark);
            }
            c.Rect(15, 3, 16, 16, MetalDark);
            c.Rect(14, 4, 17, 5, Neon);
        });

        // Диван у северной стены: чёрная стёганая синтокожа, красный акцент, неон снизу.
        // Подушка слева — там голова спящего героя
        public static Sprite Couch() => Create("couch", 112, 48, c =>
        {
            Color leather = new Color(0.19f, 0.17f, 0.23f);
            Color stitch = new Color(0.3f, 0.28f, 0.36f);
            c.RoundRect(1, 1, 110, 46, 6, MetalDark);
            c.RoundRect(3, 32, 108, 45, 5, new Color(0.13f, 0.12f, 0.16f));
            c.Rect(6, 44, 105, 44, new Color(0.3f, 1f, 1f, 0.7f));
            c.RoundRect(3, 3, 16, 40, 5, leather);
            c.RoundRect(95, 3, 108, 40, 5, leather);
            c.Rect(5, 36, 14, 37, Red);
            c.Rect(97, 36, 106, 37, Red);
            c.RoundRect(18, 5, 55, 31, 4, leather);
            c.RoundRect(56, 5, 93, 31, 4, leather);
            // Стёжка ромбами
            for (int y = 7; y < 30; y++)
            for (int x = 20; x < 92; x++)
            {
                if ((x + y) % 7 == 0 && (x - y + 70) % 7 == 0)
                {
                    c.Set(x, y, stitch);
                }
            }
            c.Rect(55, 6, 56, 30, MetalDark);
            c.RoundRect(18, 9, 31, 27, 4, new Color(0.62f, 0.14f, 0.2f));
            // Неоновая подсветка по низу
            c.Rect(8, 1, 103, 2, Neon);
            // Брошенная жёлтая куртка в ногах
            c.Ellipse(82f, 17f, 9f, 7f, new Color(0.75f, 0.62f, 0.08f));
            c.Line(new Vector2(75f, 13f), new Vector2(89f, 21f), 1.5f, new Color(0.45f, 0.36f, 0.05f));
            c.Outline(Outline);
        });

        // Металлический столик: голопроекция, кибердека, банки энергетика
        public static Sprite CoffeeTable() => Create("coffee_table", 56, 36, c =>
        {
            c.RoundRect(1, 1, 54, 34, 2, MetalDark);
            c.Rect(3, 3, 52, 32, new Color(0.14f, 0.14f, 0.18f));
            c.Rect(3, 32, 52, 32, CpYellow);
            // Голоэкран над столом
            c.Rect(6, 8, 24, 27, HoloCyan);
            c.Rect(6, 27, 24, 27, Neon);
            c.Rect(6, 8, 6, 27, Neon);
            for (int y = 11; y < 26; y += 3) c.Rect(8, y, 8 + (y * 5 % 13), y, new Color(0.7f, 1f, 1f, 0.8f));
            // Кибердека
            c.Rect(29, 8, 48, 19, new Color(0.08f, 0.08f, 0.1f));
            for (int x = 31; x < 47; x += 3) c.Rect(x, 11, x + 1, 11, Magenta);
            c.Rect(31, 15, 46, 16, new Color(0.3f, 0.3f, 0.36f));
            // Банки
            c.Circle(34f, 26f, 2.3f, CpYellow);
            c.Circle(41f, 27f, 2.3f, Red);
            c.Circle(47f, 24f, 2.3f, new Color(0.45f, 0.45f, 0.5f));
            c.Outline(Outline);
        });

        // Голоэкран у южной стены: светится в сторону дивана, по краям колонки
        public static Sprite Tv() => Create("tv", 96, 24, c =>
        {
            c.RoundRect(1, 1, 94, 12, 2, MetalDark);
            for (int x = 10; x < 86; x += 6) c.Rect(x, 6, x + 1, 6, Neon * 0.8f + new Color(0, 0, 0, 0.2f));
            c.Rect(4, 3, 12, 10, new Color(0.15f, 0.15f, 0.19f));
            c.Circle(8f, 6.5f, 2.5f, MetalDark);
            c.Rect(83, 3, 91, 10, new Color(0.15f, 0.15f, 0.19f));
            c.Circle(87f, 6.5f, 2.5f, MetalDark);
            c.Rect(8, 13, 87, 21, new Color(0.04f, 0.04f, 0.06f));
            for (int x = 10; x < 86; x++)
            {
                Color screen = Color.Lerp(Magenta, Neon, (x - 10) / 75f);
                c.Replace(x, 19, screen);
                c.Replace(x, 20, screen * 0.7f + new Color(0, 0, 0, 0.3f));
            }
            c.Rect(40, 19, 47, 20, Color.white);
            c.Outline(Outline);
        });

        // Кухня: металлическая столешница, мойка, синтезатор еды, лапша, индукционная плита
        public static Sprite KitchenCounter() => Create("kitchen_counter", 128, 36, c =>
        {
            c.Rect(1, 1, 126, 34, new Color(0.15f, 0.15f, 0.19f));
            for (int y = 4; y < 27; y += 4) c.Rect(1, y, 126, y, new Color(0.17f, 0.17f, 0.21f));
            c.Rect(1, 28, 126, 34, MetalDark);
            for (int x = 2; x < 126; x += 6) c.Rect(x, 29, x + 3, 29, CpYellow);
            c.Rect(1, 1, 126, 1, Neon);
            // Мойка
            c.RoundRect(6, 7, 30, 25, 3, MetalDark);
            c.Rect(17, 24, 19, 31, MetalLight);
            c.Circle(14f, 14f, 3.2f, new Color(0.5f, 0.5f, 0.56f));
            // Синтезатор еды с экраном
            c.Rect(38, 6, 60, 27, new Color(0.09f, 0.09f, 0.12f));
            c.Rect(41, 19, 57, 24, Magenta);
            c.Rect(43, 21, 50, 21, Color.white);
            c.Rect(45, 8, 53, 14, MetalDark);
            // Стаканчики лапши
            c.Circle(68f, 12f, 3f, new Color(0.9f, 0.88f, 0.85f));
            c.Circle(68f, 12f, 1.6f, Red);
            c.Circle(70f, 21f, 3f, new Color(0.9f, 0.88f, 0.85f));
            c.Circle(70f, 21f, 1.6f, CpYellow);
            // Индукционная плита: две конфорки раскалены
            c.Rect(80, 5, 120, 27, new Color(0.04f, 0.04f, 0.05f));
            c.Ring(90f, 11f, 3f, 4.2f, Red);
            c.Ring(110f, 11f, 3f, 4.2f, new Color(0.25f, 0.25f, 0.3f));
            c.Ring(90f, 21f, 3f, 4.2f, new Color(0.25f, 0.25f, 0.3f));
            c.Ring(110f, 21f, 3f, 4.2f, Red);
            c.Outline(Outline);
        });

        // Холодильник-автомат: металл, вертикальная LED-полоса, светящаяся панель
        public static Sprite Fridge() => Create("fridge", 36, 40, c =>
        {
            c.RoundRect(1, 1, 34, 38, 2, new Color(0.18f, 0.18f, 0.23f));
            c.Rect(1, 30, 34, 38, MetalDark);
            c.Rect(3, 3, 3, 28, Neon);
            c.Rect(8, 8, 28, 20, new Color(0.06f, 0.2f, 0.25f));
            c.Rect(10, 17, 22, 17, Neon);
            c.Rect(10, 13, 18, 13, CpYellow);
            c.Rect(10, 10, 25, 10, new Color(0.3f, 0.8f, 0.9f));
            c.Rect(29, 22, 31, 26, Red);
            c.Outline(Outline);
        });

        // Арсенал наёмника у восточной стены: стеклянная витрина с оружием и стойка с LED.
        // Сверху вниз: автомат, катана, дробовик, пистолеты, ПП, патроны
        public static Sprite Arsenal() => Create("arsenal", 72, 112, c =>
        {
            Color gun = new Color(0.05f, 0.05f, 0.07f);
            Color gunLight = new Color(0.3f, 0.3f, 0.36f);
            Color glass = new Color(0.12f, 0.2f, 0.26f);

            c.Rect(1, 1, 70, 110, MetalDark);
            // Стойка у стены (справа) с жёлтой LED-полосой
            c.Rect(58, 3, 69, 108, new Color(0.16f, 0.16f, 0.2f));
            c.Rect(57, 3, 57, 108, CpYellow);
            for (int y = 8; y < 106; y += 14) c.Rect(60, y, 67, y + 1, MetalLight);
            // Три ячейки витрины
            c.Rect(4, 4, 20, 107, glass);
            c.Rect(23, 4, 37, 107, glass);
            c.Rect(40, 4, 54, 107, glass);

            // Автомат (левая ячейка): приклад, ствольная коробка, магазин, прицел, ствол
            c.Rect(9, 18, 15, 28, gun);
            c.Rect(10, 28, 14, 78, gun);
            c.Rect(11, 79, 13, 100, gun);
            c.Rect(15, 52, 18, 62, gun);
            c.Rect(9, 64, 10, 74, gunLight);
            c.Rect(11, 40, 13, 46, Neon);
            // Пистолеты под ним
            c.Rect(8, 6, 16, 8, gun);
            c.Rect(8, 9, 10, 12, gun);
            c.Rect(9, 7, 15, 7, gunLight);

            // Катана (средняя ячейка): красная рукоять, цуба, клинок
            c.Rect(29, 8, 31, 26, new Color(0.7f, 0.1f, 0.15f));
            c.Rect(26, 27, 34, 28, CpYellow);
            c.Rect(29, 29, 31, 100, new Color(0.75f, 0.78f, 0.85f));
            c.Rect(31, 29, 31, 98, Color.white);
            c.Rect(30, 101, 30, 103, new Color(0.75f, 0.78f, 0.85f));

            // Дробовик и ПП (правая ячейка)
            c.Rect(44, 60, 50, 68, gun);
            c.Rect(45, 68, 49, 102, gun);
            c.Rect(46, 82, 48, 92, gunLight);
            c.Rect(44, 26, 50, 50, gun);
            c.Rect(50, 36, 52, 42, gun);
            c.Rect(46, 44, 48, 48, Red);
            // Коробки патронов
            c.Rect(43, 7, 51, 13, new Color(0.3f, 0.32f, 0.2f));
            c.Rect(43, 10, 51, 10, CpYellow);
            c.Rect(43, 16, 51, 21, new Color(0.3f, 0.32f, 0.2f));

            // Блики стекла
            c.Line(new Vector2(5f, 90f), new Vector2(19f, 104f), 1f, new Color(1f, 1f, 1f, 0.18f));
            c.Line(new Vector2(41f, 4f), new Vector2(54f, 18f), 1f, new Color(1f, 1f, 1f, 0.18f));
            c.Outline(Outline);
        });

        // Терминал нетраннера: стол с тремя мониторами у стены (сверху), клавиатура, кресло
        public static Sprite Terminal() => Create("terminal", 56, 48, c =>
        {
            c.Rect(1, 20, 54, 46, new Color(0.13f, 0.13f, 0.17f));
            c.Rect(1, 20, 54, 20, CpYellow);
            c.Rect(4, 40, 17, 43, Neon);
            c.Rect(20, 41, 35, 44, new Color(0.3f, 1f, 0.5f));
            c.Rect(38, 40, 51, 43, Magenta);
            c.Rect(4, 39, 51, 39, MetalDark);
            c.Rect(14, 25, 40, 30, MetalDark);
            for (int x = 16; x < 39; x += 3) c.Rect(x, 27, x + 1, 28, new Color(0.3f, 0.95f, 1f, 0.8f));
            c.Line(new Vector2(48f, 36f), new Vector2(52f, 22f), 1f, Red);
            // Кресло
            c.Circle(27f, 10f, 8f, new Color(0.1f, 0.1f, 0.12f));
            c.Ring(27f, 10f, 6.5f, 8f, Red);
            c.Rect(19, 15, 35, 17, new Color(0.08f, 0.08f, 0.1f));
            c.Outline(Outline);
        });

        // Душевая кабина: тёмная плитка, стекло с бирюзовой LED-кромкой
        public static Sprite Shower() => Create("shower", 64, 64, c =>
        {
            c.Rect(1, 1, 62, 62, new Color(0.11f, 0.14f, 0.16f));
            for (int i = 8; i < 62; i += 8)
            {
                c.Rect(i, 2, i, 61, new Color(0.06f, 0.08f, 0.09f));
                c.Rect(2, i, 61, i, new Color(0.06f, 0.08f, 0.09f));
            }
            c.Circle(32f, 32f, 3f, MetalDark);
            c.Ring(32f, 32f, 2f, 3f, MetalLight);
            c.Rect(1, 1, 62, 3, new Color(0.3f, 1f, 1f, 0.5f));
            c.Rect(1, 1, 3, 62, new Color(0.3f, 1f, 1f, 0.5f));
            c.Rect(1, 2, 62, 2, Neon);
            c.Rect(2, 1, 2, 62, Neon);
            c.Outline(Outline);
        });

        // Унитаз: бачок сверху (к стене), бирюзовая подсветка
        public static Sprite Toilet() => Create("toilet", 28, 36, c =>
        {
            c.RoundRect(3, 25, 24, 34, 2, new Color(0.22f, 0.23f, 0.28f));
            c.Rect(8, 29, 19, 30, Neon);
            c.Ellipse(13.5f, 14f, 9f, 12f, new Color(0.62f, 0.64f, 0.7f));
            c.Ellipse(13.5f, 13f, 5.5f, 8f, new Color(0.12f, 0.2f, 0.25f));
            c.Outline(Outline);
        });

        // Раковина: зеркало на стене (сверху) в розовой неоновой раме, металлическая чаша
        public static Sprite SinkMirror() => Create("sink_mirror", 40, 28, c =>
        {
            c.Rect(1, 21, 38, 26, Magenta);
            c.Rect(3, 22, 36, 25, new Color(0.45f, 0.65f, 0.75f));
            c.Rect(3, 25, 36, 25, new Color(0.85f, 1f, 1f));
            c.RoundRect(5, 3, 34, 20, 4, new Color(0.2f, 0.21f, 0.26f));
            c.Ellipse(19.5f, 11f, 9f, 5.5f, MetalDark);
            c.Rect(19, 15, 20, 20, MetalLight);
            c.Outline(Outline);
        });

        // Коврик-голомат: тёмный, с жёлтой геометрией и бирюзовыми углами
        public static Sprite Rug() => Create("rug", 96, 64, c =>
        {
            c.Rect(0, 0, 95, 63, new Color(0.05f, 0.05f, 0.08f, 0.9f));
            Color line = new Color(CpYellow.r, CpYellow.g, CpYellow.b, 0.55f);
            c.Rect(4, 4, 91, 4, line);
            c.Rect(4, 59, 91, 59, line);
            c.Rect(4, 4, 4, 59, line);
            c.Rect(91, 4, 91, 59, line);
            c.Line(new Vector2(4f, 32f), new Vector2(30f, 59f), 1f, line);
            c.Line(new Vector2(66f, 4f), new Vector2(91f, 32f), 1f, line);
            foreach (Vector2 p in new[] { new Vector2(8, 8), new Vector2(87, 8), new Vector2(8, 55), new Vector2(87, 55) })
            {
                c.Rect((int)p.x - 1, (int)p.y - 1, (int)p.x + 1, (int)p.y + 1, new Color(0.3f, 1f, 1f, 0.8f));
            }
        });

        // Колонка-сабвуфер со светящимся кольцом
        public static Sprite Speaker() => Create("speaker", 32, 32, c =>
        {
            c.RoundRect(4, 4, 27, 27, 3, new Color(0.08f, 0.08f, 0.1f));
            c.Circle(16f, 16f, 8.5f, MetalDark);
            c.Ring(16f, 16f, 7f, 8.5f, Magenta);
            c.Circle(16f, 16f, 3f, new Color(0.18f, 0.18f, 0.22f));
            c.Outline(Outline);
        });

        // Неоновая вывеска на стене: белые трубки, цвет задаётся SpriteRenderer.color
        public static Sprite NeonSign() => Create("neon_sign", 48, 16, c =>
        {
            Color halo = new Color(1f, 1f, 1f, 0.35f);
            Vector2[][] strokes =
            {
                new[] { new Vector2(4, 3), new Vector2(4, 13), new Vector2(11, 13) },
                new[] { new Vector2(8, 8), new Vector2(13, 3) },
                new[] { new Vector2(17, 13), new Vector2(17, 3), new Vector2(23, 3), new Vector2(23, 13) },
                new[] { new Vector2(17, 8), new Vector2(23, 8) },
                new[] { new Vector2(28, 3), new Vector2(34, 13) },
                new[] { new Vector2(34, 3), new Vector2(28, 13) },
                new[] { new Vector2(39, 13), new Vector2(44, 13), new Vector2(44, 3), new Vector2(39, 3) },
            };
            foreach (Vector2[] stroke in strokes)
            for (int i = 0; i < stroke.Length - 1; i++)
            {
                c.Line(stroke[i], stroke[i + 1], 3.2f, halo);
            }
            foreach (Vector2[] stroke in strokes)
            for (int i = 0; i < stroke.Length - 1; i++)
            {
                c.Line(stroke[i], stroke[i + 1], 1.3f, Color.white);
            }
        });

        // Бардак на полу: кабели, банки, ящик с патронами
        public static Sprite Clutter() => Create("clutter", 32, 32, c =>
        {
            c.Line(new Vector2(3f, 6f), new Vector2(14f, 12f), 1.2f, new Color(0.05f, 0.05f, 0.07f));
            c.Line(new Vector2(14f, 12f), new Vector2(10f, 24f), 1.2f, new Color(0.05f, 0.05f, 0.07f));
            c.Line(new Vector2(5f, 18f), new Vector2(20f, 28f), 1.2f, new Color(0.5f, 0.1f, 0.15f));
            c.Rect(16, 6, 27, 13, new Color(0.28f, 0.3f, 0.18f));
            c.Rect(16, 9, 27, 9, CpYellow);
            c.Circle(24f, 22f, 2.2f, CpYellow);
            c.Circle(8f, 27f, 2.2f, Red);
            c.Outline(Outline);
        });
    }
}
