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

        // Финал: Рэй на свалке — без рук и имплантов, ползёт (голова вправо, +x). На плечах — пустые разъёмы,
        // ноги волочатся позади
        public static Sprite HeroCrawl() => Create("hero_crawl", 40, 24, c =>
        {
            c.Line(new Vector2(4f, 9f), new Vector2(17f, 10.5f), 4.5f, HeroPants);
            c.Line(new Vector2(3f, 15.5f), new Vector2(17f, 13.5f), 4.5f, HeroPants);
            c.Ellipse(2.5f, 9f, 2.2f, 2.4f, HeroBoots);
            c.Ellipse(2f, 15.5f, 2.2f, 2.4f, HeroBoots);
            c.Ellipse(23f, 12f, 8f, 6.5f, HeroShirt);
            c.Line(new Vector2(18f, 12f), new Vector2(29f, 12f), 2f, HeroJacketShade);
            // Обрубки рук: тёмный металл и искрящий разъём
            c.Circle(26f, 5.5f, 2.2f, new Color(0.2f, 0.2f, 0.24f));
            c.Circle(26f, 18.5f, 2.2f, new Color(0.2f, 0.2f, 0.24f));
            c.Rect(26, 4, 26, 4, new Color(0.3f, 1f, 1f));
            c.Rect(26, 19, 26, 19, new Color(0.3f, 1f, 1f));
            c.Circle(33f, 12f, 4.4f, HeroHair);
            c.Rect(35, 11, 36, 13, HeroHairLight);
            c.Outline(Outline);
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
