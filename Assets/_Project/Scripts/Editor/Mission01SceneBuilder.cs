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
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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

        internal const float S = 0.6f;
        private const float W = 46f;
        private const float H = 27f;
        private const float PatrolSpeed = 1.4f;

        // Второй этаж — копия здания по той же схеме, сдвинутая в мире вправо: камеры этажей не видят друг друга
        private static readonly Vector2 Floor2Origin = new Vector2(60f, 0f);

        // Точка схемы -> мир (s_origin — начало текущего этажа)
        internal static Vector2 V(float x, float y) => ToWorld(new Vector2(x, y));
        internal static Vector2 ToWorld(Vector2 schema) => schema * S + s_origin;

        private static Sprite s_wall;
        private static Vector2 s_origin;

        // Помощники ниже (Wall, Prop, AutoDoorAt, SecurityCam...) берут начало схемы и спрайт стен отсюда —
        // другие миссии с тем же масштабом S задают свои перед сборкой
        internal static void SetLayout(Vector2 origin, Sprite wall)
        {
            s_origin = origin;
            s_wall = wall;
        }

        [MenuItem("N89/Build Mission 01 Scene")]
        public static void Build()
        {
            EnsureLayers();
            Physics2D.gravity = Vector2.zero;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadCommonAssets();
            s_origin = Vector2.zero;
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
            }, 0.65f, 0.32f, common, 0, SpriteFactory.HeroDead());
            // Бандиты — люди с имплантами: истекают синей кровью, взламываются «Коротким замыканием»
            GameObject banditPrefab = EnemyPrefab("Bandit", "БАНДИТ", SpriteFactory.Bandit(), 1, 0.5f, 0.7f,
                common.EnemyBullet, common.Muzzle, common.Blood, common.BloodPool, common.ShortCircuit, common.Electric, true, SpriteFactory.BanditDead());

            // --- Сцена ---
            Camera cam = CreateCamera();
            CreateVolume("Mission01Volume");
            BuildLights();

            s_wall = SpriteFactory.GangWall();
            s_origin = Vector2.zero;
            Transform level = new GameObject("Level").transform;
            BuildCity(level, YardMin * S - Vector2.one * 2f, YardMax * S + Vector2.one * 2f, new Vector2(-36f, -32f), new Vector2(56f, 50f));
            BuildYard(level);
            BuildBase(level);
            BuildInterior(level);
            BuildClutter(level);
            BuildDoorsFloor1(level);
            // Отключение камеры дешевле замыкания — как взлом замка
            QuickhackData cameraOff = CreateQuickhack("CameraOff", "ОТКЛЮЧИТЬ КАМЕРУ", 4, 0.6f, QuickhackEffect.Disable);
            Transform enemiesRoot = new GameObject("Bandits").transform;
            BuildCameras(level, cameraOff, common.Electric, enemiesRoot);
            Health[] enemies = SpawnBandits(banditPrefab, enemiesRoot);
            // Маркер у лестницы наверх — загорается, когда первый этаж зачищен
            Light2D stairsMarker = AddLight(level, "StairsMarker", V(3.6f, 25.4f), new Color(0.3f, 1f, 1f), 2.5f, 1.2f);
            Pulsing(stairsMarker, 3f, 0.4f, 1.4f);
            Rect stairsZone = WorldRect(new Vector2(2.2f, 24f), new Vector2(5f, 26.9f));

            // --- Второй этаж ---
            s_origin = Floor2Origin;
            Transform floor2 = new GameObject("Floor2").transform;
            BuildFloor2(floor2);
            BuildDoorsFloor2(floor2);
            Transform floor2Enemies = new GameObject("Bandits_Floor2").transform;
            BuildFloor2Cameras(floor2, cameraOff, common.Electric, floor2Enemies);
            Health[] enemies2 = SpawnFloor2Bandits(banditPrefab, floor2Enemies);
            Transform floor2Spawn = new GameObject("Floor2Spawn").transform;
            floor2Spawn.SetParent(floor2);
            floor2Spawn.position = V(3.7f, 21.4f);
            Transform target = BuildTarget(floor2, out SpriteRenderer targetBody);
            Vector2 deskPoint = V(31.1f, 4.4f);
            Rect officeZone = WorldRect(new Vector2(19.2f, 0.5f), new Vector2(45.5f, 11.1f));
            Vector2 officeDoor = V(34.1f, 11.6f);
            s_origin = Vector2.zero;

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
            // Имплант миссии 1 фиксированный — «Руки гориллы»
            AddGorillaArms(hero, follow, common);
            cam.transform.position = new Vector3(start.x, start.y, -10f);

            // --- UI и сценарий ---
            HudRefs hud = BuildHud(hero.GetComponent<HackingController>(), mono, audiowide, "МИССИЯ 1 ВЫПОЛНЕНА", "Interlude02");
            Banner banner = BuildBanner(hud.Fader.transform, mono);
            WaypointArrow waypoint = BuildWaypoint(hero.transform);

            MissionDirector director = new GameObject("MissionDirector").AddComponent<MissionDirector>();
            Configure(director, so =>
            {
                P(so, "_player").objectReferenceValue = hero.GetComponent<PlayerController>();
                P(so, "_hacking").objectReferenceValue = hero.GetComponent<HackingController>();
                P(so, "_weapon").objectReferenceValue = hero.GetComponent<Weapon>();
                P(so, "_heroBody").objectReferenceValue = hero.transform.Find("Body").GetComponent<SpriteRenderer>();
                SetArray(so, "_weapons", weapons);
                SetArray(so, "_enemies", enemies);
                P(so, "_cameraFollow").objectReferenceValue = follow;

                P(so, "_objectiveText").stringValue = "Зачисти первый этаж";
                SetArray(so, "_floor2Enemies", enemies2);
                P(so, "_stairsZone").rectValue = stairsZone;
                P(so, "_stairsMarker").objectReferenceValue = stairsMarker;
                P(so, "_floor2Spawn").objectReferenceValue = floor2Spawn;
                P(so, "_floor2Facing").floatValue = 0f;
                P(so, "_officeZone").rectValue = officeZone;
                P(so, "_officeDoor").vector2Value = officeDoor;
                P(so, "_banner").objectReferenceValue = banner;
                P(so, "_waypoint").objectReferenceValue = waypoint;

                P(so, "_target").objectReferenceValue = target;
                P(so, "_targetBody").objectReferenceValue = targetBody;
                P(so, "_targetStanding").objectReferenceValue = SpriteFactory.TargetStanding();
                P(so, "_targetDead").objectReferenceValue = SpriteFactory.TargetDead();
                P(so, "_deskPoint").vector2Value = deskPoint;
                P(so, "_bloodFxPrefab").objectReferenceValue = common.Blood;
                P(so, "_bloodPoolPrefab").objectReferenceValue = common.BloodPool;

                P(so, "_fader").objectReferenceValue = hud.Fader;
                P(so, "_objective").objectReferenceValue = hud.Objective;
                P(so, "_hackingHud").objectReferenceValue = hud.HackingHud;
                P(so, "_endScreen").objectReferenceValue = hud.EndScreen;
                P(so, "_subtitles").objectReferenceValue = hud.Subtitles;
                P(so, "_letterboxTop").objectReferenceValue = hud.LetterboxTop;
                P(so, "_letterboxBottom").objectReferenceValue = hud.LetterboxBottom;
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

            // Левая комната (раздевалка): три ряда шкафчиков
            Sprite lockers = SpriteFactory.Lockers();
            Block(walls, "Lockers_1", new Vector2(0f, 7.6f), new Vector2(1.25f, 14.9f), lockers);
            Block(walls, "Lockers_2", new Vector2(2.7f, 7.6f), new Vector2(3.9f, 14.9f), lockers);
            Block(walls, "Lockers_3", new Vector2(5.4f, 7.6f), new Vector2(6.5f, 14.9f), lockers);
        }

        // Интерьер по схеме. Высокое (шкафы, автомат) — слой Wall: закрывает обзор.
        // Низкое (стол, диван, скамьи) — слой Glass: не пройти и пули не пробивают, но поверх видно
        private static void BuildInterior(Transform parent)
        {
            Transform interior = new GameObject("Interior").transform;
            interior.SetParent(parent);

            // Полы комнат поверх бетона
            Floor(interior, "Floor_Chill", SpriteFactory.Carpet(), V(32.2f, 20.25f), V(W, H), -99);
            Floor(interior, "Floor_Discussion", SpriteFactory.WoodFloor(), V(10.3f, 0f), V(29.5f, 8.2f), -99);
            Floor(interior, "Floor_Gym", SpriteFactory.GymFloor(), V(29.5f, 0f), V(W, 8.2f), -99);

            // Лестница: два пролёта по сторонам перил
            Sprite stairs = SpriteFactory.Stairs();
            Prop(interior, "Stairs_Upper", stairs, new Vector2(2.2f, 24f), new Vector2(5f, 26.9f), -98);
            Prop(interior, "Stairs_Lower", stairs, new Vector2(2.2f, 20.35f), new Vector2(5f, 23.5f), -98);

            // Chill room: диван у нижней стены, аркадный автомат у правой
            Prop(interior, "Sofa", SpriteFactory.GangSofa(), new Vector2(32.7f, 20.45f), new Vector2(40.5f, 22.4f), 3, GlassLayer);
            Prop(interior, "Arcade", SpriteFactory.Arcade(), new Vector2(44f, 22.4f), new Vector2(45.75f, 25.8f), 3, WallLayer);

            // Discussion room: стол, на краю проектор, луч на экран на стене качалки
            Prop(interior, "Table", SpriteFactory.ConferenceTable(), new Vector2(13.8f, 2.6f), new Vector2(24.3f, 5.8f), 3, GlassLayer);
            Prop(interior, "Projector", SpriteFactory.Projector(), new Vector2(23.1f, 3.7f), new Vector2(24.1f, 4.7f), 4);
            Prop(interior, "ProjectorBeam", SpriteFactory.ProjectorBeam(), new Vector2(24.1f, 3f), new Vector2(29.2f, 5.4f), 6, -1, false);
            Prop(interior, "ProjectorScreen", SpriteFactory.ProjectorScreen(), new Vector2(29f, 2.9f), new Vector2(29.25f, 5.5f), 6, -1, false);

            // Качалка: две скамьи для жима, штанги слева. Коллайдер — только у скамьи, штанга не мешает пройти
            Sprite bench = SpriteFactory.BenchPress();
            Prop(interior, "Bench_1", bench, new Vector2(30.1f, 4.4f), new Vector2(33.9f, 6f), 3);
            Solid(interior, "Bench_1_Collider", new Vector2(30.95f, 4.95f), new Vector2(33.8f, 5.45f), GlassLayer);
            Prop(interior, "Bench_2", bench, new Vector2(30.1f, 2f), new Vector2(33.9f, 3.6f), 3);
            Solid(interior, "Bench_2_Collider", new Vector2(30.95f, 2.55f), new Vector2(33.8f, 3.05f), GlassLayer);
        }

        // ---------------------------------------------------------------- Камеры

        // Камеры наблюдения — чёрные точки на схеме: позиция (юниты схемы), куда смотрит, на сколько водит, дальность (мир)
        private static void BuildCameras(Transform parent, QuickhackData cameraOff, GameObject sparksFx, Transform enemies)
        {
            Transform root = new GameObject("Cameras").transform;
            root.SetParent(parent);
            Sprite head = SpriteFactory.SecurityCamera();
            Sprite mount = SpriteFactory.CameraMount();

            SecurityCam(root, "Camera_TopRoom", new Vector2(10.3f, 26.3f), -25f, 40f, 8f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Chill", new Vector2(45.2f, 26.3f), 215f, 35f, 7f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Corridor", new Vector2(0.7f, 19.6f), -8f, 20f, 11f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Lockers", new Vector2(9.6f, 14.4f), 225f, 35f, 7f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Hall", new Vector2(45.2f, 14.4f), 195f, 25f, 11f, head, mount, cameraOff, sparksFx, enemies);
        }

        internal static void SecurityCam(Transform parent, string name, Vector2 schemaPosition, float angle, float sweep, float range,
            Sprite headSprite, Sprite mountSprite, QuickhackData cameraOff, GameObject sparksFx, Transform enemies)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = V(schemaPosition.x, schemaPosition.y);
            CircleCollider2D col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            CreateSprite("Mount", root.transform, mountSprite, Vector2.zero, 6);
            SpriteRenderer head = CreateSprite("Head", root.transform, headSprite, Vector2.zero, 7, true, angle);
            SpriteRenderer led = CreateSprite("Led", head.transform, SpriteFactory.White(), new Vector2(-0.12f, 0f), 8, false);
            led.transform.localScale = Vector3.one * 0.35f;
            Light2D light = AddLight(root.transform, "Light", root.transform.position, Color.green, 1.2f, 0.7f);

            var cone = new GameObject("Cone");
            cone.transform.SetParent(root.transform, false);
            MeshFilter filter = cone.AddComponent<MeshFilter>();
            MeshRenderer renderer = cone.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = UnlitMaterial;
            renderer.sortingOrder = -50;

            SecurityCamera cam = root.AddComponent<SecurityCamera>();
            Configure(cam, so =>
            {
                P(so, "_head").objectReferenceValue = head.transform;
                P(so, "_led").objectReferenceValue = led;
                P(so, "_light").objectReferenceValue = light;
                P(so, "_coneFilter").objectReferenceValue = filter;
                P(so, "_coneRenderer").objectReferenceValue = renderer;
                P(so, "_baseAngle").floatValue = angle;
                P(so, "_sweepAngle").floatValue = sweep;
                P(so, "_range").floatValue = range;
                P(so, "_sightBlockers").intValue = (1 << WallLayer) | (1 << DoorLayer);
                // Тревога поднимает только свой этаж
                P(so, "_enemiesRoot").objectReferenceValue = enemies;
            });

            HackableDevice device = root.AddComponent<HackableDevice>();
            Configure(device, so =>
            {
                P(so, "_deviceName").stringValue = "КАМЕРА НАБЛЮДЕНИЯ";
                SetArray(so, "_quickhacks", cameraOff);
                P(so, "_collider").objectReferenceValue = col;
                SetArray(so, "_highlightRenderers", head);
                P(so, "_shortCircuitFxPrefab").objectReferenceValue = sparksFx;
            });
        }

        // Двор вокруг здания: асфальт и забор — между E1 и E2 можно пройти снаружи
        // Двор закрытой базы: сетка с колючкой, мешки с песком у входа, контейнер, генератор, бочки с огнём,
        // прожекторы по углам, предупреждающие таблички и мусор
        private static void BuildYard(Transform parent)
        {
            Transform yard = new GameObject("Yard").transform;
            yard.SetParent(parent);
            Vector2 min = YardMin;
            Vector2 max = YardMax;
            float t = WallThickness * 0.5f / S;
            Floor(yard, "Asphalt", SpriteFactory.Asphalt(), min * S, max * S, -101);
            Sprite fence = SpriteFactory.Fence();
            FenceWall(yard, "Fence_S", min.x - t, min.y, max.x + t, min.y, fence);
            FenceWall(yard, "Fence_N", min.x - t, max.y, max.x + t, max.y, fence);
            FenceWall(yard, "Fence_W", min.x, min.y - t, min.x, max.y + t, fence);
            FenceWall(yard, "Fence_E", max.x, min.y - t, max.x, max.y + t, fence);

            // Укрепления у входа E1 — проход к двери между мешками
            Sprite sandbags = SpriteFactory.Sandbags();
            Prop(yard, "Sandbags_W", sandbags, new Vector2(0.3f, -3.2f), new Vector2(3.6f, -1.9f), 3, GlassLayer);
            Prop(yard, "Sandbags_E", sandbags, new Vector2(7.4f, -3.2f), new Vector2(10.7f, -1.9f), 3, GlassLayer);
            Prop(yard, "Sandbags_E2", sandbags, new Vector2(48.3f, 14.4f), new Vector2(49.6f, 20.6f), 3, GlassLayer, true, 90f);

            Prop(yard, "Container", SpriteFactory.ShippingContainer(), new Vector2(-8.5f, 6f), new Vector2(-5f, 18f), 3, WallLayer);
            Prop(yard, "Generator", SpriteFactory.Generator(), new Vector2(49.5f, 3f), new Vector2(53.5f, 6.2f), 3, GlassLayer);
            Pulsing(Lamp(yard, "Generator_Lamp", 53.2f, 6f, new Color(1f, 0.2f, 0.2f), 2f, 0.9f), 5f, 0.1f, 0.9f);
            Sprite crate = SpriteFactory.Crate();
            Prop(yard, "Crate_1", crate, new Vector2(-7.5f, 30f), new Vector2(-5.5f, 32f), 3, WallLayer);
            Prop(yard, "Crate_2", crate, new Vector2(-5.3f, 30.2f), new Vector2(-3.7f, 31.8f), 3, WallLayer);
            Prop(yard, "Crate_3", crate, new Vector2(52f, 30f), new Vector2(54f, 32f), 3, WallLayer);

            Sprite barrel = SpriteFactory.FireBarrel();
            foreach (Vector2 p in new[] { new Vector2(-4f, 26f), new Vector2(51f, 25f), new Vector2(20f, -6.5f) })
            {
                FireBarrel(yard, barrel, p);
            }

            // Прожекторы по углам двора
            Sprite flood = SpriteFactory.Floodlight();
            Color floodColor = new Color(0.85f, 0.9f, 1f);
            foreach (Vector2 p in new[] { new Vector2(-9f, -9f), new Vector2(55f, -9f), new Vector2(-9f, 36f), new Vector2(55f, 36f) })
            {
                Prop(yard, "Floodlight", flood, p - Vector2.one * 0.8f, p + Vector2.one * 0.8f, 6);
                Lamp(yard, "Floodlight_Light", p.x, p.y, floodColor, 16f, 0.55f);
            }

            Sprite sign = SpriteFactory.WarningSign();
            foreach (Vector2 p in new[] { new Vector2(14f, -10f), new Vector2(32f, -10f), new Vector2(20f, 37f), new Vector2(-10f, 20f), new Vector2(56f, 8f) })
            {
                Prop(yard, "Sign", sign, p - new Vector2(1f, 0.6f), p + new Vector2(1f, 0.6f), 6);
            }

            Sprite trash = SpriteFactory.TrashPile();
            Sprite stain = SpriteFactory.OilStain();
            foreach (Vector2 p in new[] { new Vector2(24f, -8f), new Vector2(48f, -8.3f), new Vector2(-7f, 22f), new Vector2(30f, 34f) })
            {
                Decor(yard, "Trash", trash, p, 2.6f);
            }
            foreach (Vector2 p in new[] { new Vector2(12f, -6f), new Vector2(51f, 11f), new Vector2(-6f, 2f), new Vector2(40f, 33f) })
            {
                Decor(yard, "Stain", stain, p, 3.5f);
            }
        }

        private static readonly Vector2 YardMin = new Vector2(-10f, -10f);
        private static readonly Vector2 YardMax = new Vector2(W + 10f, H + 10f);

        private static void FenceWall(Transform parent, string name, float x1, float y1, float x2, float y2, Sprite sprite)
        {
            CreateWall(parent, name, V(x1, y1), V(x2, y2), sprite, WallLayer, 5);
        }

        // Бочка с огнём: невысокая (слой Glass), даёт тёплый мерцающий свет
        private static void FireBarrel(Transform parent, Sprite sprite, Vector2 schema)
        {
            Prop(parent, "FireBarrel", sprite, schema - Vector2.one * 0.65f, schema + Vector2.one * 0.65f, 3, GlassLayer);
            Pulsing(Lamp(parent, "FireBarrel_Light", schema.x, schema.y, new Color(1f, 0.55f, 0.2f), 6f, 0.9f), 9f, 0.6f, 1f);
        }

        // Декор на полу без коллайдера: центр и размер по ширине (юниты схемы). Поворот случайный в пределах
        // maxAngle в обе стороны (граффити — чуть наклонено, чтобы читалось; мусор — как угодно)
        internal static void Decor(Transform parent, string name, Sprite sprite, Vector2 schema, float size, float maxAngle = 180f)
        {
            Vector2 bounds = sprite.bounds.size;
            float aspect = bounds.y / bounds.x;
            Vector2 half = new Vector2(size, size * aspect) * 0.5f;
            SpriteRenderer sr = Prop(parent, name, sprite, schema - half, schema + half, -96);
            sr.transform.rotation = Quaternion.Euler(0f, 0f, (Hash(schema) * 2f - 1f) * maxAngle);
        }

        private static float Hash(Vector2 p) => Mathf.Repeat(Mathf.Sin(p.x * 12.9898f + p.y * 78.233f) * 43758.5453f, 1f);

        // ---------------------------------------------------------------- Грязь внутри

        // Логово бандитов: мусор, бутылки, коробки из-под пиццы, окурки, пятна, граффити на полу, матрас.
        // Всё — декор без коллайдеров, чтобы не мешать патрулям и проходам; кроме одной бочки с огнём
        private static void BuildClutter(Transform parent)
        {
            Transform clutter = new GameObject("Clutter").transform;
            clutter.SetParent(parent);
            Sprite trash = SpriteFactory.TrashPile();
            Sprite pizza = SpriteFactory.PizzaBox();
            Sprite bottles = SpriteFactory.Bottles();
            Sprite butts = SpriteFactory.Butts();
            Sprite stain = SpriteFactory.OilStain();
            Sprite tagMagenta = SpriteFactory.Graffiti("graffiti_magenta", new Color(1f, 0.25f, 0.8f, 0.85f));
            Sprite tagToxic = SpriteFactory.Graffiti("graffiti_toxic", new Color(0.45f, 1f, 0.3f, 0.8f));

            // Верхняя комната — здесь спят: матрас, бочка с огнём, коробки, бутылки
            Decor(clutter, "Mattress", SpriteFactory.Mattress(), new Vector2(20.5f, 25.4f), 4.2f);
            Decor(clutter, "Bottles", bottles, new Vector2(17.8f, 25.8f), 1.6f);
            Decor(clutter, "Pizza", pizza, new Vector2(23.8f, 24.2f), 1.5f);
            Decor(clutter, "Trash", trash, new Vector2(14.5f, 26f), 2.6f);
            Decor(clutter, "Butts", butts, new Vector2(16.5f, 22.4f), 1f);
            Decor(clutter, "Tag", tagToxic, new Vector2(21f, 22f), 4.5f, 15f);
            FireBarrel(clutter, SpriteFactory.FireBarrel(), new Vector2(27.2f, 25.6f));

            // Chill room — после вечеринки
            Decor(clutter, "Bottles", bottles, new Vector2(35.2f, 23.3f), 1.6f);
            Decor(clutter, "Bottles", bottles, new Vector2(39.6f, 24.6f), 1.4f);
            Decor(clutter, "Pizza", pizza, new Vector2(37.5f, 23.4f), 1.5f);
            Decor(clutter, "Pizza", pizza, new Vector2(42.2f, 25.6f), 1.4f);
            Decor(clutter, "Stain", stain, new Vector2(40.5f, 24.2f), 2.6f);
            Decor(clutter, "Butts", butts, new Vector2(34.4f, 25.8f), 1f);

            // Коридор — граффити и окурки
            Decor(clutter, "Tag", tagMagenta, new Vector2(8f, 17.6f), 5f, 15f);
            Decor(clutter, "Tag", tagToxic, new Vector2(38f, 17.4f), 4.5f, 15f);
            Decor(clutter, "Stain", stain, new Vector2(20f, 16.8f), 3f);
            Decor(clutter, "Butts", butts, new Vector2(30.5f, 19.4f), 1f);
            Decor(clutter, "Butts", butts, new Vector2(2f, 16f), 1f);

            // Раздевалка у входа
            Decor(clutter, "Trash", trash, new Vector2(8.6f, 5.6f), 2.4f);
            Decor(clutter, "Stain", stain, new Vector2(4.5f, 4f), 3f);
            Decor(clutter, "Butts", butts, new Vector2(2f, 5.6f), 1f);
            Decor(clutter, "Bottles", bottles, new Vector2(8.8f, 13.4f), 1.4f);

            // Большой зал — тег банды на весь пол, пятна
            Decor(clutter, "Tag", tagMagenta, new Vector2(27.5f, 11.5f), 9f, 15f);
            Decor(clutter, "Stain", stain, new Vector2(15f, 12.5f), 3.2f);
            Decor(clutter, "Stain", stain, new Vector2(40f, 10.2f), 3f);
            Decor(clutter, "Trash", trash, new Vector2(44.6f, 14f), 2f);

            // Переговорная — окурки и бутылки у стола
            Decor(clutter, "Butts", butts, new Vector2(12.2f, 7.2f), 1f);
            Decor(clutter, "Bottles", bottles, new Vector2(26.8f, 1.2f), 1.4f);
            Decor(clutter, "Pizza", pizza, new Vector2(11.6f, 1.3f), 1.4f);

            // Качалка — гантели и пятно пота
            Decor(clutter, "Dumbbells", SpriteFactory.Dumbbells(), new Vector2(36f, 6.4f), 1.8f);
            Decor(clutter, "Dumbbells", SpriteFactory.Dumbbells(), new Vector2(41.5f, 3.8f), 1.6f);
            Decor(clutter, "Stain", stain, new Vector2(39f, 1.6f), 2.4f);
        }

        // ---------------------------------------------------------------- Город за забором

        // Крыши мегаблоков вокруг базы: кварталы по сетке, между ними тёмные улицы. На крышах вентблоки
        // и неоновые вывески со светом. Чисто фон — без коллайдеров, за забором всё равно не пройти.
        // Координаты мировые: кварталы не ставятся внутри fenceMin..fenceMax (сам участок с забором)
        internal static void BuildCity(Transform parent, Vector2 fenceMin, Vector2 fenceMax, Vector2 areaMin, Vector2 areaMax)
        {
            Transform city = new GameObject("City").transform;
            city.SetParent(parent);
            var rng = new System.Random(89);

            Floor(city, "Streets", SpriteFactory.Asphalt(), areaMin, areaMax, -110);

            Color[] tints = { new Color(0.14f, 0.14f, 0.17f), new Color(0.17f, 0.15f, 0.16f), new Color(0.12f, 0.15f, 0.17f), new Color(0.19f, 0.18f, 0.2f) };
            var roofs = new Sprite[tints.Length];
            for (int i = 0; i < tints.Length; i++) roofs[i] = SpriteFactory.RoofTile($"roof_{i}", tints[i]);
            Sprite unit = SpriteFactory.RoofUnit();
            Sprite neon = SpriteFactory.RoofNeon();
            Color[] neonColors = { new Color(1f, 0.25f, 0.8f), new Color(0.3f, 1f, 1f), new Color(1f, 0.85f, 0.2f), new Color(0.45f, 1f, 0.3f), new Color(1f, 0.25f, 0.3f) };

            const float cell = 10f;
            for (float x = areaMin.x; x < areaMax.x; x += cell)
            for (float y = areaMin.y; y < areaMax.y; y += cell)
            {
                // Квартал: отступ под улицу, случайный размер
                float w = 6.5f + (float)rng.NextDouble() * 2.3f;
                float h = 6.5f + (float)rng.NextDouble() * 2.3f;
                Vector2 bMin = new Vector2(x + 1.2f, y + 1.2f);
                Vector2 bMax = bMin + new Vector2(w, h);
                if (bMax.x > areaMax.x) continue;
                // Не налезать на двор базы
                if (bMax.x > fenceMin.x && bMin.x < fenceMax.x && bMax.y > fenceMin.y && bMin.y < fenceMax.y) continue;

                SpriteRenderer roof = CreateSprite("Roof", city, roofs[rng.Next(roofs.Length)], (bMin + bMax) * 0.5f, -108);
                roof.drawMode = SpriteDrawMode.Tiled;
                roof.size = bMax - bMin;

                int units = 1 + rng.Next(3);
                for (int i = 0; i < units; i++)
                {
                    Vector2 p = new Vector2(Mathf.Lerp(bMin.x + 1f, bMax.x - 1f, (float)rng.NextDouble()), Mathf.Lerp(bMin.y + 1f, bMax.y - 1f, (float)rng.NextDouble()));
                    SpriteRenderer u = CreateSprite("Unit", city, unit, p, -107);
                    u.transform.localScale = Vector3.one * 1.4f;
                }

                if (rng.NextDouble() < 0.6)
                {
                    Color color = neonColors[rng.Next(neonColors.Length)];
                    Vector2 p = new Vector2((bMin.x + bMax.x) * 0.5f, bMax.y - 0.6f);
                    SpriteRenderer sign = CreateSprite("Neon", city, neon, p, -106, false);
                    sign.color = color;
                    sign.transform.localScale = Vector3.one * 1.5f;
                    Light2D glow = AddLight(city, "NeonLight", p, color, 4.5f, 0.9f);
                    if (rng.NextDouble() < 0.35) Pulsing(glow, 2f + (float)rng.NextDouble() * 3f, 0.3f, 0.9f);
                }
            }
        }

        // ---------------------------------------------------------------- Автодвери

        // Раздвижная дверь в проёме: from..to — края проёма вдоль стены (юниты схемы), at — координата стены
        internal static void AutoDoorAt(Transform parent, string name, bool horizontal, float from, float to, float at)
        {
            Vector2 a = horizontal ? V(from, at) : V(at, from);
            Vector2 b = horizontal ? V(to, at) : V(at, to);
            float width = Vector2.Distance(a, b);
            float thickness = WallThickness * 0.7f;

            var root = new GameObject(name) { layer = DoorLayer };
            root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation((a + b) * 0.5f, Quaternion.Euler(0f, 0f, horizontal ? 0f : 90f));
            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(width, thickness);

            Sprite panel = SpriteFactory.DoorPanelSci();
            SpriteRenderer Panel(string side, float x)
            {
                SpriteRenderer sr = CreateSprite(side, root.transform, panel, new Vector2(x, 0f), 4);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width * 0.5f, thickness);
                return sr;
            }

            SpriteRenderer left = Panel("Left", -width * 0.25f);
            SpriteRenderer right = Panel("Right", width * 0.25f);
            // Лампочки статуса на обоих концах проёма
            SpriteRenderer ledA = CreateSprite("LedA", root.transform, SpriteFactory.White(), new Vector2(-width * 0.5f, 0f), 6, false);
            SpriteRenderer ledB = CreateSprite("LedB", root.transform, SpriteFactory.White(), new Vector2(width * 0.5f, 0f), 6, false);
            ledA.transform.localScale = ledB.transform.localScale = Vector3.one * 0.5f;
            Light2D light = AddLight(root.transform, "Light", root.transform.position, new Color(1f, 0.25f, 0.2f), 1.4f, 0.6f);

            AutoDoor door = root.AddComponent<AutoDoor>();
            Configure(door, so =>
            {
                P(so, "_left").objectReferenceValue = left.transform;
                P(so, "_right").objectReferenceValue = right.transform;
                P(so, "_collider").objectReferenceValue = col;
                P(so, "_light").objectReferenceValue = light;
                SetArray(so, "_statusLeds", ledA, ledB);
                P(so, "_slide").floatValue = width * 0.5f;
                P(so, "_actors").intValue = (1 << PlayerLayer) | (1 << EnemyLayer);
            });
        }

        private static void BuildDoorsFloor1(Transform parent)
        {
            Transform doors = new GameObject("Doors").transform;
            doors.SetParent(parent);
            AutoDoorAt(doors, "Door_TopRooms_Corridor", true, 24.8f, 28.7f, 20.25f);
            AutoDoorAt(doors, "Door_Corridor_Hall", true, 26.2f, 30f, 15f);
            AutoDoorAt(doors, "Door_Hall_Discussion", true, 21.7f, 25.1f, 8.2f);
            AutoDoorAt(doors, "Door_Hall_Gym", true, 42.4f, 46f, 8.2f);
            AutoDoorAt(doors, "Door_Stairs_TopRoom", false, 20.25f, 22.8f, 9.4f);
            AutoDoorAt(doors, "Door_TopRoom_Chill", false, 24.2f, 27f, 32.2f);
            AutoDoorAt(doors, "Door_Lockers_Hall", false, 8.3f, 11.2f, 10.3f);
        }

        private static void BuildDoorsFloor2(Transform parent)
        {
            Transform doors = new GameObject("Doors").transform;
            doors.SetParent(parent);
            AutoDoorAt(doors, "Door_Stairs_Hall", false, 21f, 23.8f, 7.9f);
            AutoDoorAt(doors, "Door_Hall_Lounge", true, 12.1f, 14.8f, 19.9f);
            AutoDoorAt(doors, "Door_Hall_Corridor_W", true, 23.75f, 26.5f, 19.9f);
            AutoDoorAt(doors, "Door_Hall_Corridor_E", true, 34.4f, 37.3f, 19.9f);
            AutoDoorAt(doors, "Door_Lounge_Office", false, 8.5f, 11.4f, 18.5f);
            AutoDoorAt(doors, "Door_Corridor_Office", true, 32.6f, 35.6f, 11.6f);
        }

        // ---------------------------------------------------------------- Бандиты

        private static Health[] SpawnBandits(GameObject prefab, Transform parent)
        {
            var bandits = new System.Collections.Generic.List<Health>();

            void Stand(Vector2 position, float facing)
            {
                bandits.Add(SpawnEnemy(prefab, parent, ToWorld(position), facing, Vector2.zero, 0f).GetComponent<Health>());
            }

            void Loop(Vector2 position, float facing, params Vector2[] perimeter)
            {
                for (int i = 0; i < perimeter.Length; i++)
                    perimeter[i] *= S;
                bandits.Add(SpawnPerimeter(prefab, parent, ToWorld(position), facing, PatrolSpeed, perimeter).GetComponent<Health>());
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

        // ---------------------------------------------------------------- Второй этаж

        // Второй этаж по схеме «2 floor»: та же коробка 46 x 27 без выходов наружу.
        // Сверху слева — лестница, вдоль верха — длинный зал, слева внизу — гостиная, справа — коридор
        // с двумя камерами и под ним кабинет главаря (цель миссии). Строится при s_origin = Floor2Origin
        private static void BuildFloor2(Transform parent)
        {
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(parent);
            Transform interior = new GameObject("Interior").transform;
            interior.SetParent(parent);
            float t = WallThickness * 0.5f / S;

            Floor(parent, "Floor", SpriteFactory.ConcreteFloor(), V(0f, 0f), V(W, H), -100);
            Floor(interior, "Floor_Lounge", SpriteFactory.Carpet(), V(0f, 0f), V(18.5f, 19.9f), -99);
            Floor(interior, "Floor_Office", SpriteFactory.WoodFloor(), V(18.5f, 0f), V(W, 11.6f), -99);

            // Внешние стены — сплошные
            Wall(walls, "Outer_Bottom", -t, 0f, W + t, 0f);
            Wall(walls, "Outer_Top", -t, H, W + t, H);
            Wall(walls, "Outer_Left", 0f, -t, 0f, H + t);
            Wall(walls, "Outer_Right", W, -t, W, H + t);

            // Лестничная клетка: глухой блок слева, стена справа с проёмом в зал
            Block(walls, "Stairwell", new Vector2(0f, 19.9f), new Vector2(2.6f, H));
            Wall(walls, "V1_S", 7.9f, 19.9f, 7.9f, 21f);
            Wall(walls, "V1_N", 7.9f, 23.8f, 7.9f, H);
            // Зал / нижняя часть (y 19.9): три проёма — в гостиную и два в коридор
            Wall(walls, "H1_A", 0f, 19.9f, 12.1f, 19.9f);
            Wall(walls, "H1_B", 14.8f, 19.9f, 23.75f, 19.9f);
            Wall(walls, "H1_C", 26.5f, 19.9f, 34.4f, 19.9f);
            Wall(walls, "H1_D", 37.3f, 19.9f, W, 19.9f);
            // Гостиная / правая часть (x 18.5): проём в кабинет
            Wall(walls, "V2_S", 18.5f, 0f, 18.5f, 8.5f);
            Wall(walls, "V2_N", 18.5f, 11.4f, 18.5f, 19.9f);
            // Коридор / кабинет (y 11.6): проём в кабинет
            Wall(walls, "H2_W", 18.5f, 11.6f, 32.6f, 11.6f);
            Wall(walls, "H2_E", 35.6f, 11.6f, W, 11.6f);

            // Лестница вниз — два пролёта
            Sprite stairs = SpriteFactory.Stairs();
            Prop(interior, "Stairs_Upper", stairs, new Vector2(2.7f, 24.2f), new Vector2(4.7f, 26.7f), -98);
            Prop(interior, "Stairs_Lower", stairs, new Vector2(2.7f, 20.2f), new Vector2(4.7f, 22.9f), -98);

            // Зал: ящики в углу у лестницы
            Sprite crate = SpriteFactory.Crate();
            Prop(interior, "Crate_1", crate, new Vector2(8.3f, 25f), new Vector2(9.6f, 26.6f), 3, WallLayer);
            Prop(interior, "Crate_2", crate, new Vector2(9.7f, 25.3f), new Vector2(10.7f, 26.5f), 3, WallLayer);
            Prop(interior, "Crate_3", crate, new Vector2(8.3f, 23.8f), new Vector2(9.4f, 24.9f), 3, WallLayer);

            // Гостиная: ковёр, столик, диваны по сторонам, барная стойка у правой стены
            Prop(interior, "Rug", SpriteFactory.Rug(), new Vector2(3.95f, 3.3f), new Vector2(15.1f, 12f), -97);
            Prop(interior, "CoffeeTable", SpriteFactory.CoffeeTable(), new Vector2(7.2f, 3.5f), new Vector2(12.3f, 6.6f), 3, GlassLayer);
            Sprite sofa = SpriteFactory.GangSofa();
            Prop(interior, "Sofa_Bottom", sofa, new Vector2(4.4f, 0.6f), new Vector2(14.8f, 2.6f), 3, GlassLayer);
            Prop(interior, "Sofa_Left", sofa, new Vector2(0.5f, 11.8f), new Vector2(3f, 18.5f), 3, GlassLayer, true, -90f);
            Prop(interior, "Bar", SpriteFactory.BarCounter(), new Vector2(16.4f, 12f), new Vector2(18.1f, 18.3f), 3, GlassLayer);

            // Кабинет: стол главаря, кресло, шкафы по стенам
            Prop(interior, "BossDesk", SpriteFactory.BossDesk(), new Vector2(28f, 3.1f), new Vector2(34.2f, 5.5f), 3, GlassLayer);
            Prop(interior, "BossChair", SpriteFactory.OfficeChair(), new Vector2(29.7f, 1.1f), new Vector2(31.9f, 3f), 2);
            Sprite shelf = SpriteFactory.Shelf();
            Block(walls, "Shelf_TopRight", new Vector2(36.9f, 10.2f), new Vector2(45.3f, 10.95f), shelf);
            Block(walls, "Shelf_Left", new Vector2(18.9f, 0.67f), new Vector2(19.9f, 8.3f), shelf);
            Block(walls, "Shelf_BottomLeft", new Vector2(21.8f, 0.5f), new Vector2(28.6f, 1.25f), shelf);
            Block(walls, "Shelf_BottomRight", new Vector2(35.4f, 0.5f), new Vector2(45.2f, 1.2f), shelf);

            // Свет второго этажа
            Transform lights = new GameObject("Lights").transform;
            lights.SetParent(parent);
            Color warm = new Color(1f, 0.7f, 0.4f);
            Color magenta = new Color(1f, 0.25f, 0.75f);
            Color red = new Color(1f, 0.2f, 0.25f);
            Lamp(lights, "Stairs", 5f, 23.4f, warm, 5f, 0.7f);
            // Лампы над постами в зале (на схеме — конусы света)
            Lamp(lights, "Hall_Lamp_1", 18.5f, 23f, warm, 7f, 0.8f);
            Lamp(lights, "Hall_Lamp_2", 30.4f, 23f, warm, 7f, 0.8f);
            Lamp(lights, "Hall_Right", 42f, 23.5f, red, 7f, 0.5f);
            Pulsing(Lamp(lights, "Lounge", 9f, 9f, magenta, 12f, 0.6f), 0.7f, 0.4f, 0.7f);
            Pulsing(Lamp(lights, "Corridor", 32f, 15.7f, red, 10f, 0.6f), 1.2f, 0.35f, 0.7f);
            Lamp(lights, "Office", 31f, 6f, warm, 9f, 0.8f);
            Lamp(lights, "Office_Monitor", 31f, 4.4f, new Color(0.3f, 1f, 0.6f), 2.5f, 0.8f);
        }

        private static void BuildFloor2Cameras(Transform parent, QuickhackData cameraOff, GameObject sparksFx, Transform enemies)
        {
            Transform root = new GameObject("Cameras").transform;
            root.SetParent(parent);
            Sprite head = SpriteFactory.SecurityCamera();
            Sprite mount = SpriteFactory.CameraMount();
            // Две камеры по углам коридора над кабинетом
            SecurityCam(root, "Camera_Corridor_W", new Vector2(19.4f, 19.2f), -25f, 30f, 10f, head, mount, cameraOff, sparksFx, enemies);
            SecurityCam(root, "Camera_Corridor_E", new Vector2(45.1f, 19.2f), 205f, 30f, 10f, head, mount, cameraOff, sparksFx, enemies);
        }

        // Охрана второго этажа: 14 бандитов, из них трое патрулируют. Цель среди них не считается.
        // У лестницы никого нет — герой появляется в пустой клетке и сам решает, куда идти;
        // ближайшие к выходу из клетки стоят спиной или далеко
        private static Health[] SpawnFloor2Bandits(GameObject prefab, Transform parent)
        {
            var bandits = new System.Collections.Generic.List<Health>();

            void Stand(Vector2 position, float facing)
            {
                bandits.Add(SpawnEnemy(prefab, parent, ToWorld(position), facing, Vector2.zero, 0f).GetComponent<Health>());
            }

            void Loop(Vector2 position, float facing, params Vector2[] perimeter)
            {
                for (int i = 0; i < perimeter.Length; i++)
                    perimeter[i] *= S;
                bandits.Add(SpawnPerimeter(prefab, parent, ToWorld(position), facing, PatrolSpeed, perimeter).GetComponent<Health>());
            }

            // Зал: один спиной к лестнице, патруль вдоль зала, один под лампой, двое у дальней стены смотрят вдоль зала
            Stand(new Vector2(16.5f, 25.5f), 0f);
            Loop(new Vector2(22f, 21.4f), 0f, new Vector2(18f, 0f), new Vector2(18f, 4.1f), new Vector2(0f, 4.1f));
            Stand(new Vector2(30.4f, 23.4f), 90f);
            Stand(new Vector2(44.6f, 25.6f), 180f);
            Stand(new Vector2(44.3f, 21.4f), 180f);

            // Гостиная: один у дивана, один у столика, патруль по кругу вокруг ковра
            Stand(new Vector2(7.5f, 13f), -45f);
            Stand(new Vector2(16f, 6f), 135f);
            Loop(new Vector2(5f, 9f), 0f, new Vector2(10f, 0f), new Vector2(10f, 7f), new Vector2(0f, 7f));

            // Коридор над кабинетом: патруль по периметру и часовой у двери кабинета
            Loop(new Vector2(21f, 13.2f), 0f, new Vector2(22.5f, 0f), new Vector2(22.5f, 5f), new Vector2(0f, 5f));
            Stand(new Vector2(33.5f, 15.5f), -90f);

            // Кабинет: четверо телохранителей главаря — у обеих дверей и по сторонам стола
            Stand(new Vector2(43.5f, 9f), 180f);
            Stand(new Vector2(24f, 9.5f), -45f);
            Stand(new Vector2(21.2f, 2.4f), 45f);
            Stand(new Vector2(39f, 4f), 135f);

            return bandits.ToArray();
        }

        // ---------------------------------------------------------------- Имплант

        // «Руки гориллы»: ПКМ — рывок с ударом, зажать ПКМ — блок на 3 попадания. Руки в блоке — спрайт перед корпусом
        private static void AddGorillaArms(GameObject hero, CameraFollow follow, CommonPrefabs common)
        {
            Transform body = hero.transform.Find("Body");
            SpriteRenderer guard = CreateSprite("GorillaGuard", body, SpriteFactory.GorillaGuard(), new Vector2(0.38f, 0f), 11);

            GorillaArms arms = hero.AddComponent<GorillaArms>();
            Configure(arms, so =>
            {
                P(so, "_player").objectReferenceValue = hero.GetComponent<PlayerController>();
                P(so, "_health").objectReferenceValue = hero.GetComponent<Health>();
                P(so, "_rb").objectReferenceValue = hero.GetComponent<Rigidbody2D>();
                P(so, "_body").objectReferenceValue = body;
                P(so, "_bodyRenderer").objectReferenceValue = body.GetComponent<SpriteRenderer>();
                P(so, "_cameraFollow").objectReferenceValue = follow;
                P(so, "_guard").objectReferenceValue = guard;
                P(so, "_pixel").objectReferenceValue = SpriteFactory.White();
                P(so, "_material").objectReferenceValue = UnlitMaterial;
                P(so, "_enemyMask").intValue = 1 << 9;
                P(so, "_wallMask").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << DoorLayer);
                P(so, "_bloodFxPrefab").objectReferenceValue = common.Blood;
                P(so, "_bloodPoolPrefab").objectReferenceValue = common.BloodPool;
                P(so, "_blockFxPrefab").objectReferenceValue = common.Sparks;
            });
        }

        // ---------------------------------------------------------------- Подсказки «куда идти»

        // Крупная надпись по центру экрана — под затемнением, чтобы на смене этажа не висела поверх чёрного
        internal static Banner BuildBanner(Transform fader, TMP_FontAsset mono)
        {
            RectTransform canvas = (RectTransform)fader.parent;
            Color cyan = new Color(0.3f, 1f, 1f);
            Image box = Img("Banner", canvas, new Color(0f, 0f, 0f, 0.75f));
            box.transform.SetSiblingIndex(fader.GetSiblingIndex());
            Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1100f, 150f));
            CanvasGroup group = Group(box.gameObject, 0f);
            Image edgeTop = Img("EdgeTop", box.rectTransform, cyan);
            Anchor(edgeTop.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1100f, 4f));
            Image edgeBottom = Img("EdgeBottom", box.rectTransform, cyan);
            Anchor(edgeBottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1100f, 4f));
            TextMeshProUGUI title = Text("Title", box.rectTransform, mono, string.Empty, 56f, Color.white, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 28f), new Vector2(1060f, 66f));
            TextMeshProUGUI subtitle = Text("Subtitle", box.rectTransform, mono, string.Empty, 36f, cyan, TextAlignmentOptions.Center);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -34f), new Vector2(1060f, 50f));

            Banner banner = box.gameObject.AddComponent<Banner>();
            Configure(banner, so =>
            {
                P(so, "_group").objectReferenceValue = group;
                P(so, "_box").objectReferenceValue = box.rectTransform;
                P(so, "_title").objectReferenceValue = title;
                P(so, "_subtitle").objectReferenceValue = subtitle;
            });
            return banner;
        }

        // Стрелка у героя к следующей цели и маркер над самой целью
        internal static WaypointArrow BuildWaypoint(Transform hero)
        {
            var root = new GameObject("Waypoint");
            Sprite arrowSprite = SpriteFactory.WaypointArrow();
            SpriteRenderer arrow = CreateSprite("Arrow", root.transform, arrowSprite, Vector2.zero, 70, false);
            arrow.transform.localScale = Vector3.one * 1.3f;
            SpriteRenderer beacon = CreateSprite("Beacon", root.transform, arrowSprite, Vector2.zero, 70, false);
            beacon.transform.localScale = Vector3.one * 1.8f;

            WaypointArrow waypoint = root.AddComponent<WaypointArrow>();
            Configure(waypoint, so =>
            {
                P(so, "_follow").objectReferenceValue = hero;
                P(so, "_arrow").objectReferenceValue = arrow;
                P(so, "_beacon").objectReferenceValue = beacon;
            });
            return waypoint;
        }

        // Главарь сидит в кресле за столом лицом к двери. Не враг: не стреляет и пулей не убивается
        // (коллайдер на слое Glass — мешает пройти и ловит пули, как мебель). Умирает в катсцене
        private static Transform BuildTarget(Transform parent, out SpriteRenderer body)
        {
            var root = new GameObject("Target") { layer = GlassLayer };
            root.transform.SetParent(parent);
            root.transform.position = V(30.8f, 2.05f);
            root.AddComponent<CircleCollider2D>().radius = 0.36f;
            body = CreateSprite("Body", root.transform, SpriteFactory.TargetSitting(), Vector2.zero, 10, true, 90f);
            return root.transform;
        }

        // Прямоугольник схемы -> мир (для зон в сценарии)
        internal static Rect WorldRect(Vector2 min, Vector2 max)
        {
            Vector2 a = ToWorld(min);
            Vector2 b = ToWorld(max);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
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
            // Свечение экрана проектора и аркадного автомата
            Lamp(lights, "ProjectorScreen", 28.3f, 4.2f, cyan, 4f, 0.7f);
            Pulsing(Lamp(lights, "Arcade", 43.2f, 24.1f, magenta, 3.5f, 0.8f), 2.5f, 0.5f, 0.9f);

            // Фонари во дворе у входов
            Lamp(lights, "Street_E1", 5.5f, -2.8f, cyan, 5f, 0.6f);
            Lamp(lights, "Street_E2", 48f, 17.5f, cyan, 5f, 0.6f);
            Lamp(lights, "Street_NW", -2f, 29f, magenta, 6f, 0.4f);
            Lamp(lights, "Street_SE", 48f, -2.5f, magenta, 6f, 0.4f);
        }

        // ---------------------------------------------------------------- Помощники

        // Помощники ниже принимают координаты схемы и сами переводят их в мир
        internal static Light2D Lamp(Transform parent, string name, float x, float y, Color color, float radius, float intensity)
        {
            return AddLight(parent, name, V(x, y), color, radius * S, intensity);
        }

        internal static void Wall(Transform parent, string name, float x1, float y1, float x2, float y2)
        {
            CreateWall(parent, name, V(x1, y1), V(x2, y2), s_wall, WallLayer, 5);
        }

        // Глухой блок (лестница, шкафчики): плитка по всей площади и коллайдер. По умолчанию — плитка стены
        internal static void Block(Transform parent, string name, Vector2 min, Vector2 max, Sprite sprite = null)
        {
            min = ToWorld(min);
            max = ToWorld(max);
            SpriteRenderer sr = CreateSprite(name, parent, sprite != null ? sprite : s_wall, (min + max) * 0.5f, 5);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = max - min;
            sr.gameObject.layer = WallLayer;
            sr.gameObject.AddComponent<BoxCollider2D>().size = sr.size;
        }

        // Предмет интерьера: спрайт растягивается на прямоугольник схемы. layer < 0 — без коллайдера (декор)
        // angle — поворот спрайта (±90 — лёжа на боку: ширина спрайта идёт по высоте прямоугольника)
        internal static SpriteRenderer Prop(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max, int order,
            int layer = -1, bool lit = true, float angle = 0f)
        {
            min = ToWorld(min);
            max = ToWorld(max);
            SpriteRenderer sr = CreateSprite(name, parent, sprite, (min + max) * 0.5f, order, lit, angle);
            Vector2 size = sprite.bounds.size;
            Vector2 rect = max - min;
            if (Mathf.Abs(Mathf.DeltaAngle(angle, 90f)) < 1f || Mathf.Abs(Mathf.DeltaAngle(angle, -90f)) < 1f)
            {
                rect = new Vector2(rect.y, rect.x);
            }
            sr.transform.localScale = new Vector3(rect.x / size.x, rect.y / size.y, 1f);
            if (layer >= 0)
            {
                sr.gameObject.layer = layer;
                sr.gameObject.AddComponent<BoxCollider2D>();
            }

            return sr;
        }

        // Невидимое препятствие (часть предмета, о которую можно споткнуться)
        internal static void Solid(Transform parent, string name, Vector2 min, Vector2 max, int layer)
        {
            min = ToWorld(min);
            max = ToWorld(max);
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent);
            go.transform.position = (min + max) * 0.5f;
            go.AddComponent<BoxCollider2D>().size = max - min;
        }

        internal static void Floor(Transform parent, string name, Sprite sprite, Vector2 min, Vector2 max, int order)
        {
            SpriteRenderer sr = CreateSprite(name, parent, sprite, (min + max) * 0.5f, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = max - min;
        }

        internal static Light2D Pulsing(Light2D light, float speed, float min, float max)
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
