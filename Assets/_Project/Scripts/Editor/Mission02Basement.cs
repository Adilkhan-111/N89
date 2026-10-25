using N89.Combat;
using N89.Enemies;
using N89.Hacking;
using N89.Missions;
using N89.Player;
using N89.UI;
using N89.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static N89.EditorTools.Mission01SceneBuilder;
using static N89.EditorTools.TutorialSceneBuilder;
using Image = UnityEngine.UI.Image;

namespace N89.EditorTools
{
    // Подвал миссии 2 — арена Босса по схеме от команды. Своя схема: 1 юнит = 20 px картинки
    // (крупнее, чем этажи: арене нужен простор), в мир — тем же S = 0.6; коробка стоит далеко внизу мира (Origin),
    // чтобы её не было видно с первого этажа. Левый нижний угол — (0, 0), всё здание 56.15 x 31.3:
    //  слева зал 45.55 x 31.3 — 5 несущих колонн, 8 бетонных укрытий, Босс на месте оранжевого ромба;
    //  справа сверху вниз — комната со стеклом в зал и дверью в него, лестничная (сюда герой спускается),
    //  два туалета с дверями в зал.
    // Сначала в зале темно — свет дают три лампы, которые включает катсцена BossFight.
    internal static class Mission02Basement
    {
        public static readonly Vector2 Origin = new Vector2(0f, -90f);

        private const float W = 56.15f;
        private const float H = 31.3f;
        private const float ArenaW = 45.55f;

        internal struct Refs
        {
            public Transform Spawn;
            public float SpawnFacing;
            public Rect ReturnZone;
            public BossAI Boss;
            public SpriteRenderer BossBody;
            public Light2D[] Lamps;
            public SpriteRenderer[] Fixtures;
            public Pillar[] Pillars;
            public Rect ArenaEntry;
            public Transform ArenaCenter;
            public Vector2[] DropPoints;
            public GasCylinder Cylinder;
            public GameObject DustFx;
            public CommonPrefabs Common;
        }

        // Вызывать при SetLayout(Origin, ...): помощники Mission01SceneBuilder считают координаты от него
        public static Refs Build(CommonPrefabs common, GameObject glassFx, Material particleMat)
        {
            var refs = new Refs { Common = common };
            Transform root = new GameObject("Basement").transform;
            GameObject dust = BurstPrefab("FX_Dust", new Color(0.6f, 0.58f, 0.55f, 0.8f), new Color(0.35f, 0.34f, 0.33f, 0.6f), 14, 0.3f, 1.6f, 0.6f, 1.4f, 0.12f, 0.3f, particleMat);
            refs.DustFx = dust;

            BuildRooms(root, glassFx);
            BuildRoomLights(root);
            refs.Pillars = BuildPillars(root, dust);
            BuildCover(root);
            BuildDecor(root);
            BuildLamps(root, out refs.Lamps, out refs.Fixtures);

            Transform points = new GameObject("Points").transform;
            points.SetParent(root);
            refs.Spawn = Point(points, "BasementSpawn", new Vector2(49f, 19f));
            refs.SpawnFacing = 45f;
            // Лестница наверх — обратно в холл после боя
            refs.ReturnZone = WorldRect(new Vector2(46.9f, 11.15f), new Vector2(51.15f, 17.4f));
            refs.ArenaCenter = Point(points, "ArenaCenter", new Vector2(22.8f, 15.6f));
            refs.ArenaEntry = WorldRect(new Vector2(41.8f, 20.6f), new Vector2(45f, 24.8f));
            refs.DropPoints = new[]
            {
                V(15.5f, 22f), V(30f, 22.5f), V(15.5f, 10.5f), V(29.5f, 11f), V(3.2f, 12f), V(41.5f, 17f),
                V(23f, 23.3f), V(17f, 3.2f), V(41.5f, 4.5f), V(2.6f, 26f), V(42f, 28.5f), V(28.5f, 15.5f),
            };

            refs.Cylinder = CylinderPrefab(common);
            refs.Boss = BuildBoss(root, common, out refs.BossBody);
            return refs;
        }

        // ---------------------------------------------------------------- Помещения

        private static void BuildRooms(Transform parent, GameObject glassFx)
        {
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(parent);
            float t = WallThickness * 0.5f / S;

            Floor(parent, "Floor", SpriteFactory.ConcreteFloor(), V(0f, 0f), V(W, H), -100);
            Floor(parent, "Floor_Toilets", SpriteFactory.BathroomFloor(), V(ArenaW, 0f), V(W, 9.4f), -99);

            Wall(walls, "Outer_S", -t, 0f, W + t, 0f);
            Wall(walls, "Outer_N", -t, H, W + t, H);
            Wall(walls, "Outer_W", 0f, -t, 0f, H + t);
            Wall(walls, "Outer_E", W, -t, W, H + t);

            // Стена зала: двери из двух туалетов и из комнаты со стеклом, само стекло
            Wall(walls, "Arena_E_1", ArenaW, 0f, ArenaW, 1.5f);
            Wall(walls, "Arena_E_2", ArenaW, 3.7f, ArenaW, 6f);
            Wall(walls, "Arena_E_3", ArenaW, 8.2f, ArenaW, 21.6f);
            Wall(walls, "Arena_E_4", ArenaW, 23.8f, ArenaW, 25.4f);
            Wall(walls, "Arena_E_5", ArenaW, 29.8f, ArenaW, H);
            Mission02SceneBuilder.WindowAt(walls, "Window_Arena", false, 25.4f, 29.8f, ArenaW,
                SpriteFactory.WindowPane(), SpriteFactory.WindowBroken(), SpriteFactory.GlassShards(), glassFx);

            // Правый столбец: комната со стеклом / лестничная (дверь у правой стены), лестничная / туалеты
            Wall(walls, "H_Top_W", ArenaW, 20.9f, 53.4f, 20.9f);
            Wall(walls, "H_Top_E", 55.6f, 20.9f, W, 20.9f);
            Wall(walls, "H_Stairs", ArenaW, 9.4f, W, 9.4f);
            Wall(walls, "H_Toilets", ArenaW, 5.15f, W, 5.15f);

            Transform doors = new GameObject("Doors").transform;
            doors.SetParent(parent);
            AutoDoorAt(doors, "Door_Stairs_Top", true, 53.4f, 55.6f, 20.9f);
            AutoDoorAt(doors, "Door_Top_Arena", false, 21.6f, 23.8f, ArenaW);
            AutoDoorAt(doors, "Door_Toilets1_Arena", false, 6f, 8.2f, ArenaW);
            AutoDoorAt(doors, "Door_Toilets2_Arena", false, 1.5f, 3.7f, ArenaW);

            // Лестница сверху, с первого этажа: пролёт и глухой блок лестничной клетки
            Prop(parent, "Stairs", SpriteFactory.Stairs(), new Vector2(46.9f, 11.15f), new Vector2(51.15f, 17.4f), -98);
            Block(walls, "Stairwell", new Vector2(51.75f, 10.55f), new Vector2(W, 17.55f));

            // Комната со стеклом: пульт у дальней стены
            Prop(parent, "Console", SpriteFactory.Terminal(), new Vector2(52f, 28.4f), new Vector2(55.6f, 30.8f), 3, GlassLayer);

            // Туалеты: пять унитазов вдоль стены, раковины в соседнем
            Sprite toilet = SpriteFactory.Toilet();
            for (int i = 0; i < 5; i++)
            {
                float x = 46.7f + i * 1.75f;
                Prop(parent, "Toilet", toilet, new Vector2(x, 3.4f), new Vector2(x + 1.3f, 4.85f), 3, GlassLayer, true, 180f);
            }
            Sprite sink = SpriteFactory.SinkMirror();
            foreach (float x in new[] { 48f, 51f, 54f })
            {
                Prop(parent, "Sink", sink, new Vector2(x, 8f), new Vector2(x + 1.8f, 8.9f), 3, GlassLayer);
            }
        }

        // Свет в боковых комнатах — неяркий и с малым радиусом, чтобы не просвечивал тёмный зал
        private static void BuildRoomLights(Transform parent)
        {
            Transform lights = new GameObject("RoomLights").transform;
            lights.SetParent(parent);
            Lamp(lights, "Stairs", 49f, 15f, new Color(1f, 0.7f, 0.45f), 6f, 0.7f);
            Pulsing(Lamp(lights, "Emergency", 51.5f, 26.5f, new Color(1f, 0.15f, 0.15f), 6.5f, 0.8f), 1.5f, 0.4f, 0.9f);
            Lamp(lights, "Console", 53.8f, 29.5f, new Color(0.3f, 1f, 0.6f), 2.5f, 0.8f);
            Pulsing(Lamp(lights, "Toilets1", 51f, 7.2f, new Color(0.6f, 0.95f, 1f), 4.5f, 0.6f), 13f, 0.2f, 0.7f);
            Lamp(lights, "Toilets2", 51f, 2.4f, new Color(0.6f, 0.95f, 1f), 4.5f, 0.6f);
        }

        // ---------------------------------------------------------------- Зал

        // Пять несущих колонн (квадрат с кругом на схеме). Их рушат только ракеты Босса
        private static Pillar[] BuildPillars(Transform parent, GameObject dust)
        {
            Transform root = new GameObject("Pillars").transform;
            root.SetParent(parent);
            Sprite sprite = SpriteFactory.Pillar();
            Sprite broken = SpriteFactory.PillarBroken();
            Vector2[] centers = { new Vector2(7.9f, 26.85f), new Vector2(35.95f, 26.95f), new Vector2(23.1f, 16.6f), new Vector2(9.1f, 6.35f), new Vector2(36.15f, 6.1f) };
            var pillars = new Pillar[centers.Length];
            for (int i = 0; i < centers.Length; i++)
            {
                var go = new GameObject($"Pillar_{i}") { layer = WallLayer };
                go.transform.SetParent(root);
                go.transform.position = V(centers[i].x, centers[i].y);
                BoxCollider2D col = go.AddComponent<BoxCollider2D>();
                col.size = Vector2.one * 4.6f * S;
                SpriteRenderer sr = CreateSprite("Sprite", go.transform, sprite, Vector2.zero, 4);
                sr.transform.localScale = Vector3.one * (5.4f * S / sprite.bounds.size.x);

                Pillar pillar = go.AddComponent<Pillar>();
                Configure(pillar, so =>
                {
                    P(so, "_collider").objectReferenceValue = col;
                    P(so, "_renderer").objectReferenceValue = sr;
                    P(so, "_brokenSprite").objectReferenceValue = broken;
                    P(so, "_breakFxPrefab").objectReferenceValue = dust;
                });
                pillars[i] = pillar;
            }
            return pillars;
        }

        // Бетонные укрытия — чёрные полосы на схеме: центр, длина, ширина, угол. Закрывают обзор и ловят пули и ракеты
        private static void BuildCover(Transform parent)
        {
            Transform root = new GameObject("Cover").transform;
            root.SetParent(parent);
            Sprite barrier = SpriteFactory.Barrier();
            void Cover(float x, float y, float length, float width, float angle) =>
                Mission02SceneBuilder.RotatedProp(root, "Barrier", barrier, new Vector2(x, y), length, width, angle, WallLayer);

            Cover(17f, 27.65f, 5.5f, 1.7f, 10f);
            Cover(23.2f, 27.55f, 6.85f, 1.75f, 0f);
            Cover(5.9f, 18.9f, 4.75f, 1.7f, 100f);
            Cover(6.7f, 14.2f, 5.25f, 1.9f, 90f);
            // Правые укрытия чуть короче, чем на схеме: между ними и колоннами должен пролезать Босс
            Cover(34.6f, 21.6f, 3.8f, 1.6f, 87f);
            Cover(34.75f, 13.9f, 4.6f, 1.8f, 93f);
            Cover(22.65f, 6.05f, 5.7f, 1.8f, -29f);
            Cover(28.3f, 4.35f, 6.15f, 1.6f, 0f);
        }

        // Грязь подвала: пятна масла, мусор — без коллайдеров
        private static void BuildDecor(Transform parent)
        {
            Transform root = new GameObject("Decor").transform;
            root.SetParent(parent);
            Sprite stain = SpriteFactory.OilStain();
            foreach (Vector2 p in new[] { new Vector2(14f, 20f), new Vector2(30f, 9f), new Vector2(40f, 24f), new Vector2(4f, 4f), new Vector2(27f, 26f) })
            {
                Decor(root, "Stain", stain, p, 4f);
            }
            Decor(root, "Trash", SpriteFactory.TrashPile(), new Vector2(43f, 1.6f), 2.5f);
            Decor(root, "Trash", SpriteFactory.TrashPile(), new Vector2(2f, 29.5f), 2.5f);
            Decor(root, "Clutter", SpriteFactory.Clutter(), new Vector2(50f, 24f), 1.4f);
        }

        // Три лампы над залом — в начале погашены (их включает катсцена)
        private static void BuildLamps(Transform parent, out Light2D[] lamps, out SpriteRenderer[] fixtures)
        {
            Transform root = new GameObject("ArenaLamps").transform;
            root.SetParent(parent);
            Sprite sprite = SpriteFactory.CeilingLamp();
            Vector2[] at = { new Vector2(8.5f, 16.5f), new Vector2(23f, 22.5f), new Vector2(37.5f, 16.5f) };
            lamps = new Light2D[at.Length];
            fixtures = new SpriteRenderer[at.Length];
            for (int i = 0; i < at.Length; i++)
            {
                Light2D light = AddLight(root, $"Lamp_{i}", V(at[i].x, at[i].y), new Color(1f, 0.9f, 0.75f), 26f * S, 0f);
                light.pointLightInnerRadius = 4f * S;
                lamps[i] = light;
                // Освещаемый спрайт: пока лампа погашена, в тёмном зале его не видно
                SpriteRenderer fixture = CreateSprite($"Fixture_{i}", root, sprite, V(at[i].x, at[i].y), 55);
                fixture.color = new Color(0.15f, 0.15f, 0.18f);
                fixtures[i] = fixture;
            }
        }

        // ---------------------------------------------------------------- Босс

        private static BossAI BuildBoss(Transform parent, CommonPrefabs common, out SpriteRenderer body)
        {
            var root = new GameObject("Boss") { layer = EnemyLayer };
            root.transform.SetParent(parent);
            root.transform.position = V(13.4f, 16.15f);
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            CircleCollider2D col = root.AddComponent<CircleCollider2D>();
            // Уже, чем спрайт: иначе не пролезает между укрытиями и колоннами
            col.radius = 0.7f;

            // Огромный: спрайт 2 x 2 юнита, да ещё увеличен — в 2.5 раза выше охранника
            body = CreateSprite("Body", root.transform, SpriteFactory.Boss(), Vector2.zero, 10);
            body.gameObject.layer = EnemyLayer;
            body.transform.localScale = Vector3.one * 1.25f;
            Transform gun = new GameObject("MinigunMuzzle").transform;
            gun.SetParent(body.transform, false);
            gun.localPosition = new Vector2(0.95f, -0.62f);
            Transform launcher = new GameObject("Launcher").transform;
            launcher.SetParent(body.transform, false);
            launcher.localPosition = new Vector2(0.85f, 0.6f);
            Light2D glow = AddLight(gun, "Glow", gun.position, new Color(1f, 0.45f, 0.15f), 1.8f, 0f);

            Health health = root.AddComponent<Health>();
            SpriteRenderer bodyRenderer = body;
            Configure(health, so =>
            {
                // 400: пуля снимает 1, клинок 8, обрушение потолка 100 (1/4), газовый баллон 50 (1/8)
                P(so, "_maxHealth").intValue = 400;
                P(so, "_destroyOnDeath").boolValue = false;
                P(so, "_flashRenderer").objectReferenceValue = bodyRenderer;
            });

            Weapon minigun = root.AddComponent<Weapon>();
            Configure(minigun, so =>
            {
                P(so, "_bulletPrefab").objectReferenceValue = common.EnemyBullet;
                P(so, "_muzzle").objectReferenceValue = gun;
                P(so, "_cooldown").floatValue = 0.075f;
                P(so, "_spreadDegrees").floatValue = 7f;
                P(so, "_hitMask").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << DoorLayer) | (1 << PlayerLayer);
                P(so, "_muzzleFxPrefab").objectReferenceValue = common.Muzzle;
            });

            // Взлом имплантов ног — Босс стоит на месте, но стреляет
            QuickhackData legs = CreateQuickhack("BossLegs", "ОТКАЗ НОГ", 8, 1f, QuickhackEffect.Immobilize);
            HackableDevice hack = root.AddComponent<HackableDevice>();
            Configure(hack, so =>
            {
                P(so, "_deviceName").stringValue = "БОСС // ИМПЛАНТЫ НОГ";
                SetArray(so, "_quickhacks", legs);
                P(so, "_collider").objectReferenceValue = col;
                SetArray(so, "_highlightRenderers", bodyRenderer);
                P(so, "_shortCircuitFxPrefab").objectReferenceValue = common.Electric;
                P(so, "_glitchMaterial").objectReferenceValue = UnlitMaterial;
            });

            Rocket rocket = RocketPrefab(common);
            BossAI boss = root.AddComponent<BossAI>();
            Configure(boss, so =>
            {
                P(so, "_health").objectReferenceValue = health;
                P(so, "_rb").objectReferenceValue = rb;
                P(so, "_body").objectReferenceValue = bodyRenderer.transform;
                P(so, "_radius").floatValue = col.radius;
                // Идёт к герою всегда, кроме замершего прицела ракеты: его надо заманивать под обвал и к баллонам
                P(so, "_moveSpeed").floatValue = 2f;
                P(so, "_fireMoveSpeed").floatValue = 1f;
                P(so, "_stopDistance").floatValue = 1.6f;
                P(so, "_moveBlockers").intValue = (1 << WallLayer) | (1 << GlassLayer);
                P(so, "_minigun").objectReferenceValue = minigun;
                P(so, "_minigunGlow").objectReferenceValue = glow;
                P(so, "_rocketPrefab").objectReferenceValue = rocket;
                P(so, "_launcher").objectReferenceValue = launcher;
                P(so, "_rocketBlockers").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << DoorLayer);
                P(so, "_pixel").objectReferenceValue = SpriteFactory.White();
                P(so, "_material").objectReferenceValue = UnlitMaterial;
                P(so, "_sparkFxPrefab").objectReferenceValue = common.Electric;
            });
            return boss;
        }

        // Ракета: светится, за ней дымный хвост
        private static Rocket RocketPrefab(CommonPrefabs common)
        {
            var root = new GameObject("BossRocket");
            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.Rocket();
            sr.sharedMaterial = UnlitMaterial;
            sr.sortingOrder = 21;
            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.35f;
            trail.widthMultiplier = 0.16f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.sharedMaterial = UnlitMaterial;
            trail.sortingOrder = 20;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0f), new GradientColorKey(new Color(0.4f, 0.4f, 0.4f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            AddLight(root.transform, "Light", Vector2.zero, new Color(1f, 0.5f, 0.2f), 1.8f, 1.1f);

            Rocket rocket = root.AddComponent<Rocket>();
            Configure(rocket, so =>
            {
                P(so, "_hitMask").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << DoorLayer) | (1 << PlayerLayer);
                P(so, "_blastMask").intValue = (1 << PlayerLayer) | (1 << GlassLayer);
                P(so, "_explosionFxPrefab").objectReferenceValue = common.Explosion;
                P(so, "_remainsPrefab").objectReferenceValue = common.Remains;
            });
            return SavePrefab(root, "BossRocket").GetComponent<Rocket>();
        }

        // Газовый баллон: падает с потолка, взрывается от пули
        private static GasCylinder CylinderPrefab(CommonPrefabs common)
        {
            var root = new GameObject("GasCylinder") { layer = GlassLayer };
            CircleCollider2D col = root.AddComponent<CircleCollider2D>();
            col.radius = 0.28f;
            SpriteRenderer shadow = CreateSprite("Shadow", root.transform, SpriteFactory.SoftShadow(), Vector2.zero, -60);
            shadow.color = new Color(0f, 0f, 0f, 0.5f);
            SpriteRenderer body = CreateSprite("Body", root.transform, SpriteFactory.GasCylinder(), Vector2.zero, 4);

            Health health = root.AddComponent<Health>();
            Configure(health, so =>
            {
                P(so, "_maxHealth").intValue = 1;
                P(so, "_destroyOnDeath").boolValue = false;
            });

            GasCylinder cylinder = root.AddComponent<GasCylinder>();
            Configure(cylinder, so =>
            {
                P(so, "_health").objectReferenceValue = health;
                P(so, "_collider").objectReferenceValue = col;
                P(so, "_renderer").objectReferenceValue = body;
                P(so, "_shadow").objectReferenceValue = shadow;
                P(so, "_blastMask").intValue = (1 << PlayerLayer) | (1 << EnemyLayer) | (1 << GlassLayer);
                P(so, "_explosionFxPrefab").objectReferenceValue = common.Explosion;
                P(so, "_remainsPrefab").objectReferenceValue = common.Remains;
            });
            return SavePrefab(root, "GasCylinder").GetComponent<GasCylinder>();
        }

        // ---------------------------------------------------------------- Сценарий боя и полоса здоровья

        public static BossFight BuildFight(Refs refs, GameObject hero, CameraFollow follow, HudRefs hud, TMP_FontAsset mono, Light2D globalLight)
        {
            BossHud bossHud = BuildBossHud(hud, mono);

            // Свои цвета в субтитрах: Босс — красный, Рэй — голубой
            Configure(hud.Subtitles, so =>
            {
                SerializedProperty speakers = P(so, "_speakers");
                int start = speakers.arraySize;
                speakers.arraySize = start + 2;
                void Style(int i, string name, Color nameColor, Color textColor)
                {
                    SerializedProperty s = speakers.GetArrayElementAtIndex(i);
                    s.FindPropertyRelative("Name").stringValue = name;
                    s.FindPropertyRelative("NameColor").colorValue = nameColor;
                    s.FindPropertyRelative("TextColor").colorValue = textColor;
                }
                Style(start, "БОСС", new Color(1f, 0.3f, 0.25f), new Color(1f, 0.9f, 0.86f));
                Style(start + 1, "РЭЙ", new Color(0.3f, 1f, 1f), new Color(0.88f, 1f, 1f));
            });

            BossFight fight = new GameObject("BossFight").AddComponent<BossFight>();
            Configure(fight, so =>
            {
                P(so, "_player").objectReferenceValue = hero.GetComponent<PlayerController>();
                P(so, "_hacking").objectReferenceValue = hero.GetComponent<HackingController>();
                P(so, "_cameraFollow").objectReferenceValue = follow;
                P(so, "_objective").objectReferenceValue = hud.Objective;
                P(so, "_subtitles").objectReferenceValue = hud.Subtitles;
                P(so, "_fader").objectReferenceValue = hud.Fader;
                P(so, "_letterboxTop").objectReferenceValue = hud.LetterboxTop;
                P(so, "_letterboxBottom").objectReferenceValue = hud.LetterboxBottom;
                P(so, "_hud").objectReferenceValue = bossHud;

                P(so, "_boss").objectReferenceValue = refs.Boss;
                P(so, "_bossBody").objectReferenceValue = refs.BossBody;
                P(so, "_bossDownSprite").objectReferenceValue = SpriteFactory.BossDown();
                P(so, "_bossDeadSprite").objectReferenceValue = SpriteFactory.BossDead();
                P(so, "_bloodFxPrefab").objectReferenceValue = refs.Common.Blood;
                P(so, "_bloodPoolPrefab").objectReferenceValue = refs.Common.BloodPool;

                P(so, "_globalLight").objectReferenceValue = globalLight;
                SetArray(so, "_lamps", refs.Lamps);
                SetArray(so, "_lampFixtures", refs.Fixtures);
                P(so, "_arenaEntry").rectValue = refs.ArenaEntry;
                P(so, "_arenaCenter").objectReferenceValue = refs.ArenaCenter;

                SetArray(so, "_pillars", refs.Pillars);
                P(so, "_warningSprite").objectReferenceValue = SpriteFactory.CollapseZone();
                SetArray(so, "_rubbleSprites", SpriteFactory.Rubble("rubble_a", 3), SpriteFactory.Rubble("rubble_b", 17));
                P(so, "_dustFxPrefab").objectReferenceValue = refs.DustFx;
                P(so, "_material").objectReferenceValue = UnlitMaterial;
                P(so, "_cylinderPrefab").objectReferenceValue = refs.Cylinder;
                P(so, "_caseSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Interlude02SceneBuilder.CaseSpritePath);
                SerializedProperty drops = P(so, "_dropPoints");
                drops.arraySize = refs.DropPoints.Length;
                for (int i = 0; i < refs.DropPoints.Length; i++) drops.GetArrayElementAtIndex(i).vector2Value = refs.DropPoints[i];
            });
            return fight;
        }

        // Полоса здоровья Босса вверху по центру: имя, красная полоса, белая «тень» урона
        private static BossHud BuildBossHud(HudRefs hud, TMP_FontAsset mono)
        {
            RectTransform canvas = (RectTransform)hud.Fader.transform.parent;
            Vector2 top = new Vector2(0.5f, 1f);
            RectTransform root = MainMenuSceneBuilder.CreateRect("BossHud", canvas);
            root.SetSiblingIndex(hud.Fader.transform.GetSiblingIndex());
            Anchor(root, top, top, new Vector2(0f, -70f), new Vector2(900f, 64f));
            CanvasGroup group = Group(root.gameObject, 0f);

            Color red = new Color(1f, 0.25f, 0.22f);
            TextMeshProUGUI label = Text("Name", root, mono, "БОСС", 26f, red, TextAlignmentOptions.BottomLeft);
            Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(600f, 34f));
            Image back = Img("Back", root, new Color(0.12f, 0.02f, 0.03f, 0.85f));
            Anchor(back.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(900f, 22f));
            Image lag = Img("Lag", back.rectTransform, new Color(1f, 0.95f, 0.9f, 0.9f));
            Image fill = Img("Fill", back.rectTransform, red);
            foreach (Image bar in new[] { lag, fill })
            {
                MainMenuSceneBuilder.Stretch(bar.rectTransform);
                bar.type = Image.Type.Filled;
                bar.fillMethod = Image.FillMethod.Horizontal;
                bar.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            BossHud bossHud = root.gameObject.AddComponent<BossHud>();
            Configure(bossHud, so =>
            {
                P(so, "_group").objectReferenceValue = group;
                P(so, "_name").objectReferenceValue = label;
                P(so, "_fill").objectReferenceValue = fill;
                P(so, "_lag").objectReferenceValue = lag;
            });
            return bossHud;
        }

        private static Transform Point(Transform parent, string name, Vector2 schema)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = V(schema.x, schema.y);
            return go.transform;
        }
    }
}
