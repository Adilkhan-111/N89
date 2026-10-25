using UnityEngine;

namespace N89.EditorTools
{
    // Временный арт: летающая машина Рэя — белое угловатое air-купе (клиновидный нос, тонированный купол,
    // светодиодная полоса спереди), вместо колёс — антигравитационные опоры. Вид сверху, нос вправо (+x).
    // Настоящий арт — заменить PNG в Generated с теми же именами.
    internal static partial class SpriteFactory
    {
        private static readonly Color CarWhite = new Color(0.9f, 0.91f, 0.93f);
        private static readonly Color CarShade = new Color(0.66f, 0.68f, 0.72f);
        private static readonly Color CarCrease = new Color(0.78f, 0.79f, 0.83f);
        private static readonly Color CarGlass = new Color(0.17f, 0.12f, 0.11f);
        private static readonly Color CarGlassLight = new Color(0.36f, 0.27f, 0.24f);
        private static readonly Color CarPod = new Color(0.1f, 0.1f, 0.13f);

        // 88 x 44 px = 2.75 x 1.4 юнита
        public static Sprite AirCar() => Create("air_car", 88, 44, c =>
        {
            const float cy = 21.5f;

            // Антигравитационные опоры по углам — торчат из-под кузова
            foreach (Vector2 p in new[] { new Vector2(18f, 3f), new Vector2(18f, 40f), new Vector2(68f, 4f), new Vector2(68f, 39f) })
            {
                c.RoundRect((int)p.x - 7, (int)p.y - 3, (int)p.x + 7, (int)p.y + 3, 2f, CarPod);
                c.Rect((int)p.x - 4, (int)p.y, (int)p.x + 4, (int)p.y, Neon);
            }

            // Кузов-клин: сзади шире, нос сужается и срезан фасками
            for (int x = 3; x <= 85; x++)
            {
                float k = (x - 3f) / 82f;
                float half = Mathf.Lerp(18.5f, 14f, k);
                if (x > 76) half -= (x - 76) * 0.9f;
                if (x < 7) half -= (7 - x) * 1.2f;
                int y0 = Mathf.RoundToInt(cy - half), y1 = Mathf.RoundToInt(cy + half);
                c.Rect(x, y0, x, y1, CarWhite);
                c.Rect(x, y0, x, y0 + 1, CarShade);
                c.Rect(x, y1 - 1, x, y1, CarShade);
            }

            // Тонированный купол: начинается у капота и тянется почти до кормы
            for (int x = 22; x <= 72; x++)
            {
                float k = (x - 22f) / 50f;
                float half = x > 62 ? Mathf.Lerp(12f, 7f, (x - 62f) / 10f) : Mathf.Lerp(12f, 13f, k);
                c.Rect(x, Mathf.RoundToInt(cy - half), x, Mathf.RoundToInt(cy + half), CarGlass);
            }
            c.Line(new Vector2(30f, cy + 7f), new Vector2(62f, cy + 9f), 1.4f, CarGlassLight);
            c.Line(new Vector2(36f, cy + 3f), new Vector2(54f, cy + 4f), 1f, CarGlassLight);

            // Грани капота от носа к углам купола
            c.Line(new Vector2(84f, cy - 3f), new Vector2(72f, cy - 7f), 1f, CarCrease);
            c.Line(new Vector2(84f, cy + 3f), new Vector2(72f, cy + 7f), 1f, CarCrease);
            c.Line(new Vector2(20f, cy - 15f), new Vector2(72f, cy - 12f), 1f, CarCrease);
            c.Line(new Vector2(20f, cy + 15f), new Vector2(72f, cy + 12f), 1f, CarCrease);

            // Светодиодная полоса спереди и красный стоп-сигнал сзади
            c.Rect(85, Mathf.RoundToInt(cy - 8f), 86, Mathf.RoundToInt(cy + 8f), new Color(0.85f, 1f, 1f));
            c.Rect(3, Mathf.RoundToInt(cy - 13f), 4, Mathf.RoundToInt(cy + 13f), new Color(1f, 0.15f, 0.15f));
            // Двери: тонкие швы по бокам купола
            c.Rect(46, Mathf.RoundToInt(cy - 17f), 46, Mathf.RoundToInt(cy - 14f), CarShade);
            c.Rect(46, Mathf.RoundToInt(cy + 14f), 46, Mathf.RoundToInt(cy + 17f), CarShade);
            c.Outline(Outline);
        });

        // Тень машины на земле: при взлёте отъезжает в сторону и бледнеет
        public static Sprite AirCarShadow() => Create("air_car_shadow", 88, 44, c =>
        {
            c.RoundRect(3, 3, 85, 40, 10f, new Color(0f, 0f, 0f, 0.45f));
        });
    }
}
