using N89.Missions;
using N89.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Финал игры (концовка «не отдал relic»): ночная свалка — асфальт, кучи мусора, контейнеры, бочки с огнём.
    // Рэй без рук лежит внизу, далеко впереди (вверху) — яркий свет; он ползёт к нему (EndingDirector).
    // Запуск: N89 > Build Ending Scene.
    public static class EndingSceneBuilder
    {
        public const string SceneName = "Ending";
        private static readonly Rect Area = new Rect(-9f, -18f, 18f, 36f);

        [MenuItem("N89/Build Ending Scene")]
        public static void Build()
        {
            EnsureLayers();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadCommonAssets();
            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");
            TMP_FontAsset audiowide = MainMenuSceneBuilder.GetOrCreateFontAsset("Audiowide-Regular");

            Camera cam = CreateCamera();
            cam.orthographicSize = 4.5f;
            CreateVolume("EndingVolume");

            Transform lights = new GameObject("Lights").transform;
            Light2D global = new GameObject("Global Light").AddComponent<Light2D>();
            global.transform.SetParent(lights);
            global.lightType = Light2D.LightType.Global;
            global.intensity = 0.3f;
            global.color = new Color(0.45f, 0.45f, 0.7f);

            // Свалка
            Transform yard = new GameObject("Junkyard").transform;
            SpriteRenderer ground = CreateSprite("Ground", yard, SpriteFactory.Asphalt(), Area.center, -100);
            ground.drawMode = SpriteDrawMode.Tiled;
            ground.size = Area.size + Vector2.one * 20f;

            var rng = new System.Random(89);
            float R(float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());
            Sprite trash = SpriteFactory.TrashPile();
            Sprite[] small = { SpriteFactory.PizzaBox(), SpriteFactory.Bottles(), SpriteFactory.Butts(), SpriteFactory.OilStain() };
            // Кучи мусора по краям, посередине — прогалина, по которой можно ползти
            for (int i = 0; i < 34; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                Vector2 p = new Vector2(side * R(2.2f, 9f), R(Area.yMin, Area.yMax));
                SpriteRenderer pile = CreateSprite("TrashPile", yard, trash, p, 2, true, R(0f, 360f));
                pile.transform.localScale = Vector3.one * R(1.2f, 2.4f);
            }
            for (int i = 0; i < 40; i++)
            {
                CreateSprite("Junk", yard, small[i % small.Length], new Vector2(R(-8f, 8f), R(Area.yMin, Area.yMax)), 1, true, R(0f, 360f));
            }
            for (int i = 0; i < 6; i++)
            {
                SpriteRenderer rubble = CreateSprite("Rubble", yard, SpriteFactory.Rubble($"rubble_{i % 3}", i % 3), new Vector2(R(-7f, 7f), R(Area.yMin, Area.yMax)), 1, true, R(0f, 360f));
                rubble.transform.localScale = Vector3.one * R(1f, 1.8f);
            }
            CreateSprite("Mattress", yard, SpriteFactory.Mattress(), new Vector2(-2.6f, -12f), 1, true, 20f);
            CreateSprite("Container_L", yard, SpriteFactory.ShippingContainer(), new Vector2(-7.5f, 4f), 3, true, 8f);
            CreateSprite("Container_R", yard, SpriteFactory.ShippingContainer(), new Vector2(7f, -6f), 3, true, -12f);
            foreach (Vector2 p in new[] { new Vector2(-4.5f, -9f), new Vector2(4.2f, 2f), new Vector2(-3.8f, 10f) })
            {
                CreateSprite("FireBarrel", yard, SpriteFactory.FireBarrel(), p, 3);
                Mission01SceneBuilder.Pulsing(AddLight(lights, "Fire", p, new Color(1f, 0.55f, 0.2f), 3.5f, 1f), 6f, 0.7f, 1.1f);
            }

            // Свет впереди
            Transform lightPoint = new GameObject("LightPoint").transform;
            lightPoint.position = new Vector2(0f, Area.yMax - 1f);
            Light2D bright = AddLight(lights, "TheLight", lightPoint.position, new Color(0.9f, 0.97f, 1f), 9f, 1.2f);

            // Рэй — внизу, головой к свету
            // Ползти недолго: от света — около 10 юнитов
            Vector2 start = new Vector2(0f, Area.yMax - 11f);
            Transform hero = new GameObject("Bryce").transform;
            hero.position = start;
            var crawlFrames = new Sprite[4];
            for (int i = 0; i < crawlFrames.Length; i++) crawlFrames[i] = SpriteFactory.HeroCrawl(i);
            SpriteRenderer body = CreateSprite("Body", hero, crawlFrames[0], Vector2.zero, 10, true, 90f);
            body.transform.localScale = Vector3.one * 1.3f;
            // Лужа под ним — там, где очнулся; дальше за ним тянется след (EndingDirector)
            CreateSprite("BloodPool", yard, SpriteFactory.HeroBloodSmear(), start + new Vector2(0f, -0.3f), 5, true, 80f)
                .transform.localScale = new Vector3(2.4f, 2f, 1f);
            // Маркер «куда ползти»: стрелка у героя и маркер над светом
            WaypointArrow waypoint = Mission01SceneBuilder.BuildWaypoint(hero);
            // Relic в голове светится бирюзовым — по нему Рэя и видно в темноте
            Mission01SceneBuilder.Pulsing(AddLight(hero, "RelicGlow", start, new Color(0.3f, 1f, 1f), 2.6f, 0.9f), 1.5f, 0.6f, 1f);
            cam.transform.position = new Vector3(start.x, start.y + 1.5f, -10f);

            // UI: мысли внизу, затемнение, белая вспышка, титр
            RectTransform canvas = BuildCanvas();
            TextMeshProUGUI thought = Text("Thought", canvas, mono, string.Empty, 30f, new Color(0.8f, 0.9f, 1f), TextAlignmentOptions.Center);
            Anchor(thought.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1400f, 60f));
            Image white = Img("White", canvas, Color.white);
            MainMenuSceneBuilder.Stretch(white.rectTransform);
            CanvasGroup whiteGroup = Group(white.gameObject, 0f);
            TextMeshProUGUI title = Text("Title", canvas, audiowide, "N89", 120f, Color.black, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(1200f, 160f));
            TextMeshProUGUI subtitle = Text("Subtitle", canvas, mono, string.Empty, 36f, Color.black, TextAlignmentOptions.Center);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(1200f, 60f));
            Image black = Img("Black", canvas, Color.black);
            MainMenuSceneBuilder.Stretch(black.rectTransform);
            CanvasGroup blackGroup = Group(black.gameObject, 1f);
            BuildPauseMenu(canvas, mono);

            EndingDirector director = new GameObject("EndingDirector").AddComponent<EndingDirector>();
            Configure(director, so =>
            {
                P(so, "_hero").objectReferenceValue = hero;
                P(so, "_heroBody").objectReferenceValue = body.transform;
                SerializedProperty frames = P(so, "_crawlFrames");
                frames.arraySize = crawlFrames.Length;
                for (int i = 0; i < crawlFrames.Length; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = crawlFrames[i];
                P(so, "_bloodSmear").objectReferenceValue = SpriteFactory.HeroBloodSmear();
                P(so, "_waypoint").objectReferenceValue = waypoint;
                P(so, "_camera").objectReferenceValue = cam;
                P(so, "_area").rectValue = Area;
                P(so, "_lightPoint").objectReferenceValue = lightPoint;
                P(so, "_light").objectReferenceValue = bright;
                P(so, "_black").objectReferenceValue = blackGroup;
                P(so, "_white").objectReferenceValue = whiteGroup;
                P(so, "_thought").objectReferenceValue = thought;
                P(so, "_title").objectReferenceValue = title;
                P(so, "_subtitle").objectReferenceValue = subtitle;
            });

            string path = $"Assets/_Project/Scenes/{SceneName}.unity";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            AddSceneToBuildSettings(path, Mission03SceneBuilder.SceneName);
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Финал собран: {path}");
        }
    }
}
