using N89.Combat;
using N89.Enemies;
using N89.Hacking;
using N89.Missions;
using N89.Player;
using N89.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Собирает миссию 1 — логово банды по схеме: голые стены и бандиты, интерьера пока нет.
    // Координаты ниже — в юнитах схемы (1 юнит схемы = 24 px): здание 46 x 27, левый нижний угол — (0, 0).
    // В мир они переводятся умножением на S, чтобы комнаты были тесными, как в Hotline Miami
    // (персонажи и толщина стен не масштабируются — при смене S проверить проходы и посты у стен).
    // E1 — проём снизу слева (герой стартует перед ним), E2 — проём справа в коридоре. Вокруг здания — двор.
    // Стоящие бандиты — закорючки на схеме, патрульные — пунктирные петли (обходят по кругу).
    // Запуск: N89 > Build Mission 01 Scene.
    public static class Mission01SceneBuilder
    {
        public const string SceneName = "Mission01";
        private const string ScenePath = "Assets/_Project/Scenes/" + SceneName + ".unity";

        private const float S = 0.6f;
        private const float W = 46f;
        private const float H = 27f;
        private const float PatrolSpeed = 1.4f;

        // Точка схемы -> мир
        private static Vector2 V(float x, float y) => new Vector2(x, y) * S;

        private static Sprite s_wall;

        [MenuItem("N89/Build Mission 01 Scene")]
        public static void Build()
        {
            EnsureLayers();
            Physics2D.gravity = Vector2.zero;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadCommonAssets();
            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");
            TMP_FontAsset audiowide = MainMenuSceneBuilder.GetOrCreateFontAsset("Audiowide-Regular");

            // --- Префабы ---
            CommonPrefabs common = CreateCommonPrefabs();
            WeaponData[] weapons = Interlude01SceneBuilder.CreateWeapons();
            // Рэй в бою: все механики туториала; спрайт и параметры ствола ставит MissionDirector по выбору в арсенале
            GameObject heroPrefab = PlayerPrefab("HeroCombat", SpriteFactory.HeroRifle(), new[]
            {
                SpriteFactory.HeroLegs("hero_legs_idle", 0f), SpriteFactory.HeroLegs("hero_legs_left", 9f),
                SpriteFactory.HeroLegs("hero_legs_pass", 2f), SpriteFactory.HeroLegs("hero_legs_right", -9f),
            }, 0.65f, 0.32f, common);
            // Бандиты — люди с имплантами: истекают синей кровью, взламываются «Коротким замыканием»
            GameObject banditPrefab = EnemyPrefab("Bandit", "БАНДИТ", SpriteFactory.Bandit(), 1, 0.4f, 0.55f,
                common.EnemyBullet, common.Muzzle, common.Blood, common.BloodPool, common.ShortCircuit, common.Electric, true);

            // --- Сцена ---
            Camera cam = CreateCamera();
            CreateVolume("Mission01Volume");
            BuildLights();

            s_wall = SpriteFactory.GangWall();
            Transform level = new GameObject("Level").transform;
            BuildYard(level);
            BuildBase(level);

            Transform enemiesRoot = new GameObject("Bandits").transform;
            Health[] enemies = SpawnBandits(banditPrefab, enemiesRoot);

            Transform gameplay = new GameObject("Gameplay").transform;
            Vector2 start = V(5.5f, -2.5f);
            GameObject hero = Spawn(heroPrefab, gameplay, start);
            hero.transform.Find("Body").rotation = Quaternion.Euler(0f, 0f, 90f);
            hero.transform.Find("Legs").rotation = Quaternion.Euler(0f, 0f, 90f);

            CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
            Configure(follow, so =>
            {
                P(so, "_target").objectReferenceValue = hero.transform;
                P(so, "_maxLookAhead").floatValue = 3f;
            });
            Configure(hero.GetComponent<PlayerController>(), so => P(so, "_cameraFollow").objectReferenceValue = follow);
            cam.transform.position = new Vector3(start.x, start.y, -10f);

            // --- UI и сценарий ---
            HudRefs hud = BuildHud(hero.GetComponent<HackingController>(), mono, audiowide, "МИССИЯ 1 ВЫПОЛНЕНА", "Interlude02");

            MissionDirector director = new GameObject("MissionDirector").AddComponent<MissionDirector>();
            Configure(director, so =>
            {
                P(so, "_player").objectReferenceValue = hero.GetComponent<PlayerController>();
                P(so, "_hacking").objectReferenceValue = hero.GetComponent<HackingController>();
                P(so, "_weapon").objectReferenceValue = hero.GetComponent<Weapon>();
                P(so, "_heroBody").objectReferenceValue = hero.transform.Find("Body").GetComponent<SpriteRenderer>();
                SetArray(so, "_weapons", weapons);
                SetArray(so, "_enemies", enemies);
                P(so, "_fader").objectReferenceValue = hud.Fader;
                P(so, "_objective").objectReferenceValue = hud.Objective;
                P(so, "_hackingHud").objectReferenceValue = hud.HackingHud;
                P(so, "_endScreen").objectReferenceValue = hud.EndScreen;
            });

            ValidateQuickhacks();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath, Interlude01SceneBuilder.SceneName);
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Миссия 1 собрана: {ScenePath}");
        }

        // ---------------------------------------------------------------- Здание

        private static void BuildBase(Transform parent)
        {
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(parent);
            float t = WallThickness * 0.5f / S;

            Floor(parent, "Floor", SpriteFactory.ConcreteFloor(), Vector2.zero, V(W, H), -100);

            // Внешние стены: E1 — проём снизу (x 4..7), E2 — проём справа в коридоре (y 16..19)
            Wall(walls, "Outer_Bottom_W", -t, 0f, 4f, 0f);
            Wall(walls, "Outer_Bottom_E", 7f, 0f, W + t, 0f);
            Wall(walls, "Outer_Top", -t, H, W + t, H);
            Wall(walls, "Outer_Left", 0f, -t, 0f, H + t);
            Wall(walls, "Outer_Right_S", W, -t, W, 16f);
            Wall(walls, "Outer_Right_N", W, 19f, W, H + t);

            // Верхние комнаты / коридор (y 20.25), проём в середине
            Wall(walls, "H1_W", 0f, 20.25f, 24.8f, 20.25f);
            Wall(walls, "H1_E", 28.7f, 20.25f, W, 20.25f);
            // Коридор / нижняя часть (y 15), проём в середине
            Wall(walls, "H2_W", 0f, 15f, 26.2f, 15f);
            Wall(walls, "H2_E", 30f, 15f, W, 15f);
            // Большой зал / нижние комнаты (y 8.2): проём в середине и у правого края
            Wall(walls, "H3_W", 10.3f, 8.2f, 21.7f, 8.2f);
            Wall(walls, "H3_E", 25.1f, 8.2f, 42.4f, 8.2f);

            // Лестничная / вторая комната (x 9.4), проём внизу
            Wall(walls, "V1", 9.4f, 22.8f, 9.4f, H);
            // Вторая / правая верхняя комната (x 32.2), проём вверху
            Wall(walls, "V2", 32.2f, 20.25f, 32.2f, 24.2f);
            // Левая комната / зал и нижняя комната (x 10.3), проём в зал на y 8.3..11.2
            Wall(walls, "V3_S", 10.3f, 0f, 10.3f, 8.3f);
            Wall(walls, "V3_N", 10.3f, 11.2f, 10.3f, 15f);
            // Нижняя средняя / нижняя правая комната (x 29.5), сплошная
            Wall(walls, "V4", 29.5f, 0f, 29.5f, 8.2f);

            // Лестница (верхний левый угол): глухой блок и перила между пролётами
            Block(walls, "Stairs_Block", new Vector2(0f, 20.25f), new Vector2(2.1f, H));
            Wall(walls, "Stairs_Rail", 2.1f, 23.75f, 5.6f, 23.75f);

            // Левая комната: три глухих стеллажа
            Block(walls, "Rack_1", new Vector2(0f, 7.6f), new Vector2(1.25f, 14.9f));
            Block(walls, "Rack_2", new Vector2(2.7f, 7.6f), new Vector2(3.9f, 14.9f));
            Block(walls, "Rack_3", new Vector2(5.4f, 7.6f), new Vector2(6.5f, 14.9f));

            // Нижняя средняя комната: большой стол посередине (вокруг него проходят патрульные)
            Block(walls, "Table", new Vector2(13.8f, 2.6f), new Vector2(24.3f, 5.8f));
        }

        // Двор вокруг здания: асфальт и забор — между E1 и E2 можно пройти снаружи
        private static void BuildYard(Transform parent)
        {
            Transform fence = new GameObject("Yard").transform;
            fence.SetParent(parent);
            Vector2 min = new Vector2(-4f, -4.5f);
            Vector2 max = new Vector2(W + 4f, H + 4f);
            float t = WallThickness * 0.5f / S;
            Floor(fence, "Asphalt", SpriteFactory.Asphalt(), min * S, max * S, -101);
            Wall(fence, "Fence_S", min.x - t, min.y, max.x + t, min.y);
            Wall(fence, "Fence_N", min.x - t, max.y, max.x + t, max.y);
            Wall(fence, "Fence_W", min.x, min.y - t, min.x, max.y + t);
            Wall(fence, "Fence_E", max.x, min.y - t, max.x, max.y + t);
        }

        // ---------------------------------------------------------------- Бандиты

        private static Health[] SpawnBandits(GameObject prefab, Transform parent)
        {
            var bandits = new System.Collections.Generic.List<Health>();

            void Stand(Vector2 position, float facing)
            {
                bandits.Add(SpawnEnemy(prefab, parent, position * S, facing, Vector2.zero, 0f).GetComponent<Health>());
            }

            void Loop(Vector2 position, float facing, params Vector2[] perimeter)
            {
                for (int i = 0; i < perimeter.Length; i++)
                    perimeter[i] *= S;
                bandits.Add(SpawnPerimeter(prefab, parent, position * S, facing, PatrolSpeed, perimeter).GetComponent<Health>());
            }

            // Вход E1: двое смотрят вглубь комнаты — к ним можно подойти со спины
            Stand(new Vector2(1.9f, 1.5f), 60f);
            Stand(new Vector2(8.5f, 1.5f), 120f);

            // Лестничная, вторая и правая верхние комнаты
            Stand(new Vector2(7.5f, 25.4f), -90f);
            Stand(new Vector2(11f, 24.7f), -45f);
            Stand(new Vector2(30.6f, 22f), 180f);
            Stand(new Vector2(43.1f, 22f), 180f);

            // Коридор: две петли — левая и правая половины
            Loop(new Vector2(1.5f, 18.9f), 0f, new Vector2(23.4f, 0f), new Vector2(23.4f, -2.5f), new Vector2(0f, -2.5f));
            Loop(new Vector2(44.3f, 18.9f), 180f, new Vector2(-15.5f, 0f), new Vector2(-15.5f, -2.5f), new Vector2(0f, -2.5f));

            // Большой зал: двое на одной петле, с противоположных углов
            Loop(new Vector2(11.6f, 13.7f), 0f, new Vector2(32.7f, 0f), new Vector2(32.7f, -4.1f), new Vector2(0f, -4.1f));
            Loop(new Vector2(44.3f, 9.6f), 180f, new Vector2(-32.7f, 0f), new Vector2(-32.7f, 4.1f), new Vector2(0f, 4.1f));

            // Нижняя средняя комната: двое обходят стол, у стола — часовой лицом к стене
            Loop(new Vector2(28.2f, 6.75f), 180f, new Vector2(-16.6f, 0f), new Vector2(-16.6f, -5.17f), new Vector2(0f, -5.17f));
            Loop(new Vector2(11.6f, 1.58f), 0f, new Vector2(16.6f, 0f), new Vector2(16.6f, 5.17f), new Vector2(0f, 5.17f));
            Stand(new Vector2(25.4f, 4.2f), 0f);

            // Нижняя правая комната: четверо, смотрят на вход сверху справа
            Stand(new Vector2(31.25f, 6.4f), 0f);
            Stand(new Vector2(31.5f, 1.6f), 30f);
            Stand(new Vector2(37.7f, 1.8f), 70f);
            Stand(new Vector2(44f, 1.4f), 90f);

            return bandits.ToArray();
        }

        // ---------------------------------------------------------------- Свет

        private static void BuildLights()
        {
            Transform lights = new GameObject("Lights").transform;
            Light2D global = new GameObject("Global Light").AddComponent<Light2D>();
            global.transform.SetParent(lights);
            global.lightType = Light2D.LightType.Global;
            global.intensity = 0.3f;
            global.color = new Color(0.5f, 0.5f, 0.75f);

            Color warm = new Color(1f, 0.7f, 0.4f);
            Color toxic = new Color(0.45f, 1f, 0.3f);
            Color red = new Color(1f, 0.2f, 0.25f);
            Color magenta = new Color(1f, 0.25f, 0.75f);
            Color cyan = new Color(0.3f, 1f, 1f);

            Lamp(lights, "Stairs", 5.5f, 24f, warm, 5f, 0.8f);
            Lamp(lights, "Room_Top", 20.8f, 23.6f, warm, 9f, 0.7f);
            Lamp(lights, "Room_TopRight", 39f, 23.6f, toxic, 7f, 0.6f);
            Pulsing(Lamp(lights, "Corridor_W", 12f, 17.6f, red, 8f, 0.6f), 1.2f, 0.35f, 0.7f);
            Pulsing(Lamp(lights, "Corridor_E", 37f, 17.6f, red, 8f, 0.6f), 1.2f, 0.35f, 0.7f);
            Lamp(lights, "Room_Left", 5f, 7.5f, warm, 7f, 0.7f);
            Pulsing(Lamp(lights, "Hall", 27f, 11.6f, magenta, 12f, 0.6f), 0.8f, 0.45f, 0.7f);
            Lamp(lights, "Room_Table", 20f, 4.2f, warm, 8f, 0.8f);
            Lamp(lights, "Room_BottomRight", 37.7f, 4.1f, toxic, 7f, 0.7f);

            // Фонари во дворе у входов
            Lamp(lights, "Street_E1", 5.5f, -2.8f, cyan, 5f, 0.6f);
            Lamp(lights, "Street_E2", 48f, 17.5f, cyan, 5f, 0.6f);
            Lamp(lights, "Street_NW", -2f, 29f, magenta, 6f, 0.4f);
            Lamp(lights, "Street_SE", 48f, -2.5f, magenta, 6f, 0.4f);
        }

        // ---------------------------------------------------------------- Помощники

        // Помощники ниже принимают координаты схемы и сами переводят их в мир
        private static Light2D Lamp(Transform parent, string name, float x, float y, Color color, float radius, float intensity)
        {
            return AddLight(parent, name, V(x, y), color, radius * S, intensity);
        }

        private static void Wall(Transform parent, string name, float x1, float y1, float x2, float y2)
        {
            CreateWall(parent, name, V(x1, y1), V(x2, y2), s_wall, WallLayer, 5);
        }

        // Глухой блок (стеллаж, лестница, стол): плитка стены по всей площади и коллайдер
        private static void Block(Transform parent, string name, Vector2 min, Vector2 max)
        {
            min *= S;
            max *= S;
            SpriteRenderer sr = CreateSprite(name, parent, s_wall, (min + max) * 0.5f, 5);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = max - min;
            sr.gameObject.layer = WallLayer;
            sr.gameObject.AddComponent<BoxCollider2D>().size = sr.size;
        }

        private static void Floor(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max, int order)
        {
            SpriteRenderer sr = CreateSprite(name, parent, sprite, (min + max) * 0.5f, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = max - min;
        }

        private static Light2D Pulsing(Light2D light, float speed, float min, float max)
        {
            Pulse pulse = light.gameObject.AddComponent<Pulse>();
            Configure(pulse, so =>
            {
                P(so, "_light").objectReferenceValue = light;
                P(so, "_speed").floatValue = speed;
                P(so, "_scaleAmount").floatValue = 0f;
                P(so, "_lightMin").floatValue = min;
                P(so, "_lightMax").floatValue = max;
            });
            return light;
        }
    }
}
