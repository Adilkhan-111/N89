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
