using N89.Combat;
using N89.Enemies;
using N89.Hacking;
using N89.Missions;
using N89.Player;
using N89.UI;
using N89.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static N89.EditorTools.Mission01SceneBuilder;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Собирает миссию 2 — «Арка Системс»: на вид обычный дом посреди города, но с серьёзной охраной.
    // Пока только первый этаж по схеме от команды: зачистил — иди к лестнице в холле, там конец (второго этажа ещё нет).
    // Координаты — в юнитах схемы, как в миссии 1 (1 юнит = 24 px, мир = схема × S = 0.6), левый нижний угол — (0, 0).
    // Здание — буква Г: основная часть 34 x 25.9 и холл с лестницей справа снизу 12.8 x 16.25.
    // Комнаты: прихожая (вход снизу), кабинет, гостиная, кухня, лаунж наверху, зал с бассейном, комната охраны, холл.
    // Жёлтые проёмы на схеме — автодвери, синие — стекло, которое разбивается пулей (BreakableWindow).
    // Запуск: N89 > Build Mission 02 Scene.
    public static class Mission02SceneBuilder
    {
        public const string SceneName = "Mission02";

        private const float MainW = 34f;
        private const float W = 46.8f;
        private const float H = 25.9f;
        private const float HallTop = 16.25f;
        private const float PatrolSpeed = 1.4f;
        private const float DroneSpeed = 3f;

        // Участок вокруг здания: тротуар и глухая стена соседних мегаблоков
        private static readonly Vector2 PlotMin = new Vector2(-7f, -7f);
        private static readonly Vector2 PlotMax = new Vector2(W + 7f, H + 7f);

        private static readonly Color Teal = new Color(0.25f, 0.95f, 0.85f);
        private static readonly Color Warm = new Color(1f, 0.72f, 0.45f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.75f);
        private static readonly Color Cyan = new Color(0.3f, 1f, 1f);

        [MenuItem("N89/Build Mission 02 Scene")]
        public static void Build()
        {
            EnsureLayers();
            Physics2D.gravity = Vector2.zero;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadCommonAssets();
            SetLayout(Vector2.zero, SpriteFactory.HomeWall());
            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");
            TMP_FontAsset audiowide = MainMenuSceneBuilder.GetOrCreateFontAsset("Audiowide-Regular");

            // --- Префабы ---
            CommonPrefabs common = CreateCommonPrefabs();
            WeaponData[] weapons = Interlude01SceneBuilder.CreateWeapons();
            // Рэй — тот же префаб, что в миссии 1 (аргументы совпадают, чтобы не менять его там)
            GameObject heroPrefab = PlayerPrefab("HeroCombat", SpriteFactory.HeroRifle(), new[]
            {
                SpriteFactory.HeroLegs("hero_legs_idle", 0f), SpriteFactory.HeroLegs("hero_legs_left", 9f),
                SpriteFactory.HeroLegs("hero_legs_pass", 2f), SpriteFactory.HeroLegs("hero_legs_right", -9f),
            }, 0.65f, 0.32f, common, 0, SpriteFactory.HeroDead());
            // Охрана «Арки» — люди с имплантами (синяя кровь, замыкание оглушает), реакция как у охраны Корпорации Е
            GameObject guardPrefab = EnemyPrefab("ArkaGuard", "ОХРАНА // АРКА СИСТЕМС", SpriteFactory.ArkaGuard(), 1, 0.5f, 0.6f,
                common.EnemyBullet, common.Muzzle, common.Blood, common.BloodPool, common.ShortCircuit, common.Electric, true, SpriteFactory.ArkaGuardDead());
            // Боевой дрон из туториала: облетает периметр холла, с маршрута не уходит
            GameObject dronePrefab = EnemyPrefab("ArkaDrone", "БОЕВОЙ ДРОН // АРКА", SpriteFactory.CombatDrone(), 2, 0.75f, 0.6f,
                common.EnemyBullet, common.Muzzle, common.Explosion, common.Remains, common.ShortCircuit, common.Electric, false);
            Material particleMat = MainMenuSceneBuilder.GetOrCreateMaterial("ParticleUnlit", new Material(UnlitMaterial));
            GameObject glassFx = BurstPrefab("FX_Glass", new Color(0.6f, 0.95f, 1f), Color.white, 30, 2f, 7f, 0.25f, 0.6f, 0.05f, 0.1f, particleMat);

            // --- Сцена ---
            Camera cam = CreateCamera();
            CreateVolume("Mission02Volume");
            Light2D globalLight = BuildLights();

            Transform level = new GameObject("Level").transform;
            BuildCity(level, PlotMin * S - Vector2.one * 2f, PlotMax * S + Vector2.one * 2f, new Vector2(-36f, -34f), new Vector2(68f, 52f));
            BuildPlot(level);
            BuildWalls(level);
            BuildDoors(level);
            BuildWindows(level, glassFx);
            BuildFurniture(level);
            BuildPool(level);

            QuickhackData cameraOff = CreateQuickhack("CameraOff", "ОТКЛЮЧИТЬ КАМЕРУ", 4, 0.6f, QuickhackEffect.Disable);
            Transform enemiesRoot = new GameObject("Guards").transform;
            BuildCameras(level, cameraOff, common.Electric, enemiesRoot);
            Health[] enemies = SpawnGuards(guardPrefab, dronePrefab, enemiesRoot);

            // Лестница наверх в холле — маркер загорается, когда этаж зачищен
            Light2D stairsMarker = AddLight(level, "StairsMarker", V(44.3f, 7.2f), Cyan, 2.5f, 1.2f);
            Pulsing(stairsMarker, 3f, 0.4f, 1.4f);
            Rect stairsZone = WorldRect(new Vector2(42.6f, 5.9f), new Vector2(46.3f, 8.5f));
            // Из подвала герой поднимается сюда — рядом с лестницей, но вне её зоны
            Transform returnSpawn = new GameObject("ReturnSpawn").transform;
            returnSpawn.SetParent(level);
            returnSpawn.position = V(41.4f, 7.2f);

            // --- Подвал с Боссом: отдельная коробка далеко внизу мира, со своей схемой ---
            SetLayout(Mission02Basement.Origin, SpriteFactory.Wall());
            Mission02Basement.Refs basement = Mission02Basement.Build(common, glassFx, particleMat);
            SetLayout(Vector2.zero, SpriteFactory.HomeWall());

            Transform gameplay = new GameObject("Gameplay").transform;
            // Дальше радиуса автодвери — она не открывается, пока герой сам не подойдёт
            Vector2 start = V(6.9f, -4.5f);
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
            // Имплант миссии 2 фиксированный — «Руки-лезвия»
            AddBladeArms(hero, follow, common);
            cam.transform.position = new Vector3(start.x, start.y, -10f);

            // --- UI и сценарий ---
            HudRefs hud = BuildHud(hero.GetComponent<HackingController>(), mono, audiowide, "МИССИЯ 2 ВЫПОЛНЕНА", "Interlude03");
            Banner banner = BuildBanner(hud.Fader.transform, mono);
            WaypointArrow waypoint = BuildWaypoint(hero.transform);
            BossFight bossFight = Mission02Basement.BuildFight(basement, hero, follow, hud, mono, globalLight);

            MissionDirector director = new GameObject("MissionDirector").AddComponent<MissionDirector>();
            Configure(director, so =>
            {
                P(so, "_player").objectReferenceValue = hero.GetComponent<PlayerController>();
                P(so, "_hacking").objectReferenceValue = hero.GetComponent<HackingController>();
                P(so, "_weapon").objectReferenceValue = hero.GetComponent<Weapon>();
                P(so, "_heroBody").objectReferenceValue = hero.transform.Find("Body").GetComponent<SpriteRenderer>();
                P(so, "_mission").intValue = 2;
                SetArray(so, "_weapons", weapons);
                SetArray(so, "_enemies", enemies);
                P(so, "_cameraFollow").objectReferenceValue = follow;

                P(so, "_title").stringValue = "МИССИЯ 2";
                P(so, "_subtitle").stringValue = "Арка Системс";
                P(so, "_objectiveText").stringValue = "Зачисти первый этаж";
                P(so, "_clearedText").stringValue = "Первый этаж зачищен";
                P(so, "_stairsText").stringValue = "Спустись в подвал";
                P(so, "_stairsZone").rectValue = stairsZone;
                P(so, "_stairsMarker").objectReferenceValue = stairsMarker;
                // Лестница ведёт вниз, в подвал к Боссу; смерть там отматывает к подножию лестницы
                P(so, "_floor2Spawn").objectReferenceValue = basement.Spawn;
                P(so, "_floor2Facing").floatValue = basement.SpawnFacing;
                P(so, "_bossFight").objectReferenceValue = bossFight;
                P(so, "_checkpoint").stringValue = "mission02_basement";
                P(so, "_implantTitle").stringValue = "РУКИ-ЛЕЗВИЯ";
                P(so, "_implantHint").stringValue = "ПКМ — УДАР ЛЕЗВИЕМ   ·   ПО ПУЛЕ — ОТБИТЬ";
                P(so, "_banner").objectReferenceValue = banner;
                P(so, "_waypoint").objectReferenceValue = waypoint;

                P(so, "_fader").objectReferenceValue = hud.Fader;
                P(so, "_objective").objectReferenceValue = hud.Objective;
                P(so, "_hackingHud").objectReferenceValue = hud.HackingHud;
                P(so, "_endScreen").objectReferenceValue = hud.EndScreen;
                P(so, "_subtitles").objectReferenceValue = hud.Subtitles;
                P(so, "_letterboxTop").objectReferenceValue = hud.LetterboxTop;
                P(so, "_letterboxBottom").objectReferenceValue = hud.LetterboxBottom;

                // Отход: из подвала по лестнице в холл, на тротуар к машине (справа от входа)
                Mission01SceneBuilder.BuildAirCar(gameplay, V(15f, -4.5f), 0f, so);
                P(so, "_returnZone").rectValue = basement.ReturnZone;
                P(so, "_returnSpawn").objectReferenceValue = returnSpawn;
                P(so, "_returnFacing").floatValue = 180f;
                P(so, "_returnText").stringValue = "Поднимись из подвала";
            });

            ValidateQuickhacks();
            string path = $"Assets/_Project/Scenes/{SceneName}.unity";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            AddSceneToBuildSettings(path, Interlude02SceneBuilder.SceneName);
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Миссия 2 собрана: {path}");
        }

        // ---------------------------------------------------------------- Имплант

        // «Руки-лезвия»: клинок на шарнире у правого предплечья (крутится вокруг него во время взмаха) и след-серп
        private static void AddBladeArms(GameObject hero, CameraFollow follow, CommonPrefabs common)
        {
            // Сноп искр при отбивании пули: белые и голубые, быстрые и короткие
            Material particleMat = MainMenuSceneBuilder.GetOrCreateMaterial("ParticleUnlit", new Material(UnlitMaterial));
            GameObject deflectFx = BurstPrefab("FX_Deflect", Color.white, Cyan, 28, 5f, 14f, 0.08f, 0.25f, 0.04f, 0.09f, particleMat);
            Transform body = hero.transform.Find("Body");
            var pivot = new GameObject("BladePivot").transform;
            pivot.SetParent(body, false);
            pivot.localPosition = new Vector2(0.1f, -0.15f);
            Sprite blade = SpriteFactory.BladeArm();
            CreateSprite("Blade", pivot, blade, Vector2.zero, 11).transform.localPosition = new Vector2(blade.bounds.size.x * 0.5f, 0f);
            SpriteRenderer trail = CreateSprite("BladeSlash", body, SpriteFactory.BladeSlash(), Vector2.zero, 12, false);
            const float reach = 1.35f;
            trail.transform.localScale = Vector3.one * reach;
            SpriteRenderer smear = CreateSprite("BladeSmear", body, SpriteFactory.BladeSmear(), Vector2.zero, 12, false);
            smear.transform.localScale = Vector3.one * reach;

            BladeArms arms = hero.AddComponent<BladeArms>();
            Configure(arms, so =>
            {
                P(so, "_player").objectReferenceValue = hero.GetComponent<PlayerController>();
                P(so, "_health").objectReferenceValue = hero.GetComponent<Health>();
                P(so, "_body").objectReferenceValue = body;
                P(so, "_cameraFollow").objectReferenceValue = follow;
                P(so, "_blade").objectReferenceValue = pivot;
                P(so, "_trail").objectReferenceValue = trail;
                P(so, "_smear").objectReferenceValue = smear;
                P(so, "_pixel").objectReferenceValue = SpriteFactory.White();
                P(so, "_material").objectReferenceValue = UnlitMaterial;
                P(so, "_reach").floatValue = reach;
                P(so, "_hitMask").intValue = (1 << EnemyLayer) | (1 << GlassLayer);
                P(so, "_wallMask").intValue = (1 << WallLayer) | (1 << DoorLayer);
                P(so, "_deflectMask").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << DoorLayer) | (1 << EnemyLayer);
                P(so, "_bloodFxPrefab").objectReferenceValue = common.Blood;
                P(so, "_bloodPoolPrefab").objectReferenceValue = common.BloodPool;
                P(so, "_deflectFxPrefab").objectReferenceValue = deflectFx;
                P(so, "_ringSprite").objectReferenceValue = SpriteFactory.FxRing();
            });
        }

        // ---------------------------------------------------------------- Участок

        // Тротуар вокруг здания, глухие стены соседних домов по краю, кадки с растениями и вывеска у входа
        private static void BuildPlot(Transform parent)
        {
            Transform plot = new GameObject("Plot").transform;
            plot.SetParent(parent);
            float t = WallThickness * 0.5f / S;
            Floor(plot, "Sidewalk", SpriteFactory.Sidewalk(), ToWorld(PlotMin), ToWorld(PlotMax), -101);

            Sprite wall = SpriteFactory.Wall();
            CreateWall(plot, "Plot_S", V(PlotMin.x - t, PlotMin.y), V(PlotMax.x + t, PlotMin.y), wall, WallLayer, 5);
            CreateWall(plot, "Plot_N", V(PlotMin.x - t, PlotMax.y), V(PlotMax.x + t, PlotMax.y), wall, WallLayer, 5);
            CreateWall(plot, "Plot_W", V(PlotMin.x, PlotMin.y - t), V(PlotMin.x, PlotMax.y + t), wall, WallLayer, 5);
            CreateWall(plot, "Plot_E", V(PlotMax.x, PlotMin.y - t), V(PlotMax.x, PlotMax.y + t), wall, WallLayer, 5);

            // Кадки вдоль фасада и у входа — низкие, слой Glass
            Sprite plant = SpriteFactory.Plant();
            foreach (Vector2 p in new[] { new Vector2(3.6f, -1.6f), new Vector2(10.2f, -1.6f), new Vector2(20f, -1.6f), new Vector2(30f, -1.6f),
                         new Vector2(42f, -1.6f), new Vector2(-1.6f, 10f), new Vector2(-1.6f, 20f), new Vector2(38f, 18f), new Vector2(48.4f, 8f) })
            {
                Prop(plot, "Planter", plant, p - Vector2.one * 0.75f, p + Vector2.one * 0.75f, 3, GlassLayer);
            }

            // Вывеска «Арки» над входом и коврик
            SpriteRenderer sign = Prop(plot, "ArkaSign", SpriteFactory.NeonSign(), new Vector2(5.3f, -0.95f), new Vector2(8.5f, -0.35f), 7, -1, false);
            sign.color = Teal;
            Pulsing(Lamp(plot, "ArkaSign_Light", 6.9f, -0.8f, Teal, 6f, 1f), 2f, 0.7f, 1f);
            Prop(plot, "Doormat", SpriteFactory.Rug(), new Vector2(5.6f, 0.3f), new Vector2(8.2f, 1.6f), -97);

            Decor(plot, "Trash", SpriteFactory.TrashPile(), new Vector2(50f, -4f), 2.2f);
            Decor(plot, "Stain", SpriteFactory.OilStain(), new Vector2(26f, -4.5f), 3f);
            Decor(plot, "Butts", SpriteFactory.Butts(), new Vector2(9f, -3.5f), 1f);
        }

        // ---------------------------------------------------------------- Стены

        private static void BuildWalls(Transform parent)
        {
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(parent);
            float t = WallThickness * 0.5f / S;

            // Полы: под всем зданием мрамор холла, по комнатам свои
            Floor(parent, "Floor", SpriteFactory.MarbleFloor(), V(0f, 0f), V(MainW, H), -100);
            Floor(parent, "Floor_Hall", SpriteFactory.MarbleFloor(), V(MainW, 0f), V(W, HallTop), -100);
            Floor(parent, "Floor_Entrance", SpriteFactory.WoodFloor(), V(0f, 0f), V(11.6f, 6.1f), -99);
            Floor(parent, "Floor_Office", SpriteFactory.WoodFloor(), V(11.6f, 0f), V(17.1f, 6.1f), -99);
            Floor(parent, "Floor_Living", SpriteFactory.ApartmentFloor(), V(17.1f, 0f), V(MainW, 6.1f), -99);
            Floor(parent, "Floor_Kitchen", SpriteFactory.BathroomFloor(), V(0f, 6.1f), V(11.6f, H), -99);
            Floor(parent, "Floor_Lounge", SpriteFactory.Carpet(), V(11.6f, 17.4f), V(MainW, H), -99);
            Floor(parent, "Floor_Pool", SpriteFactory.BathroomFloor(), V(11.6f, 6.1f), V(28.2f, 17.4f), -99);
            Floor(parent, "Floor_Security", SpriteFactory.ConcreteFloor(), V(28.2f, 6.1f), V(MainW, 17.4f), -99);

            // Внешние стены. Вход — снизу в прихожую (x 5.6..8.2)
            Wall(walls, "Outer_Bottom_W", -t, 0f, 5.6f, 0f);
            Wall(walls, "Outer_Bottom_E", 8.2f, 0f, W + t, 0f);
            Wall(walls, "Outer_Left", 0f, -t, 0f, H + t);
            Wall(walls, "Outer_Top", -t, H, MainW + t, H);
            Wall(walls, "Outer_HallTop", MainW, HallTop, W + t, HallTop);
            Wall(walls, "Outer_HallRight", W, -t, W, HallTop + t);

            // x 11.6: прихожая/кабинет (стекло 1.4..4.5), кухня/бассейн (дверь 10.4..12.7), кухня/лаунж (дверь 17.6..19.9)
            Wall(walls, "V_Kitchen_1", 11.6f, 0f, 11.6f, 1.4f);
            Wall(walls, "V_Kitchen_2", 11.6f, 4.5f, 11.6f, 10.4f);
            Wall(walls, "V_Kitchen_3", 11.6f, 12.7f, 11.6f, 17.6f);
            Wall(walls, "V_Kitchen_4", 11.6f, 19.9f, 11.6f, H);
            // y 6.1: прихожая/кухня (открытый проход 2.5..4.7), бассейн/кабинет (дверь 13.7..16), бассейн/гостиная (стекло 19.7..27.8),
            // охрана/гостиная (дверь 30.5..32.8)
            Wall(walls, "H_Mid_1", 0f, 6.1f, 2.5f, 6.1f);
            Wall(walls, "H_Mid_2", 4.7f, 6.1f, 13.7f, 6.1f);
            Wall(walls, "H_Mid_3", 16f, 6.1f, 19.7f, 6.1f);
            Wall(walls, "H_Mid_4", 27.8f, 6.1f, 30.5f, 6.1f);
            Wall(walls, "H_Mid_5", 32.8f, 6.1f, MainW, 6.1f);
            // y 17.4: лаунж/бассейн (дверь 16..18.4), лаунж/охрана (дверь 29.1..31.4)
            Wall(walls, "H_Top_1", 11.6f, 17.4f, 16f, 17.4f);
            Wall(walls, "H_Top_2", 18.4f, 17.4f, 29.1f, 17.4f);
            Wall(walls, "H_Top_3", 31.4f, 17.4f, MainW, 17.4f);
            // Бассейн/охрана и кабинет/гостиная — сплошные
            Wall(walls, "V_Pool", 28.2f, 6.1f, 28.2f, 17.4f);
            Wall(walls, "V_Office", 17.1f, 0f, 17.1f, 6.1f);
            // x 34: дом/холл — двери из гостиной (0.5..2.6) и из комнаты охраны (13.9..16.1); выше — внешняя стена
            Wall(walls, "V_Hall_1", MainW, -t, MainW, 0.5f);
            Wall(walls, "V_Hall_2", MainW, 2.6f, MainW, 13.9f);
            Wall(walls, "V_Hall_3", MainW, 16.1f, MainW, H + t);

            // Холл: перегородки вокруг лестницы (чёрные полосы на схеме)
            Block(walls, "Hall_Wall_1", new Vector2(39.4f, 11.6f), new Vector2(W, 12.7f));
            Block(walls, "Hall_Wall_2", new Vector2(39.8f, 8.8f), new Vector2(W, 9.96f));
            Block(walls, "Hall_Wall_3", new Vector2(41.5f, 4.67f), new Vector2(W, 5.5f));
            Block(walls, "Hall_Wall_4", new Vector2(MainW, 3.25f), new Vector2(38.4f, 4.2f));
        }

        private static void BuildDoors(Transform parent)
        {
            Transform doors = new GameObject("Doors").transform;
            doors.SetParent(parent);
            AutoDoorAt(doors, "Door_Entrance", true, 5.6f, 8.2f, 0f);
            AutoDoorAt(doors, "Door_Kitchen_Lounge", false, 17.6f, 19.9f, 11.6f);
            AutoDoorAt(doors, "Door_Kitchen_Pool", false, 10.4f, 12.7f, 11.6f);
            AutoDoorAt(doors, "Door_Lounge_Pool", true, 16f, 18.4f, 17.4f);
            AutoDoorAt(doors, "Door_Lounge_Security", true, 29.1f, 31.4f, 17.4f);
            AutoDoorAt(doors, "Door_Pool_Office", true, 13.7f, 16f, 6.1f);
            AutoDoorAt(doors, "Door_Security_Living", true, 30.5f, 32.8f, 6.1f);
            AutoDoorAt(doors, "Door_Security_Hall", false, 13.9f, 16.1f, MainW);
            AutoDoorAt(doors, "Door_Living_Hall", false, 0.5f, 2.6f, MainW);
        }

        // ---------------------------------------------------------------- Стекло

        private static void BuildWindows(Transform parent, GameObject glassFx)
        {
            Transform windows = new GameObject("Windows").transform;
            windows.SetParent(parent);
            Sprite pane = SpriteFactory.WindowPane();
            Sprite broken = SpriteFactory.WindowBroken();
            Sprite shards = SpriteFactory.GlassShards();
            WindowAt(windows, "Window_Entrance_Office", false, 1.4f, 4.5f, 11.6f, pane, broken, shards, glassFx);
            WindowAt(windows, "Window_Pool_Living", true, 19.7f, 27.8f, 6.1f, pane, broken, shards, glassFx);
        }

        // Стекло в проёме: from..to — края вдоль стены (юниты схемы), at — координата стены
        internal static void WindowAt(Transform parent, string name, bool horizontal, float from, float to, float at,
            Sprite paneSprite, Sprite brokenSprite, Sprite shardsSprite, GameObject glassFx)
        {
            Vector2 a = horizontal ? V(from, at) : V(at, from);
            Vector2 b = horizontal ? V(to, at) : V(at, to);
            float width = Vector2.Distance(a, b);

            var root = new GameObject(name) { layer = GlassLayer };
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.Euler(0f, 0f, horizontal ? 0f : 90f));
            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, WallThickness * 0.6f);

            SpriteRenderer pane = CreateSprite("Pane", root.transform, paneSprite, Vector2.zero, 4, false);
            pane.drawMode = SpriteDrawMode.Tiled;
            pane.size = new Vector2(width, WallThickness * 0.6f);
            SpriteRenderer shards = CreateSprite("Shards", root.transform, shardsSprite, Vector2.zero, -95);
            shards.drawMode = SpriteDrawMode.Tiled;
            shards.size = new Vector2(width, 1.6f);
            shards.transform.localRotation = Quaternion.identity;

            Health health = root.AddComponent<Health>();
            Configure(health, so =>
            {
                P(so, "_maxHealth").intValue = 1;
                P(so, "_destroyOnDeath").boolValue = false;
                P(so, "_deathFxPrefab").objectReferenceValue = glassFx;
            });

            BreakableWindow window = root.AddComponent<BreakableWindow>();
            Configure(window, so =>
            {
                P(so, "_health").objectReferenceValue = health;
                P(so, "_collider").objectReferenceValue = col;
                P(so, "_pane").objectReferenceValue = pane;
                P(so, "_brokenSprite").objectReferenceValue = brokenSprite;
                P(so, "_shards").objectReferenceValue = shards;
            });
        }

        // ---------------------------------------------------------------- Обстановка

        // Обычный дом: высокое (шкафы, пульт охраны) — слой Wall, низкое (диваны, столы, остров) — Glass,
        // ковры, растения и техника на стенах — без коллайдера
        private static void BuildFurniture(Transform parent)
        {
            Transform furniture = new GameObject("Furniture").transform;
            furniture.SetParent(parent);
            Sprite plant = SpriteFactory.Plant();
            Sprite armchair = SpriteFactory.Armchair();
            Sprite sofa = SpriteFactory.ModernSofa();
            Sprite sign = SpriteFactory.NeonSign();

            // Прихожая: два шкафа у левой стены, растение в углу
            Sprite wardrobe = SpriteFactory.Wardrobe();
            Block(furniture, "Wardrobe_1", new Vector2(0.8f, 0.6f), new Vector2(1.5f, 5.4f), wardrobe);
            Block(furniture, "Wardrobe_2", new Vector2(1.95f, 0.6f), new Vector2(2.65f, 5.4f), wardrobe);
            Prop(furniture, "Plant", plant, new Vector2(9.9f, 0.6f), new Vector2(11.1f, 1.8f), 3);
            NeonSign(furniture, sign, new Vector2(6.9f, 5.6f), Teal);

            // Кабинет: стол у стекла, кресло, стеллаж у правой стены
            Prop(furniture, "Desk", SpriteFactory.BossDesk(), new Vector2(12.2f, 1.7f), new Vector2(13.5f, 4.2f), 3, GlassLayer, true, 90f);
            Prop(furniture, "DeskChair", SpriteFactory.OfficeChair(), new Vector2(13.7f, 2.4f), new Vector2(14.7f, 3.4f), 2, -1, true, 180f);
            Block(furniture, "Bookshelf", new Vector2(16.3f, 2.6f), new Vector2(16.9f, 5.6f), SpriteFactory.Shelf());

            // Гостиная: ряд мягкой мебели на ковре, телевизор на нижней стене
            Prop(furniture, "Rug_Living", SpriteFactory.Rug(), new Vector2(19.3f, 2f), new Vector2(31f, 4.1f), -97);
            Prop(furniture, "Couch", SpriteFactory.Couch(), new Vector2(19.8f, 2.4f), new Vector2(23.6f, 3.7f), 3, GlassLayer);
            Prop(furniture, "CoffeeTable", SpriteFactory.CoffeeTable(), new Vector2(24.4f, 2.5f), new Vector2(26.2f, 3.6f), 3, GlassLayer);
            Prop(furniture, "Armchair_1", armchair, new Vector2(27f, 2.4f), new Vector2(28.3f, 3.7f), 3, GlassLayer, true, 90f);
            Prop(furniture, "Armchair_2", armchair, new Vector2(29f, 2.4f), new Vector2(30.3f, 3.7f), 3, GlassLayer, true, 90f);
            Prop(furniture, "TV", SpriteFactory.Tv(), new Vector2(23.6f, 0.15f), new Vector2(27.4f, 0.55f), 6);

            // Кухня: остров с плитой и мойкой, два кресла у окна, растение
            Prop(furniture, "KitchenIsland", SpriteFactory.KitchenIsland(), new Vector2(3f, 13f), new Vector2(8.5f, 22.8f), 3, GlassLayer);
            Prop(furniture, "Armchair_K1", armchair, new Vector2(6f, 8.7f), new Vector2(7.5f, 10.2f), 3, GlassLayer, true, 90f);
            Prop(furniture, "Armchair_K2", armchair, new Vector2(7.7f, 8.7f), new Vector2(9.2f, 10.2f), 3, GlassLayer, true, 90f);
            Sprite stool = SpriteFactory.Stool();
            foreach (float y in new[] { 15.5f, 18f, 20.5f })
            {
                Prop(furniture, "Stool", stool, new Vector2(8.7f, y), new Vector2(9.5f, y + 0.8f), 2);
            }
            NeonSign(furniture, sign, new Vector2(0.45f, 16f), Magenta, -90f);

            // Лаунж наверху: длинные диваны под разными углами (чёрные полосы на схеме), растения, вывеска
            RotatedProp(furniture, "Sofa_A", sofa, new Vector2(14.5f, 23.3f), 3.6f, 1f, -37f, GlassLayer);
            RotatedProp(furniture, "Sofa_B", sofa, new Vector2(17.3f, 23.9f), 2.4f, 1f, 45f, GlassLayer);
            RotatedProp(furniture, "Sofa_C", sofa, new Vector2(24.9f, 23.7f), 5f, 1f, -17f, GlassLayer);
            RotatedProp(furniture, "Sofa_D", sofa, new Vector2(28.6f, 24.5f), 4.4f, 1f, 11f, GlassLayer);
            RotatedProp(furniture, "Sofa_E", sofa, new Vector2(29.2f, 22.9f), 3.5f, 1f, -17f, GlassLayer);
            Prop(furniture, "Rug_Lounge", SpriteFactory.Rug(), new Vector2(20f, 19.6f), new Vector2(27f, 22.2f), -97);
            Prop(furniture, "Plant", plant, new Vector2(12.2f, 24.4f), new Vector2(13.4f, 25.3f), 3);
            NeonSign(furniture, sign, new Vector2(22.8f, 25.45f), Magenta);
            Prop(furniture, "Speaker", SpriteFactory.Speaker(), new Vector2(20.5f, 24.6f), new Vector2(21.5f, 25.4f), 3);

            // Комната охраны: пульт с мониторами камер у правой стены, кресло, растение у двери в холл
            Block(furniture, "SecurityConsole", new Vector2(32.3f, 8.6f), new Vector2(33.45f, 13.4f), SpriteFactory.SecurityConsole());
            Prop(furniture, "ConsoleChair", SpriteFactory.OfficeChair(), new Vector2(31.1f, 10.5f), new Vector2(32.1f, 11.5f), 2);
            Prop(furniture, "Plant", plant, new Vector2(32.4f, 16.1f), new Vector2(33.4f, 16.9f), 3);
            Decor(furniture, "Clutter", SpriteFactory.Clutter(), new Vector2(30f, 8.4f), 1.2f);

            // Холл: лестница наверх, растения
            Prop(furniture, "Stairs", SpriteFactory.Stairs(), new Vector2(42.1f, 5.67f), new Vector2(46.5f, 8.67f), -98, -1, true, 90f);
            Prop(furniture, "Plant", plant, new Vector2(35f, 0.7f), new Vector2(36.2f, 1.9f), 3);
            Prop(furniture, "Plant", plant, new Vector2(35f, 14.6f), new Vector2(36.2f, 15.6f), 3);
            NeonSign(furniture, sign, new Vector2(40.5f, 15.75f), Teal);
        }

        // Бассейн с рыбками посреди зала: не пройти (слой Glass), но поверх видно и пули летят над водой (ShootThrough)
        private static void BuildPool(Transform parent)
        {
            Transform pool = new GameObject("Pool").transform;
            pool.SetParent(parent);
            Vector2 min = new Vector2(13.9f, 8.4f);
            Vector2 max = new Vector2(25.9f, 15.1f);
            Prop(pool, "Pool", SpriteFactory.Pool(), min, max, 3, GlassLayer).gameObject.AddComponent<ShootThrough>();
            Pulsing(Lamp(pool, "Pool_Light", 19.9f, 11.75f, Cyan, 9f, 0.9f), 0.6f, 0.6f, 1f);

            // Рыбки плавают внутри воды (бортик — 6 px из 128 по ширине и 6 из 80 по высоте, плюс запас)
            Rect water = WorldRect(min, max);
            water.xMin += 0.55f;
            water.xMax -= 0.55f;
            water.yMin += 0.5f;
            water.yMax -= 0.5f;
            Sprite[] kinds =
            {
                SpriteFactory.Fish("fish_koi", new Color(1f, 0.5f, 0.15f), new Color(0.97f, 0.95f, 0.92f)),
                SpriteFactory.Fish("fish_white", new Color(0.95f, 0.95f, 0.97f), new Color(0.95f, 0.2f, 0.15f)),
                SpriteFactory.Fish("fish_gold", new Color(1f, 0.8f, 0.2f), new Color(1f, 0.95f, 0.6f)),
                SpriteFactory.Fish("fish_neon", new Color(0.3f, 1f, 0.9f), new Color(0.1f, 0.5f, 0.6f)),
            };
            var rng = new System.Random(2);
            for (int i = 0; i < 15; i++)
            {
                Vector2 p = new Vector2(Mathf.Lerp(water.xMin, water.xMax, (float)rng.NextDouble()), Mathf.Lerp(water.yMin, water.yMax, (float)rng.NextDouble()));
                SpriteRenderer fish = CreateSprite("Fish", pool, kinds[i % kinds.Length], p, 4, true, (float)rng.NextDouble() * 360f);
                Fish swim = fish.gameObject.AddComponent<Fish>();
                Configure(swim, so =>
                {
                    P(so, "_area").rectValue = water;
                    P(so, "_speed").floatValue = 0.5f + (float)rng.NextDouble() * 0.5f;
                });
            }
        }

        // Предмет под произвольным углом: центр (схема), длина вдоль и ширина поперёк (схема)
        internal static void RotatedProp(Transform parent, string name, Sprite sprite, Vector2 center, float length, float width, float angle, int layer)
        {
            SpriteRenderer sr = CreateSprite(name, parent, sprite, ToWorld(center), 3, true, angle);
            Vector2 size = sprite.bounds.size;
            sr.transform.localScale = new Vector3(length * S / size.x, width * S / size.y, 1f);
            sr.gameObject.layer = layer;
            sr.gameObject.AddComponent<BoxCollider2D>();
        }

        private static void NeonSign(Transform parent, Sprite sprite, Vector2 schema, Color color, float angle = 0f)
        {
            SpriteRenderer sr = CreateSprite("NeonSign", parent, sprite, ToWorld(schema), 7, false, angle);
            sr.color = color;
            Pulsing(AddLight(sr.transform, "Glow", ToWorld(schema), color, 2.4f, 0.9f), 2.5f, 0.75f, 1f);
        }

        // ---------------------------------------------------------------- Камеры

        // Красные точки на схеме: позиция, куда смотрит, на сколько водит, дальность (мир)
        private static void BuildCameras(Transform parent, QuickhackData cameraOff, GameObject sparksFx, Transform enemies)
        {
            Transform root = new GameObject("Cameras").transform;
            root.SetParent(parent);
            Sprite head = SpriteFactory.SecurityCamera();
            Sprite mount = SpriteFactory.CameraMount();

            SecurityCam(root, "Camera_Lounge", new Vector2(33.2f, 25.1f), 215f, 35f, 9f, head, mount, cameraOff, sparksFx, enemies);
            // В зале с бассейном одна камера — в дальнем от входов углу
            SecurityCam(root, "Camera_Pool_NE", new Vector2(27.4f, 16.6f), 225f, 30f, 8f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Security", new Vector2(33.2f, 6.9f), 135f, 30f, 6f, head, mount, cameraOff, sparksFx, enemies);
            // Смотрит на проход в кухню, а не на входную дверь: иначе засекает героя ещё с улицы
            SecurityCam(root, "Camera_Entrance", new Vector2(10.8f, 5.3f), 165f, 25f, 7f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Living", new Vector2(17.9f, 5.3f), -20f, 25f, 9f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Hall_N", new Vector2(46f, 15.5f), 215f, 35f, 10f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Hall_S", new Vector2(46f, 0.8f), 145f, 35f, 10f, head, mount, cameraOff, sparksFx, enemies);
        }

        // ---------------------------------------------------------------- Охрана

        // Оранжевые закорючки — посты, пунктирные петли — маршруты. Петли в холле облетают дроны
        private static Health[] SpawnGuards(GameObject guard, GameObject drone, Transform parent)
        {
            var guards = new System.Collections.Generic.List<Health>();

            void Stand(Vector2 position, float facing)
            {
                guards.Add(SpawnEnemy(guard, parent, ToWorld(position), facing, Vector2.zero, 0f).GetComponent<Health>());
            }

            // Маршрут — абсолютные точки схемы по кругу, первая — старт
            void Loop(GameObject prefab, float speed, float facing, params Vector2[] route)
            {
                var perimeter = new Vector2[route.Length - 1];
                for (int i = 1; i < route.Length; i++)
                    perimeter[i - 1] = (route[i] - route[0]) * S;
                guards.Add(SpawnPerimeter(prefab, parent, ToWorld(route[0]), facing, speed, perimeter).GetComponent<Health>());
            }

            // Кухня: двое обходят комнату вокруг острова, с противоположных углов
            Loop(guard, PatrolSpeed, 0f, new Vector2(1.4f, 24.5f), new Vector2(10.2f, 24.5f), new Vector2(10.2f, 7.5f), new Vector2(1.4f, 7.5f));
            Loop(guard, PatrolSpeed, 180f, new Vector2(10.2f, 7.5f), new Vector2(1.4f, 7.5f), new Vector2(1.4f, 24.5f), new Vector2(10.2f, 24.5f));

            // Лаунж: пятеро на постах между диванами
            Stand(new Vector2(12.9f, 22.5f), 0f);
            Stand(new Vector2(19.6f, 24.7f), -90f);
            Stand(new Vector2(32.6f, 24.3f), 200f);
            Stand(new Vector2(22.1f, 18.7f), 180f);
            Stand(new Vector2(32.7f, 18.7f), 135f);

            // Зал с бассейном: четверо по углам вокруг воды
            Stand(new Vector2(12.9f, 15.9f), -45f);
            Stand(new Vector2(26.9f, 15.9f), -135f);
            Stand(new Vector2(17f, 7.5f), 90f);
            Stand(new Vector2(26.9f, 7.5f), 135f);

            // Комната охраны: двое у двери в лаунж и у двери в гостиную
            Stand(new Vector2(29.4f, 16.1f), -45f);
            Stand(new Vector2(29.4f, 7.4f), 45f);

            // Кабинет: один в углу у стеллажа
            Stand(new Vector2(15.8f, 1.3f), 135f);

            // Гостиная: двое обходят мебель по кругу
            Loop(guard, PatrolSpeed, 0f, new Vector2(18.3f, 1.2f), new Vector2(32.8f, 1.2f), new Vector2(32.8f, 4.9f), new Vector2(18.3f, 4.9f));
            Loop(guard, PatrolSpeed, 180f, new Vector2(32.8f, 4.9f), new Vector2(18.3f, 4.9f), new Vector2(18.3f, 1.2f), new Vector2(32.8f, 1.2f));

            // Холл: пост у лестницы и два дрона — верхняя и нижняя петли
            Stand(new Vector2(41.3f, 7.6f), 180f);
            Loop(drone, DroneSpeed, 0f, new Vector2(35.4f, 15f), new Vector2(45.5f, 15f), new Vector2(45.5f, 13.4f), new Vector2(38.6f, 13.4f),
                new Vector2(38.6f, 10.6f), new Vector2(35.4f, 10.6f));
            Loop(drone, DroneSpeed, 0f, new Vector2(35.4f, 7f), new Vector2(39.8f, 7f), new Vector2(39.8f, 3.6f), new Vector2(45.6f, 3.6f),
                new Vector2(45.6f, 1.3f), new Vector2(39f, 1.3f), new Vector2(39f, 5f), new Vector2(35.4f, 5f));

            return guards.ToArray();
        }

        // ---------------------------------------------------------------- Свет

        private static Light2D BuildLights()
        {
            Transform lights = new GameObject("Lights").transform;
            Light2D global = new GameObject("Global Light").AddComponent<Light2D>();
            global.transform.SetParent(lights);
            global.lightType = Light2D.LightType.Global;
            global.intensity = 0.3f;
            global.color = new Color(0.5f, 0.52f, 0.78f);

            Color green = new Color(0.3f, 1f, 0.5f);
            Color cold = new Color(0.8f, 0.9f, 1f);
            Color red = new Color(1f, 0.2f, 0.25f);

            Lamp(lights, "Entrance", 6f, 3f, Cyan, 8f, 0.6f);
            Lamp(lights, "Office", 14.3f, 3f, Warm, 6f, 0.7f);
            Lamp(lights, "Living", 25.5f, 3.2f, Warm, 12f, 0.6f);
            Pulsing(Lamp(lights, "Living_TV", 25.5f, 0.9f, new Color(0.75f, 0.4f, 1f), 5f, 0.9f), 11f, 0.6f, 1.1f);
            Lamp(lights, "Kitchen", 5.8f, 16f, Warm, 13f, 0.7f);
            Lamp(lights, "Kitchen_Hob", 4.5f, 21.5f, new Color(1f, 0.3f, 0.2f), 2.5f, 0.8f);
            Pulsing(Lamp(lights, "Lounge", 22.8f, 21.6f, Magenta, 14f, 0.6f), 0.8f, 0.45f, 0.7f);
            Lamp(lights, "Pool_Room", 20f, 11.75f, cold, 13f, 0.45f);
            Lamp(lights, "Security", 31f, 11.5f, green, 7f, 0.5f);
            Pulsing(Lamp(lights, "Security_Monitors", 32.6f, 11f, green, 3.5f, 0.9f), 6f, 0.6f, 1f);
            Lamp(lights, "Hall", 40.5f, 8f, cold, 16f, 0.55f);
            Pulsing(Lamp(lights, "Hall_Stairs", 44f, 7.2f, red, 5f, 0.7f), 1.2f, 0.35f, 0.8f);

            // Фонари на тротуаре
            foreach (Vector2 p in new[] { new Vector2(-4f, -4f), new Vector2(20f, -4.5f), new Vector2(50f, -4f), new Vector2(-4f, 29f),
                         new Vector2(25f, 30f), new Vector2(50f, 22f) })
            {
                Lamp(lights, "Street", p.x, p.y, Cyan, 9f, 0.5f);
            }
            return global;
        }
    }
}
