using UnityEngine;

namespace N89.EditorTools
{
    // Временный арт башни «Арки» (миссия 3, последний этаж): ключ-карта начальника и сейф в стене.
    internal static partial class SpriteFactory
    {
        // Ключ-карта: тёмный пластик, жёлтая полоса, бирюзовый чип
        public static Sprite KeyCard() => Create("key_card", 16, 12, c =>
        {
            c.RoundRect(0, 0, 15, 11, 2, new Color(0.12f, 0.12f, 0.16f));
            c.Rect(1, 7, 14, 8, CpYellow);
            c.Rect(3, 2, 6, 5, new Color(0.3f, 1f, 1f));
            c.Rect(9, 2, 13, 3, new Color(0.5f, 0.5f, 0.6f));
            c.Outline(Outline);
        });

        private static readonly Color HeroBlood = new Color(0.5f, 0.03f, 0.06f);
        private static readonly Color HeroBloodLight = new Color(0.75f, 0.08f, 0.1f);

        // Финал: Рэй на свалке, едва живой — без рук и имплантов, ползёт на животе, отталкиваясь ногами
        // (голова вправо, +x). Кадры: 0 — ноги подтянуты, 1 — толкается левой, 2 — вытянулся вперёд,
        // 3 — толкается правой. Куртка в дырах и крови, из плечевых разъёмов сыплются искры
        public static Sprite HeroCrawl(int frame) => Create(frame == 0 ? "hero_crawl" : $"hero_crawl_{frame}", 48, 28, c =>
        {
            // Ноги: бедро → колено → стопа. Нижняя нога — y < 14, верхняя — y > 14
            Vector2[] bent = { new Vector2(18f, 11f), new Vector2(12f, 4.5f), new Vector2(6f, 8.5f) };
            Vector2[] straight = { new Vector2(18f, 11f), new Vector2(10f, 10.5f), new Vector2(2.5f, 10.5f) };
            Vector2[] low = frame == 0 || frame == 3 ? bent : straight;
            Vector2[] high = frame == 0 || frame == 1 ? bent : straight;
            foreach (Vector2[] leg in new[] { low, Mirror(high) })
            {
                c.Line(leg[0], leg[1], 4f, HeroPants);
                c.Line(leg[1], leg[2], 3.6f, HeroPants);
                c.Ellipse(leg[2].x, leg[2].y, 2.2f, 2f, HeroBoots);
            }
            c.Rect((int)low[1].x, (int)low[1].y, (int)low[1].x + 1, (int)low[1].y, HeroBlood);

            // Корпус: вытянулся — чуть длиннее и дальше вперёд
            float shift = frame == 2 ? 1f : frame == 0 ? -1f : 0f;
            float cx = 27f + shift;
            c.Ellipse(cx, 14f, frame == 2 ? 8.6f : 7.8f, 6.4f, HeroJacket);
            c.Line(new Vector2(cx - 6f, 14f), new Vector2(cx + 5f, 14f), 1.6f, HeroJacketShade);
            // Дыры от пуль и кровь на спине
            c.Circle(cx - 2f, 10.5f, 1.6f, HeroBlood);
            c.Circle(cx + 3f, 17.5f, 1.3f, HeroBlood);
            c.Rect((int)cx - 2, 10, (int)cx - 2, 10, HeroBloodLight);
            c.Rect((int)cx + 3, 17, (int)cx + 3, 17, HeroBloodLight);
            c.Rect((int)cx - 5, 16, (int)cx - 4, 17, HeroShirt);

            // Обрубки рук: тёмный металл, оборванные провода, искра мигает через кадр
            Color steel = new Color(0.2f, 0.2f, 0.24f);
            c.Circle(cx + 3f, 7.5f, 2.3f, steel);
            c.Circle(cx + 3f, 20.5f, 2.3f, steel);
            c.Line(new Vector2(cx + 4f, 6f), new Vector2(cx + 6f, 4f), 1f, new Color(0.9f, 0.3f, 0.2f));
            c.Line(new Vector2(cx + 4f, 22f), new Vector2(cx + 5.5f, 24f), 1f, new Color(0.3f, 0.5f, 0.9f));
            Color spark = frame % 2 == 0 ? new Color(0.3f, 1f, 1f) : CpYellow;
            c.Rect((int)cx + 3, 6 - frame % 2, (int)cx + 3, 6 - frame % 2, spark);
            c.Rect((int)cx + 2, 22 + frame % 2, (int)cx + 2, 22 + frame % 2, spark);

            // Голова опущена; у виска — бирюзовый relic
            float hx = 36.5f + shift;
            c.Circle(hx, 14f, 4.4f, HeroHair);
            c.Rect((int)hx, 12, (int)hx + 2, 13, HeroHairLight);
            c.Rect((int)hx + 1, 17, (int)hx + 2, 17, new Color(0.3f, 1f, 1f));
            c.Outline(Outline);
        });

        private static Vector2[] Mirror(Vector2[] points)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++) result[i] = new Vector2(points[i].x, 28f - points[i].y);
            return result;
        }

        // Кровавый след за ползущим Рэем (вдоль +x): мазок и пара капель
        public static Sprite HeroBloodSmear() => Create("hero_blood_smear", 14, 10, c =>
        {
            c.Ellipse(6.5f, 5f, 5.5f, 2.4f, new Color(HeroBlood.r, HeroBlood.g, HeroBlood.b, 0.85f));
            c.Ellipse(8f, 5.5f, 3f, 1.2f, new Color(HeroBloodLight.r, HeroBloodLight.g, HeroBloodLight.b, 0.8f));
            c.Rect(1, 8, 1, 8, HeroBlood);
            c.Rect(12, 1, 12, 1, HeroBlood);
        });

        // Лежащий Рэй для вставки relic: как hero_dead (DrawCorpse), но без правой руки — её рисует сценарий
        // отдельным спрайтом (hero_arm), который тянется к виску
        public static Sprite HeroDying() => Create("hero_dying", 48, 32, c =>
        {
            Color boots = new Color(0.06f, 0.06f, 0.07f);
            c.Line(new Vector2(19f, 14f), new Vector2(7f, 10.5f), 3.4f, HeroPants);
            c.Line(new Vector2(19f, 18f), new Vector2(12.5f, 23f), 3.4f, HeroPants);
            c.Line(new Vector2(12.5f, 23f), new Vector2(6.5f, 22f), 3.2f, HeroPants);
            c.Circle(6f, 10.2f, 2f, boots);
            c.Circle(5.5f, 22f, 2f, boots);
            c.Line(new Vector2(18f, 16f), new Vector2(30f, 16f), 9.5f, HeroJacket);
            c.Line(new Vector2(18f, 12.3f), new Vector2(30f, 12.3f), 2f, HeroJacketShade);
            c.Line(new Vector2(29f, 20.5f), new Vector2(37f, 27f), 2.8f, HeroJacket);
            c.Circle(38f, 28f, 2f, HeroSkin);
            c.Circle(35f, 16f, 4.6f, HeroHair);
            c.Circle(34f, 17.5f, 1.6f, HeroHairLight);
            c.Rect(33, 12, 35, 12, Magenta);
            c.Outline(Outline);
        });

        // Рука Рэя для финала (шарнир — левый край): рукав бордовой куртки и кисть
        public static Sprite HeroArm() => Create("hero_arm", 12, 5, c =>
        {
            c.Rect(0, 1, 8, 3, HeroJacket);
            c.Rect(0, 3, 8, 3, new Color(0.62f, 0.18f, 0.26f));
            c.Ellipse(9.8f, 2f, 1.8f, 1.7f, HeroSkin);
            c.Outline(Outline);
        });

        // Пистолет начальника (вид сверху, ствол вправо): воронёный, с хромированным затвором
        public static Sprite Pistol() => Create("pistol_chief", 14, 8, c =>
        {
            c.Rect(0, 1, 4, 6, new Color(0.12f, 0.12f, 0.15f));
            c.Rect(3, 3, 13, 6, new Color(0.18f, 0.18f, 0.22f));
            c.Rect(4, 5, 12, 6, new Color(0.6f, 0.62f, 0.7f));
            c.Outline(Outline);
        });

        // Сейф в стене (вид сверху, дверца к залу — вниз по спрайту): закрыт — кодовый замок горит красным
        public static Sprite WallSafe() => Create("wall_safe", 32, 12, c =>
        {
            c.Rect(0, 0, 31, 11, new Color(0.1f, 0.1f, 0.13f));
            c.Rect(2, 1, 29, 9, new Color(0.32f, 0.34f, 0.4f));
            c.Rect(3, 2, 28, 3, new Color(0.22f, 0.23f, 0.28f));
            c.Circle(15.5f, 6f, 2.5f, new Color(0.15f, 0.15f, 0.18f));
            c.Rect(24, 5, 26, 7, new Color(1f, 0.2f, 0.25f));
            c.Outline(Outline);
        });

        // Тот же сейф открытым: дверца откинута, внутри бирюзовая подсветка
        public static Sprite WallSafeOpen() => Create("wall_safe_open", 32, 12, c =>
        {
            c.Rect(0, 0, 31, 11, new Color(0.1f, 0.1f, 0.13f));
            c.Rect(2, 3, 29, 10, new Color(0.04f, 0.18f, 0.2f));
            c.Rect(4, 5, 27, 8, new Color(0.2f, 0.7f, 0.75f));
            c.Rect(0, 0, 31, 2, new Color(0.32f, 0.34f, 0.4f));
            c.Rect(24, 0, 26, 1, new Color(0.2f, 1f, 0.5f));
            c.Outline(Outline);
        });
    }
}
