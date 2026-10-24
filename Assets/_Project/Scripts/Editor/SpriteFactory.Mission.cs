using UnityEngine;

namespace N89.EditorTools
{
    // Временный арт миссий: логово банды — голый бетон, снаружи асфальт.
    internal static partial class SpriteFactory
    {
        // Бетонная стена с пятнами ржавчины и потёками
        public static Sprite GangWall() => Create("wall_gang", 32, 20, c =>
        {
            for (int y = 0; y < 20; y++)
            for (int x = 0; x < 32; x++)
            {
                float tone = 0.15f + Hash(x, y + 90) * 0.04f;
                Color col = new Color(tone + 0.01f, tone, tone - 0.005f);
                if (y < 2 || y > 17) col = new Color(0.07f, 0.065f, 0.07f);
                else if ((x * 5 + y * 3) % 29 == 0) col = new Color(0.32f, 0.17f, 0.1f);
                c.Replace(x, y, col);
            }
            c.Rect(9, 2, 9, 17, new Color(0.1f, 0.095f, 0.1f));
            c.Rect(25, 2, 25, 17, new Color(0.1f, 0.095f, 0.1f));
        });

        // Бетонный пол: плиты 16x16, трещины, грязь
        public static Sprite ConcreteFloor() => Create("floor_concrete", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float grime = Mathf.PerlinNoise(x * 0.09f + 11f, y * 0.09f + 4f);
                float tone = 0.12f + grime * 0.05f + Hash(x, y + 40) * 0.015f;
                Color col = new Color(tone, tone, tone + 0.01f);
                if (x % 16 == 0 || y % 16 == 0) col = new Color(0.07f, 0.07f, 0.08f);
                c.Replace(x, y, col);
            }
            c.Line(new Vector2(4f, 22f), new Vector2(11f, 27f), 1f, new Color(0.06f, 0.06f, 0.07f));
            c.Line(new Vector2(20f, 5f), new Vector2(23f, 11f), 1f, new Color(0.06f, 0.06f, 0.07f));
        });

        // Асфальт двора вокруг логова
        public static Sprite Asphalt() => Create("floor_asphalt", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float tone = 0.08f + Hash(x + 7, y + 13) * 0.03f;
                c.Replace(x, y, new Color(tone, tone, tone + 0.012f));
            }
        });

        // ---------------------------------------------------------------- Интерьер логова

        private static readonly Color Rust = new Color(0.36f, 0.18f, 0.1f);
        private static readonly Color Steel = new Color(0.26f, 0.27f, 0.31f);
        private static readonly Color SteelDark = new Color(0.13f, 0.13f, 0.16f);

        // Затёртый ковролин в «Chill room»: бордовый, в пятнах
        public static Sprite Carpet() => Create("floor_carpet", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float stain = Mathf.PerlinNoise(x * 0.12f + 3f, y * 0.12f + 8f);
                float tone = 0.16f + Hash(x + 3, y + 5) * 0.03f - stain * 0.05f;
                c.Replace(x, y, new Color(tone + 0.06f, tone * 0.45f, tone * 0.55f));
            }
        });

        // Резиновые маты в качалке: чёрные плиты с крошкой
        public static Sprite GymFloor() => Create("floor_gym", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float tone = 0.07f + Hash(x + 31, y + 17) * 0.025f;
                Color col = new Color(tone, tone, tone + 0.01f);
                if (Hash(x + 5, y + 61) > 0.93f) col = new Color(0.2f, 0.2f, 0.24f);
                if (x % 16 == 0 || y % 16 == 0) col = new Color(0.03f, 0.03f, 0.04f);
                c.Replace(x, y, col);
            }
        });

        // Паркет в переговорной: тёмные доски
        public static Sprite WoodFloor() => Create("floor_wood", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                int plank = y / 8;
                float tone = 0.13f + Hash(plank, (x + plank * 11) / 16) * 0.04f + Hash(x, y + 77) * 0.012f;
                Color col = new Color(tone + 0.05f, tone + 0.01f, tone - 0.03f);
                if (y % 8 == 0 || (x + plank * 11) % 16 == 0) col = new Color(0.06f, 0.04f, 0.03f);
                c.Replace(x, y, col);
            }
        });

        // Ряд шкафчиков (тайлится по вертикали): дверцы с вентиляцией, ручка, кое-где ржавчина
        public static Sprite Lockers() => Create("lockers", 24, 20, c =>
        {
            c.Rect(0, 0, 23, 19, SteelDark);
            c.Rect(1, 1, 22, 18, Steel);
            for (int y = 12; y <= 16; y += 2) c.Rect(4, y, 15, y, SteelDark);
            c.Rect(18, 6, 19, 10, new Color(0.7f, 0.7f, 0.75f));
            c.Rect(1, 0, 22, 0, Outline);
            c.Rect(3, 2, 6, 4, Rust);
            c.Set(20, 15, Rust);
        });

        // Лестничный пролёт: ступени поперёк, сверху вниз темнее
        public static Sprite Stairs() => Create("stairs", 44, 56, c =>
        {
            for (int x = 0; x < 44; x++)
            {
                int step = x / 6;
                float tone = 0.1f + step * 0.018f;
                for (int y = 0; y < 56; y++)
                {
                    Color col = new Color(tone, tone, tone + 0.01f);
                    if (x % 6 == 0) col = new Color(0.04f, 0.04f, 0.05f);
                    else if (x % 6 == 1) col = new Color(tone + 0.08f, tone + 0.08f, tone + 0.09f);
                    c.Replace(x, y, col);
                }
            }
            c.Rect(0, 0, 43, 1, Outline);
            c.Rect(0, 54, 43, 55, Outline);
        });

        // Продавленный диван банды: красная кожа с заплатками, спинкой к нижней стене
        public static Sprite GangSofa() => Create("sofa_gang", 152, 40, c =>
        {
            Color leather = new Color(0.42f, 0.08f, 0.1f);
            Color leatherDark = new Color(0.26f, 0.04f, 0.06f);
            c.RoundRect(1, 1, 150, 38, 5, leatherDark);
            c.RoundRect(3, 1, 148, 11, 4, new Color(0.2f, 0.03f, 0.05f));
            c.RoundRect(2, 4, 16, 37, 4, leather);
            c.RoundRect(135, 4, 149, 37, 4, leather);
            c.RoundRect(18, 11, 58, 35, 3, leather);
            c.RoundRect(59, 11, 96, 35, 3, leather);
            c.RoundRect(97, 11, 133, 35, 3, leather);
            // Заплатки скотчем и прорехи
            c.Rect(30, 20, 38, 23, new Color(0.55f, 0.55f, 0.5f));
            c.Rect(108, 25, 113, 31, new Color(0.08f, 0.06f, 0.05f));
            c.Line(new Vector2(70f, 15f), new Vector2(84f, 29f), 1f, new Color(0.15f, 0.02f, 0.04f));
            // Банки и пакет на сиденье
            c.Circle(46f, 28f, 2.5f, CpYellow);
            c.Circle(122f, 18f, 2.5f, new Color(0.5f, 0.5f, 0.55f));
            c.Outline(Outline);
        });

        // Аркадный автомат у стены: экран смотрит влево, в комнату
        public static Sprite Arcade() => Create("arcade", 32, 64, c =>
        {
            c.RoundRect(1, 1, 30, 62, 4, new Color(0.1f, 0.06f, 0.16f));
            c.RoundRect(4, 4, 27, 59, 3, new Color(0.2f, 0.1f, 0.32f));
            c.Rect(6, 18, 18, 46, new Color(0.04f, 0.04f, 0.06f));
            for (int y = 20; y < 45; y++)
            {
                Color glow = Color.Lerp(Magenta, Neon, (y - 20) / 25f);
                c.Rect(7, y, 17, y, y % 3 == 0 ? glow * 0.6f + new Color(0, 0, 0, 0.4f) : glow);
            }
            // Джойстик и кнопки на панели
            c.Rect(20, 22, 25, 42, new Color(0.08f, 0.08f, 0.1f));
            c.Circle(22.5f, 38f, 1.8f, Red);
            c.Circle(22.5f, 31f, 1.4f, CpYellow);
            c.Circle(22.5f, 26f, 1.4f, Neon);
            c.Rect(8, 52, 23, 55, Neon);
            c.Outline(Outline);
        });

        // Длинный стол переговорной: бумаги, пепельницы, ствол, деньги
        public static Sprite ConferenceTable() => Create("conference_table", 200, 60, c =>
        {
            c.RoundRect(1, 1, 198, 58, 6, new Color(0.08f, 0.05f, 0.04f));
            c.RoundRect(3, 3, 196, 56, 5, new Color(0.24f, 0.15f, 0.1f));
            for (int x = 10; x < 190; x += 23) c.Rect(x, 5, x, 54, new Color(0.2f, 0.12f, 0.08f));
            // Бумаги
            c.Rect(22, 34, 37, 49, new Color(0.85f, 0.84f, 0.78f));
            c.Rect(26, 30, 41, 44, new Color(0.75f, 0.74f, 0.7f));
            c.Rect(96, 12, 110, 26, new Color(0.85f, 0.84f, 0.78f));
            // Пепельницы
            c.Circle(60f, 18f, 4f, new Color(0.3f, 0.3f, 0.33f));
            c.Circle(60f, 18f, 2f, new Color(0.12f, 0.12f, 0.12f));
            c.Circle(135f, 42f, 4f, new Color(0.3f, 0.3f, 0.33f));
            c.Circle(135f, 42f, 2f, new Color(0.12f, 0.12f, 0.12f));
            // Пистолет и пачка денег
            c.Rect(72, 38, 86, 41, Gun);
            c.Rect(72, 34, 75, 38, Gun);
            c.Rect(150, 14, 164, 21, new Color(0.25f, 0.55f, 0.3f));
            c.Rect(150, 17, 164, 17, new Color(0.15f, 0.35f, 0.2f));
            c.Outline(Outline);
        });

        // Проектор на краю стола, объективом вправо
        public static Sprite Projector() => Create("projector", 16, 14, c =>
        {
            c.RoundRect(1, 1, 12, 12, 2, new Color(0.07f, 0.07f, 0.09f));
            c.Rect(3, 3, 9, 10, new Color(0.18f, 0.18f, 0.22f));
            c.Rect(12, 4, 14, 9, new Color(0.25f, 0.25f, 0.3f));
            c.Rect(14, 5, 14, 8, new Color(0.8f, 1f, 1f));
            c.Outline(Outline);
        });

        // Луч проектора: светлая трапеция, к экрану шире и прозрачнее
        public static Sprite ProjectorBeam() => Create("projector_beam", 96, 48, c =>
        {
            for (int x = 0; x < 96; x++)
            {
                float t = x / 95f;
                float half = Mathf.Lerp(3f, 23.5f, t);
                for (int y = 0; y < 48; y++)
                {
                    float d = Mathf.Abs(y - 23.5f);
                    if (d <= half)
                    {
                        float a = Mathf.Lerp(0.45f, 0.15f, t) * (1f - d / (half + 1f) * 0.5f);
                        c.Set(x, y, new Color(0.75f, 0.95f, 1f, a));
                    }
                }
            }
        });

        // Картинка проектора на стене: голубое пятно с «презентацией»
        public static Sprite ProjectorScreen() => Create("projector_screen", 6, 48, c =>
        {
            c.Rect(0, 0, 5, 47, new Color(0.6f, 0.9f, 1f));
            for (int y = 6; y < 42; y += 5) c.Rect(1, y, 4, y + 1, new Color(1f, 0.35f, 0.6f));
            c.Rect(0, 0, 0, 47, Color.white);
        });

        // Скамья для жима со штангой на стойках (штанга слева, поперёк скамьи)
        public static Sprite BenchPress() => Create("bench_press", 72, 40, c =>
        {
            Color pad = new Color(0.12f, 0.12f, 0.15f);
            c.Rect(16, 14, 66, 26, new Color(0.3f, 0.3f, 0.34f));
            c.RoundRect(16, 15, 70, 25, 3, pad);
            c.Rect(20, 19, 66, 20, new Color(0.22f, 0.22f, 0.27f));
            // Стойки и гриф
            c.Rect(10, 11, 13, 13, Steel);
            c.Rect(10, 27, 13, 29, Steel);
            c.Rect(11, 3, 12, 36, new Color(0.7f, 0.7f, 0.74f));
            // Блины
            c.Rect(5, 0, 18, 4, new Color(0.08f, 0.08f, 0.1f));
            c.Rect(5, 35, 18, 39, new Color(0.08f, 0.08f, 0.1f));
            c.Rect(7, 2, 16, 2, Red);
            c.Rect(7, 37, 16, 37, Red);
            c.Outline(Outline);
        });

        // Камера наблюдения (вид сверху, объектив вправо) и крепление к стене
        public static Sprite SecurityCamera() => Create("security_camera", 20, 12, c =>
        {
            c.RoundRect(2, 2, 15, 9, 2, new Color(0.78f, 0.78f, 0.82f));
            c.Rect(3, 3, 13, 4, new Color(0.55f, 0.55f, 0.6f));
            c.Rect(15, 3, 18, 8, new Color(0.12f, 0.12f, 0.15f));
            c.Rect(18, 4, 18, 7, new Color(0.3f, 0.6f, 0.8f));
            c.Outline(Outline);
        });

        public static Sprite CameraMount() => Create("camera_mount", 10, 10, c =>
        {
            c.Circle(4.5f, 4.5f, 4.2f, new Color(0.2f, 0.2f, 0.24f));
            c.Circle(4.5f, 4.5f, 2f, new Color(0.4f, 0.4f, 0.45f));
            c.Outline(Outline);
        });
    }
}
