using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace N89.EditorTools
{
    // Генерирует временный пиксель-арт для прототипа (PNG в Art/Sprites/Generated).
    // Когда появится настоящий арт — просто заменить PNG с теми же именами.
    internal static class SpriteFactory
    {
        public const int PixelsPerUnit = 32;
        private const string Dir = "Assets/_Project/Art/Sprites/Generated";

        private static readonly Color Outline = new Color(0.04f, 0.02f, 0.07f);
        private static readonly Color Skin = new Color(0.86f, 0.68f, 0.56f);
        private static readonly Color Gown = new Color(0.8f, 0.83f, 0.9f);
        private static readonly Color Neon = new Color(0.3f, 1f, 1f);
        private static readonly Color Red = new Color(1f, 0.18f, 0.28f);

        // Вид сверху, смотрит вправо (+x). Боевая стойка: левое плечо (сверху) выдвинуто вперёд,
        // правое (снизу) отведено назад, пистолет в двух руках на линии прицела (y = 16).
        public static Sprite Player() => Create("player", 32, 32, c =>
        {
            // Больничная роба, короткие волосы, неоновый имплант на затылке
            DrawShooter(c, Gown, new Color(0.62f, 0.65f, 0.74f), Skin, new Color(0.72f, 0.55f, 0.45f),
                new Color(0.16f, 0.11f, 0.16f), Skin, Neon, false);
        });

        public static Sprite Guard() => Create("guard", 32, 32, c =>
        {
            // Охранник корпорации Е: тёмная броня, шлем с красным визором, автомат
            DrawShooter(c, new Color(0.22f, 0.24f, 0.32f), new Color(0.13f, 0.14f, 0.2f), new Color(0.1f, 0.1f, 0.12f),
                new Color(0.07f, 0.07f, 0.09f), new Color(0.11f, 0.11f, 0.15f), Red, new Color(1f, 0.45f, 0.1f), true);
            // Нашивка корпорации на плече
            c.Rect(12, 22, 14, 23, new Color(1f, 0.45f, 0.1f));
        });

        // Вид сверху, смотрит вправо (+x). Боевая стойка: левое плечо (сверху) выдвинуто вперёд,
        // правое (снизу) отведено назад, оружие в двух руках на линии прицела (y = 16).
        private static void DrawShooter(PixelCanvas c, Color body, Color bodyShade, Color hands, Color handsShade,
            Color head, Color face, Color backOfHead, bool rifle)
        {
            Color gun = new Color(0.14f, 0.14f, 0.18f);
            Vector2 leftShoulder = new Vector2(13.5f, 23.5f);
            Vector2 rightShoulder = new Vector2(7.5f, 9.5f);
            Vector2 grip = new Vector2(23.5f, 16f);

            // Правая (дальняя) рука: рукав у плеча, дальше кисть тянется к рукояти наискосок
            c.Line(rightShoulder, grip, 2.6f, handsShade);
            c.Line(rightShoulder, Vector2.Lerp(rightShoulder, grip, 0.35f), 3.4f, bodyShade);
            // Корпус — "развёрнутые" плечи
            c.Line(leftShoulder, rightShoulder, 7f, body);
            c.Line(leftShoulder + new Vector2(-1.8f, -0.4f), rightShoulder + new Vector2(-1.8f, -0.4f), 2f, bodyShade);
            // Левая (ближняя) рука — от выдвинутого плеча прямо вперёд
            c.Line(leftShoulder, grip + new Vector2(0f, 0.6f), 2.6f, hands);
            c.Line(leftShoulder, Vector2.Lerp(leftShoulder, grip, 0.35f), 3.4f, body);
            // Оружие
            if (rifle)
            {
                c.Rect(19, 15, 31, 16, gun);
                c.Rect(21, 13, 23, 14, gun);
                c.Rect(28, 16, 31, 17, new Color(0.3f, 0.3f, 0.36f));
            }
            else
            {
                c.Rect(22, 15, 31, 16, gun);
                c.Rect(22, 14, 24, 14, gun);
                c.Rect(30, 16, 31, 16, new Color(0.3f, 0.3f, 0.36f));
            }
            // Голова сверху: в основном макушка, спереди край лица (или визор), на затылке метка
            c.Circle(11.5f, 16.5f, 4.1f, head);
            c.Rect(14, 15, 15, 17, face);
            c.Rect(7, 16, 8, 17, backOfHead);
            c.Outline(Outline);
        }

        public static Sprite Crosshair() => Create("crosshair", 15, 15, c =>
        {
            // Белый "+" с разрывом в центре и чёрной обводкой, как в Hotline Miami
            c.Rect(7, 1, 7, 4, Color.white);
            c.Rect(7, 10, 7, 13, Color.white);
            c.Rect(1, 7, 4, 7, Color.white);
            c.Rect(10, 7, 13, 7, Color.white);
            c.Rect(7, 7, 7, 7, Color.white);
            c.Outline(Color.black);
        });

        public static Sprite Drone() => Create("drone", 32, 32, c =>
            DrawDrone(c, new Color(0.27f, 0.28f, 0.35f), new Color(0.17f, 0.17f, 0.22f), false));

        // Боевой дрон охраны: темнее, с оранжевым кольцом корпорации и стволом
        public static Sprite CombatDrone() => Create("drone_combat", 32, 32, c =>
            DrawDrone(c, new Color(0.18f, 0.18f, 0.23f), new Color(1f, 0.45f, 0.1f), true));

        private static void DrawDrone(PixelCanvas c, Color body, Color ring, bool gun)
        {
            Color arm = new Color(0.16f, 0.16f, 0.2f);
            Color rotor = new Color(0.34f, 0.35f, 0.42f);
            Vector2 center = new Vector2(16, 16);
            foreach (Vector2 p in new[] { new Vector2(7, 7), new Vector2(25, 7), new Vector2(7, 25), new Vector2(25, 25) })
            {
                c.Line(center, p, 2f, arm);
                c.Circle(p.x, p.y, 3.8f, rotor);
                c.Circle(p.x, p.y, 1.6f, arm);
            }

            if (gun)
            {
                c.Rect(20, 15, 31, 16, new Color(0.12f, 0.12f, 0.15f));
            }

            c.Circle(16, 16, 8.5f, body);
            c.Ring(16, 16, 5.5f, 6.5f, ring);
            c.Circle(14, 18.5f, 2.5f, new Color(0.45f, 0.46f, 0.55f, 0.7f));
            c.Circle(21.5f, 16, 2.6f, Red);
            c.Circle(22f, 16, 1.1f, new Color(1f, 0.75f, 0.75f));
            c.Outline(Outline);
        }

        public static Sprite Target() => Create("target", 32, 32, c =>
        {
            Color red = new Color(0.85f, 0.12f, 0.2f);
            Color white = new Color(0.92f, 0.9f, 0.92f);
            c.Circle(16, 16, 13, red);
            c.Circle(16, 16, 10, white);
            c.Circle(16, 16, 7, red);
            c.Circle(16, 16, 4, white);
            c.Circle(16, 16, 1.8f, red);
            c.Outline(Outline);
        });

        public static Sprite TurretBase() => Create("turret_base", 32, 32, c =>
        {
            c.Circle(16, 16, 12, new Color(0.18f, 0.18f, 0.22f));
            c.Ring(16, 16, 9, 10, new Color(0.1f, 0.1f, 0.13f));
            c.Circle(16, 16, 8, new Color(0.24f, 0.24f, 0.29f));
            foreach (Vector2 p in new[] { new Vector2(16, 5.5f), new Vector2(16, 26.5f), new Vector2(5.5f, 16), new Vector2(26.5f, 16) })
            {
                c.Circle(p.x, p.y, 1.2f, new Color(0.4f, 0.4f, 0.45f));
            }
            c.Outline(Outline);
        });

        public static Sprite TurretHead() => Create("turret_head", 32, 32, c =>
        {
            c.Rect(16, 14, 31, 17, new Color(0.32f, 0.32f, 0.38f));
            c.Rect(28, 13, 31, 18, new Color(0.2f, 0.2f, 0.25f));
            c.Circle(16, 16, 6.5f, new Color(0.42f, 0.4f, 0.48f));
            c.Circle(19, 16, 1.6f, Red);
            c.Outline(Outline);
        });

        public static Sprite Bullet(string name, Color color) => Create(name, 12, 4, c =>
        {
            for (int x = 0; x < 12; x++)
            {
                Color col = color;
                col.a = Mathf.Pow(x / 11f, 1.5f);
                c.Rect(x, 1, x, 2, col);
            }
            c.Rect(9, 1, 11, 2, Color.white);
        });

        public static Sprite Wall() => Create("wall", 32, 20, c =>
        {
            c.Rect(0, 0, 31, 19, new Color(0.11f, 0.1f, 0.15f));
            c.Rect(0, 0, 31, 1, new Color(0.05f, 0.04f, 0.07f));
            c.Rect(0, 18, 31, 19, new Color(0.05f, 0.04f, 0.07f));
            c.Rect(0, 2, 31, 2, new Color(0.5f, 0.18f, 0.7f));
            c.Rect(0, 17, 31, 17, new Color(0.5f, 0.18f, 0.7f));
            c.Rect(0, 3, 0, 16, new Color(0.07f, 0.06f, 0.1f));
            c.Rect(16, 3, 16, 16, new Color(0.07f, 0.06f, 0.1f));
            c.Rect(1, 15, 31, 15, new Color(0.16f, 0.15f, 0.22f));
            foreach (int x in new[] { 4, 12, 20, 28 })
            {
                c.Rect(x, 6, x, 6, new Color(0.26f, 0.24f, 0.32f));
                c.Rect(x, 12, x, 12, new Color(0.26f, 0.24f, 0.32f));
            }
        });

        public static Sprite Glass() => Create("glass", 32, 20, c =>
        {
            c.Rect(0, 0, 31, 19, new Color(0.35f, 0.85f, 1f, 0.22f));
            c.Rect(0, 0, 31, 1, new Color(0.55f, 0.95f, 1f, 0.85f));
            c.Rect(0, 18, 31, 19, new Color(0.55f, 0.95f, 1f, 0.85f));
            c.Line(new Vector2(6, 3), new Vector2(12, 16), 1.5f, new Color(1f, 1f, 1f, 0.35f));
            c.Line(new Vector2(20, 3), new Vector2(23, 10), 1f, new Color(1f, 1f, 1f, 0.25f));
        });

        public static Sprite DoorPanel() => Create("door", 32, 20, c =>
        {
            c.Rect(0, 0, 31, 19, new Color(0.22f, 0.2f, 0.28f));
            for (int x = 0; x < 32; x++)
            for (int y = 0; y < 20; y++)
            {
                bool edge = y < 4 || y > 15;
                if (edge)
                {
                    bool stripe = (x + y) / 3 % 2 == 0;
                    c.Replace(x, y, stripe ? new Color(0.85f, 0.65f, 0.1f) : new Color(0.08f, 0.07f, 0.1f));
                }
            }
            c.Rect(0, 9, 31, 10, new Color(0.14f, 0.12f, 0.18f));
        });

        public static Sprite LabFloor(float radiusUnits) => Create("floor_lab", 1024, 1024, c =>
        {
            float r = radiusUnits * PixelsPerUnit;
            float apothem = r * 0.8660254f;
            for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                float px = x + 0.5f - c.Width / 2f;
                float py = y + 0.5f - c.Height / 2f;
                float hd = HexDistance(px, py);
                if (hd > apothem)
                {
                    continue;
                }

                Color col = FloorTile(x, y);

                // Концентрические шестиугольники — "разметка" лаборатории
                foreach (float ring in new[] { 0.28f, 0.56f, 0.8f })
                {
                    if (Mathf.Abs(hd - apothem * ring) < 1.2f)
                    {
                        col = Color.Lerp(col, new Color(0.15f, 0.55f, 0.65f), 0.7f);
                    }
                }

                // Полоса опасности вдоль стен
                if (hd > apothem - 26f)
                {
                    bool stripe = Mathf.FloorToInt((px + py) / 12f) % 2 == 0;
                    col = stripe ? new Color(0.45f, 0.36f, 0.08f) : new Color(0.07f, 0.06f, 0.08f);
                }

                c.Replace(x, y, col);
            }
        });

        public static Sprite FloorTileSprite() => Create("floor_tile", 32, 32, c =>
        {
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                c.Replace(x, y, FloorTile(x, y) * 0.8f + new Color(0, 0, 0, 0.2f));
        });

        public static Sprite Marker() => Create("marker", 32, 32, c =>
        {
            c.Ring(16, 16, 11, 13.5f, Neon);
            c.Ring(16, 16, 6, 7, new Color(Neon.r, Neon.g, Neon.b, 0.5f));
            c.Circle(16, 16, 2, Neon);
        });

        public static Sprite Capsule() => Create("capsule", 64, 96, c =>
        {
            c.RoundRect(4, 4, 59, 91, 14, new Color(0.2f, 0.22f, 0.3f));
            c.RoundRect(10, 16, 53, 84, 10, new Color(0.08f, 0.09f, 0.13f));
            c.RoundRect(14, 22, 49, 78, 8, new Color(0.25f, 0.75f, 0.85f, 0.45f));
            c.Line(new Vector2(20, 30), new Vector2(30, 70), 2f, new Color(1f, 1f, 1f, 0.25f));
            c.Rect(22, 6, 41, 10, new Color(0.12f, 0.12f, 0.16f));
            c.Rect(26, 7, 28, 9, Neon);
            c.Rect(35, 7, 37, 9, Red);
            c.Outline(Outline);
        });

        public static Sprite Console() => Create("console", 32, 32, c =>
        {
            c.RoundRect(2, 4, 29, 27, 3, new Color(0.17f, 0.17f, 0.23f));
            c.Rect(5, 13, 26, 24, new Color(0.06f, 0.35f, 0.4f));
            c.Rect(7, 21, 20, 21, Neon);
            c.Rect(7, 18, 24, 18, new Color(0.3f, 0.9f, 0.9f, 0.7f));
            c.Rect(7, 15, 15, 15, new Color(1f, 0.3f, 0.8f, 0.8f));
            c.Rect(5, 6, 26, 10, new Color(0.1f, 0.1f, 0.13f));
            for (int x = 7; x < 26; x += 3)
            {
                c.Rect(x, 8, x, 8, new Color(0.4f, 0.4f, 0.5f));
            }
            c.Outline(Outline);
        });

        public static Sprite Crate() => Create("crate", 32, 32, c =>
        {
            Color strap = new Color(0.14f, 0.12f, 0.17f);
            c.RoundRect(2, 2, 29, 29, 2, new Color(0.25f, 0.21f, 0.3f));
            c.Line(new Vector2(4, 4), new Vector2(28, 28), 2f, strap);
            c.Line(new Vector2(4, 28), new Vector2(28, 4), 2f, strap);
            c.Rect(2, 2, 29, 3, strap);
            c.Rect(2, 28, 29, 29, strap);
            c.Rect(13, 13, 18, 18, new Color(0.85f, 0.65f, 0.1f));
            c.Outline(Outline);
        });

        public static Sprite Scorch() => Create("scorch", 32, 32, c =>
        {
            var rng = new System.Random(89);
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16, 16));
                float edge = 9f + (float)rng.NextDouble() * 4f;
                if (d < edge)
                {
                    c.Replace(x, y, new Color(0.03f, 0.02f, 0.04f, Mathf.Lerp(0.8f, 0.3f, d / edge)));
                }
            }
            for (int i = 0; i < 10; i++)
            {
                int x = rng.Next(5, 27);
                int y = rng.Next(5, 27);
                c.Rect(x, y, x + rng.Next(0, 2), y, new Color(0.35f, 0.35f, 0.42f));
            }
        });

        // Лужа синей крови под убитым охранником: пятно с брызгами вокруг
        public static Sprite BloodPool() => Create("blood_pool", 32, 32, c =>
        {
            var rng = new System.Random(89);
            Color dark = new Color(0.03f, 0.1f, 0.5f, 0.9f);
            Color light = new Color(0.12f, 0.38f, 0.95f, 0.9f);
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16, 16));
                float edge = 7f + (float)rng.NextDouble() * 3f;
                if (d < edge)
                {
                    c.Replace(x, y, d < edge - 2.5f ? dark : light);
                }
            }
            for (int i = 0; i < 9; i++)
            {
                int x = rng.Next(2, 30);
                int y = rng.Next(2, 30);
                c.Rect(x, y, x + rng.Next(0, 2), y + rng.Next(0, 2), light);
            }
        });

        public static Sprite White() => Create("white", 8, 8, c => c.Rect(0, 0, 7, 7, Color.white));

        private static Color FloorTile(int x, int y)
        {
            int tx = x / 32;
            int ty = y / 32;
            int lx = x % 32;
            int ly = y % 32;

            float variation = Hash(tx, ty) * 0.025f;
            Color col = new Color(0.1f + variation, 0.1f + variation, 0.14f + variation);
            if (lx == 0 || ly == 0)
            {
                col = new Color(0.05f, 0.05f, 0.08f);
            }
            else if ((lx == 3 || lx == 28) && (ly == 3 || ly == 28))
            {
                col = new Color(0.17f, 0.17f, 0.22f);
            }

            // Грязь пятнами
            float grime = Mathf.PerlinNoise(x * 0.015f + 3.1f, y * 0.015f + 7.7f);
            col *= Mathf.Lerp(0.7f, 1.1f, grime);
            col.a = 1f;
            return col;
        }

        // "Шестиугольное расстояние" для pointy-top шестиугольника (возвращает апофему точки)
        private static float HexDistance(float x, float y)
        {
            const float s = 0.8660254f;
            return Mathf.Max(Mathf.Abs(x), Mathf.Max(Mathf.Abs(x * 0.5f + y * s), Mathf.Abs(-x * 0.5f + y * s)));
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        private static Sprite Create(string name, int width, int height, Action<PixelCanvas> draw)
        {
            MainMenuSceneBuilder.EnsureFolder(Dir);
            var canvas = new PixelCanvas(width, height);
            draw(canvas);

            string path = $"{Dir}/{name}.png";
            File.WriteAllBytes(path, canvas.EncodePng());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
