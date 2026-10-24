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
    }
}
