using System;
using System.Collections.Generic;
using N89.Combat;
using N89.Enemies;
using N89.Hacking;
using N89.Player;
using N89.Tutorial;
using N89.UI;
using N89.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace N89.EditorTools
{
    // Собирает туториал: шестиугольная кибер-лаборатория, игрок (Номер 89), мишени, дроны,
    // взламываемая дверь, дрон за стеклом, шлюз с турелями и весь HUD.
    // Запуск: N89 > Build Tutorial Scene. Пересоздаёт Tutorial.unity, спрайты и префабы.
    public static class TutorialSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PrefabsDir = Root + "/Prefabs";
        private const string QuickhacksDir = Root + "/ScriptableObjects/Quickhacks";
        private const string SettingsDir = Root + "/Settings";
        private const string ScenePath = Root + "/Scenes/Tutorial.unity";

        private const float LabRadius = 15f;
        internal const float WallThickness = 0.625f;
        private const float DoorLength = 3.2f;
        private const int RamSlots = 15;

        internal const int WallLayer = 6;
        internal const int GlassLayer = 7;
        internal const int PlayerLayer = 8;
        internal const int EnemyLayer = 9;
        // Автоматические двери (AutoDoor): закрывают обзор и ловят пули, но не мешают поиску пути
        internal const int DoorLayer = 10;

        private static readonly Color Cyan = new Color(0.3f, 1f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.8f);
        private static readonly Color Yellow = new Color(1f, 0.85f, 0.3f);
        private static readonly Color BloodLight = new Color(0.15f, 0.45f, 1f);
        private static readonly Color BloodDark = new Color(0.03f, 0.1f, 0.55f);

        private static Material s_lit;
        private static Material s_unlit;
        internal static Material UnlitMaterial => s_unlit;
        private static Sprite s_white;

        [MenuItem("N89/Build Tutorial Scene")]
        public static void Build()
        {
            EnsureLayers();
            Physics2D.gravity = Vector2.zero;
            MainMenuSceneBuilder.EnsureFolder(PrefabsDir);
            MainMenuSceneBuilder.EnsureFolder(QuickhacksDir);
            MainMenuSceneBuilder.EnsureFolder(SettingsDir);

            // Сцену создаём ДО загрузки ассетов: NewScene выгружает из памяти ассеты, на которые ещё
            // ничего не ссылается (например, QuickhackData), и в сцену записались бы пустые ссылки.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadCommonAssets();

            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");
            TMP_FontAsset audiowide = MainMenuSceneBuilder.GetOrCreateFontAsset("Audiowide-Regular");

            // --- Данные взломов и общие префабы ---
            CommonPrefabs common = CreateCommonPrefabs();
            QuickhackData unlock = common.Unlock;
            QuickhackData shortCircuit = common.ShortCircuit;
            GameObject explosion = common.Explosion;
            GameObject electric = common.Electric;
            GameObject blood = common.Blood;
            GameObject muzzle = common.Muzzle;
            GameObject remains = common.Remains;
            GameObject bloodPool = common.BloodPool;
            Bullet turretBullet = common.EnemyBullet;

            // 89 — ребёнок, поэтому хитбокс меньше, чем у охраны. Короткие детские шаги, но он быстрый —
            // кадры ног сменяются реже по пути
            GameObject playerPrefab = PlayerPrefab("Player", SpriteFactory.Player(), new[]
            {
                SpriteFactory.KidLegs("player_legs_idle", 0f), SpriteFactory.KidLegs("player_legs_left", 8f),
                SpriteFactory.KidLegs("player_legs_pass", 1.5f), SpriteFactory.KidLegs("player_legs_right", -8f),
            }, 0.6f, 0.26f, common, 8, SpriteFactory.PlayerDead());
            GameObject dronePrefab = DronePrefab(SpriteFactory.Drone(), explosion, remains);
            GameObject targetPrefab = TargetPrefab(SpriteFactory.Target(), explosion, remains);
            TurretPrefab(SpriteFactory.TurretBase(), SpriteFactory.TurretHead(), turretBullet, muzzle);
            // Охрана — люди: истекают синей кровью. Дроны — техника: взрываются
            GameObject guardPrefab = EnemyPrefab("CorpGuard", "ОХРАНА // КОРПОРАЦИЯ Е", SpriteFactory.Guard(), 1, 0.5f, 0.6f,
                turretBullet, muzzle, blood, bloodPool, shortCircuit, electric, true, SpriteFactory.GuardDead());
            GameObject combatDronePrefab = EnemyPrefab("CombatDrone", "БОЕВОЙ ДРОН", SpriteFactory.CombatDrone(), 2, 0.75f, 0.6f,
                turretBullet, muzzle, explosion, remains, shortCircuit, electric, false);

            // --- Сцена ---
            Camera cam = CreateCamera();
            CreateVolume("TutorialVolume");

            Transform lights = new GameObject("Lights").transform;
            Light2D globalLight = new GameObject("Global Light").AddComponent<Light2D>();
            globalLight.transform.SetParent(lights);
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 0.45f;
            globalLight.color = new Color(0.6f, 0.62f, 0.9f);

            AddLight(lights, "Center", new Vector2(0f, 0f), new Color(0.9f, 0.3f, 0.9f), 11f, 0.8f);
            AddLight(lights, "Range", new Vector2(0f, 9.5f), new Color(0.3f, 0.9f, 1f), 7f, 1f);
            AddLight(lights, "WestChamber", new Vector2(-10.8f, 0f), new Color(0.2f, 1f, 0.8f), 5f, 1.2f);
            AddLight(lights, "EastChamber", new Vector2(10.8f, 0f), new Color(0.4f, 0.5f, 1f), 5f, 1f);
            AddLight(lights, "Capsule", new Vector2(0f, -11.3f), Cyan, 4f, 1.2f);

            Transform lab = new GameObject("Lab").transform;
            BuildLab(lab, out Door hackDoor, out Door exitGate, out HackableDevice exitGateHack, out float gateX, unlock);

            Transform route = new GameObject("EscapeRoute").transform;
            EscapeRefs escape = BuildEscapeRoute(route, lights, gateX, unlock, guardPrefab, combatDronePrefab, mono);

            var alarmLights = new List<Light2D>
            {
                AddLight(lights, "Alarm_East", new Vector2(10.8f, 0f), new Color(1f, 0.1f, 0.15f), 7f, 2f),
                AddLight(lights, "Alarm_Center", new Vector2(0f, 0f), new Color(1f, 0.1f, 0.15f), 12f, 1.2f),
            };
            alarmLights.AddRange(escape.Alarms);
            foreach (Light2D alarm in alarmLights)
            {
                Pulse pulse = alarm.gameObject.AddComponent<Pulse>();
                Configure(pulse, so =>
                {
                    P(so, "_light").objectReferenceValue = alarm;
                    P(so, "_speed").floatValue = 7f;
                    P(so, "_scaleAmount").floatValue = 0f;
                    P(so, "_lightMin").floatValue = 0.2f;
                    P(so, "_lightMax").floatValue = alarm.intensity;
                });
            }

            Transform gameplay = new GameObject("Gameplay").transform;

            CreateSprite("Capsule", gameplay, SpriteFactory.Capsule(), new Vector2(0f, -11.8f), 3);
            CreateSprite("Console", gameplay, SpriteFactory.Console(), new Vector2(-2.6f, -11.4f), 3);
            CreateSprite("Console", gameplay, SpriteFactory.Console(), new Vector2(2.6f, -11.4f), 3);
            CreateProp("Console", gameplay, SpriteFactory.Console(), new Vector2(10.8f, 4.5f));
            CreateProp("Console", gameplay, SpriteFactory.Console(), new Vector2(10.8f, -4.5f));
            CreateProp("Console", gameplay, SpriteFactory.Console(), new Vector2(-10.8f, 4.5f));
            Sprite crate = SpriteFactory.Crate();
            CreateProp("Crate", gameplay, crate, new Vector2(-6.4f, 3.6f));
            CreateProp("Crate", gameplay, crate, new Vector2(-6.4f, 2.6f));
            CreateProp("Crate", gameplay, crate, new Vector2(6.3f, -4f));
            CreateProp("Crate", gameplay, crate, new Vector2(5.4f, -9.2f));

            // Линия огня на стрельбище
            SpriteRenderer firingLine = CreateSprite("FiringLine", gameplay, s_white, new Vector2(0f, 6.6f), -85, false);
            firingLine.transform.localScale = new Vector3(56f, 0.5f, 1f);
            firingLine.color = new Color(0.85f, 0.65f, 0.1f, 0.6f);

            GameObject player = Spawn(playerPrefab, gameplay, new Vector2(0f, -9.6f));
            player.transform.Find("Body").rotation = Quaternion.Euler(0f, 0f, 90f);
            player.transform.Find("Legs").rotation = Quaternion.Euler(0f, 0f, 90f);

            Sprite markerSprite = SpriteFactory.Marker();
            TriggerZone[] markers =
            {
                CreateMarker(gameplay, markerSprite, new Vector2(-4.5f, -6f)),
                CreateMarker(gameplay, markerSprite, new Vector2(4.5f, -6f)),
                CreateMarker(gameplay, markerSprite, new Vector2(0f, -1.5f)),
            };

            Health[] targets =
            {
                Spawn(targetPrefab, gameplay, new Vector2(-3.5f, 10f)).GetComponent<Health>(),
                Spawn(targetPrefab, gameplay, new Vector2(0f, 11.3f)).GetComponent<Health>(),
                Spawn(targetPrefab, gameplay, new Vector2(3.5f, 10f)).GetComponent<Health>(),
            };

            Health[] drones =
            {
                SpawnDrone(dronePrefab, gameplay, new Vector2(-6f, 8f), new Vector2(5f, 0f), 2.5f),
                SpawnDrone(dronePrefab, gameplay, new Vector2(6f, 5.6f), new Vector2(-5f, 0f), 3.2f),
            };

            Health glassDrone = SpawnDrone(dronePrefab, gameplay, new Vector2(-10.8f, -2f), new Vector2(0f, 4f), 1.5f);
            glassDrone.name = "GlassDrone";
            HackableDevice droneHack = glassDrone.gameObject.AddComponent<HackableDevice>();
            Configure(droneHack, so =>
            {
                P(so, "_deviceName").stringValue = "УЧЕБНЫЙ ДРОН";
                SetArray(so, "_quickhacks", shortCircuit);
                P(so, "_collider").objectReferenceValue = glassDrone.GetComponent<Collider2D>();
                SetArray(so, "_highlightRenderers", glassDrone.transform.Find("Body").GetComponent<SpriteRenderer>());
                P(so, "_shortCircuitFxPrefab").objectReferenceValue = electric;
            });

            // --- Камера и игрок ---
            CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
            Configure(follow, so =>
            {
                P(so, "_target").objectReferenceValue = player.transform;
                P(so, "_maxLookAhead").floatValue = 3f;
            });
            PlayerController playerController = player.GetComponent<PlayerController>();
            Configure(playerController, so => P(so, "_cameraFollow").objectReferenceValue = follow);
            cam.transform.position = new Vector3(0f, -9.6f, -10f);

            // --- UI ---
            HudRefs hud = BuildHud(player.GetComponent<HackingController>(), mono, audiowide);

            // --- Катсцена выстрела в спину: трассер и вспышка попадания (включаются из кода) ---
            Transform cutscene = new GameObject("Cutscene").transform;
            SpriteRenderer tracer = CreateSprite("Tracer", cutscene, s_white, Vector2.zero, 25, false);
            tracer.color = new Color(1f, 0.95f, 0.7f);
            tracer.gameObject.SetActive(false);
            SpriteRenderer impactLine = CreateSprite("ImpactLine", cutscene, s_white, Vector2.zero, 26, false);
            impactLine.color = Color.white;
            AddLight(impactLine.transform, "Glow", Vector2.zero, new Color(0.8f, 0.9f, 1f), 3f, 2.5f);
            impactLine.gameObject.SetActive(false);

            // --- Режиссёр туториала ---
            TutorialDirector director = new GameObject("TutorialDirector").AddComponent<TutorialDirector>();
            Configure(director, so =>
            {
                P(so, "_player").objectReferenceValue = playerController;
                P(so, "_hacking").objectReferenceValue = player.GetComponent<HackingController>();
                P(so, "_cameraFollow").objectReferenceValue = follow;
                P(so, "_subtitles").objectReferenceValue = hud.Subtitles;
                P(so, "_objective").objectReferenceValue = hud.Objective;
                P(so, "_hackingHud").objectReferenceValue = hud.HackingHud;
                P(so, "_ammoHud").objectReferenceValue = hud.Ammo;
                P(so, "_reload").objectReferenceValue = player.GetComponent<ActiveReload>();
                P(so, "_fader").objectReferenceValue = hud.Fader;
                P(so, "_endScreen").objectReferenceValue = hud.EndScreen;
                SetArray(so, "_moveMarkers", markers);
                SetArray(so, "_targets", targets);
                SetArray(so, "_drones", drones);
                P(so, "_hackDoor").objectReferenceValue = hackDoor;
                P(so, "_glassDrone").objectReferenceValue = glassDrone;
                P(so, "_exitGate").objectReferenceValue = exitGate;
                P(so, "_exitGateHack").objectReferenceValue = exitGateHack;
                P(so, "_escapeStart").objectReferenceValue = escape.Start;
                P(so, "_firstEscapeDoor").objectReferenceValue = escape.FirstDoor;
                P(so, "_corridorZone").objectReferenceValue = escape.Corridor;
                P(so, "_guardRoomZone").objectReferenceValue = escape.GuardRoom;
                P(so, "_droneCorridorZone").objectReferenceValue = escape.DroneCorridor;
                P(so, "_hallZone").objectReferenceValue = escape.Hall;
                P(so, "_exitZone").objectReferenceValue = escape.Exit;
                P(so, "_assassin").objectReferenceValue = escape.Assassin;
                SetArray(so, "_alarmLights", alarmLights.ToArray());
                P(so, "_globalLight").objectReferenceValue = globalLight;
                P(so, "_letterboxTop").objectReferenceValue = hud.LetterboxTop;
                P(so, "_letterboxBottom").objectReferenceValue = hud.LetterboxBottom;
                P(so, "_tracer").objectReferenceValue = tracer;
                P(so, "_impactLine").objectReferenceValue = impactLine;
                P(so, "_muzzleFxPrefab").objectReferenceValue = muzzle;
                P(so, "_bloodPoolPrefab").objectReferenceValue = bloodPool;
                P(so, "_deadSprite").objectReferenceValue = SpriteFactory.PlayerDead();
            });

            escape.Assassin.gameObject.SetActive(false);
            // Убийца у выхода — сценарный, в счёт и «пар» ранга не входит
            Configure(hud.Score, so => SetArray(so, "_exclude", escape.Assassin.GetComponent<Health>()));

            ValidateQuickhacks();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Туториал собран: {ScenePath}");
        }

        // ---------------------------------------------------------------- Лаборатория

        private static void BuildLab(Transform lab, out Door hackDoor, out Door exitGate, out HackableDevice exitGateHack, out float gateX,
            QuickhackData unlock)
        {
            Sprite wall = SpriteFactory.Wall();
            Sprite glass = SpriteFactory.Glass();
            Sprite door = SpriteFactory.DoorPanel();

            CreateSprite("Floor", lab, SpriteFactory.LabFloor(LabRadius), Vector2.zero, -100);

            // Внешние стены шестиугольника (pointy-top). Восточная грань — со шлюзом посередине.
            float apothem = LabRadius * 0.8660254f;
            for (int k = 0; k < 5; k++)
            {
                Vector2 a = HexVertex(k);
                Vector2 b = HexVertex(k + 1);
                Vector2 normal = ((a + b) * 0.5f).normalized;
                Vector2 shift = normal * (WallThickness * 0.5f);
                Vector2 extend = (b - a).normalized * 0.25f;
                CreateWall(lab, "OuterWall", a + shift - extend, b + shift + extend, wall, WallLayer, 5);
            }

            float eastX = apothem + WallThickness * 0.5f;
            gateX = eastX;
            float half = DoorLength * 0.5f;
            CreateWall(lab, "OuterWall", new Vector2(eastX, -LabRadius * 0.5f - 0.25f), new Vector2(eastX, -half), wall, WallLayer, 5);
            CreateWall(lab, "OuterWall", new Vector2(eastX, half), new Vector2(eastX, LabRadius * 0.5f + 0.25f), wall, WallLayer, 5);

            // Восточная комната (за взламываемой дверью)
            CreateWall(lab, "InnerWall", new Vector2(8f, half), new Vector2(8f, 10.9f), wall, WallLayer, 5);
            CreateWall(lab, "InnerWall", new Vector2(8f, -10.9f), new Vector2(8f, -half), wall, WallLayer, 5);
            hackDoor = CreateDoor(lab, "HackDoor", new Vector2(8f, 0f), door, new Vector2(7.4f, 2.2f));
            MakeHackable(hackDoor, "ДВЕРЬ // ЭЛЕКТРОЗАМОК", unlock);

            // Западная комната за стеклом
            CreateWall(lab, "InnerWall", new Vector2(-8f, 3f), new Vector2(-8f, 10.9f), wall, WallLayer, 5);
            CreateWall(lab, "InnerWall", new Vector2(-8f, -10.9f), new Vector2(-8f, -3f), wall, WallLayer, 5);
            CreateWall(lab, "Glass", new Vector2(-8f, -3f), new Vector2(-8f, 3f), glass, GlassLayer, 6, false);

            // Шлюз наружу — взламывается в конце калибровки, за ним начинается побег
            exitGate = CreateDoor(lab, "ExitGate", new Vector2(eastX, 0f), door, new Vector2(eastX - 0.6f, 2.2f));
            exitGateHack = MakeHackable(exitGate, "ШЛЮЗ // ВНЕШНИЙ КОНТУР", unlock);
        }

        private static HackableDevice MakeHackable(Door door, string deviceName, QuickhackData unlock)
        {
            HackableDevice device = door.gameObject.AddComponent<HackableDevice>();
            Configure(device, so =>
            {
                P(so, "_deviceName").stringValue = deviceName;
                SetArray(so, "_quickhacks", unlock);
                P(so, "_collider").objectReferenceValue = door.GetComponent<Collider2D>();
                SetArray(so, "_highlightRenderers", door.transform.Find("Panel").GetComponent<SpriteRenderer>());
            });
            return device;
        }

        // ---------------------------------------------------------------- Побег (часть 2)

        private struct EscapeRefs
        {
            public Transform Start;
            public Door FirstDoor;
            public TriggerZone Corridor;
            public TriggerZone GuardRoom;
            public TriggerZone DroneCorridor;
            public TriggerZone Hall;
            public TriggerZone Exit;
            public EnemyAI Assassin;
            public List<Light2D> Alarms;
        }

        // Маршрут побега к востоку от шлюза: коридор A → комната охраны B → коридор C (на север) →
        // склад F → коридор D (на восток) → зал E с ярко освещённым выходом. Стены ставятся по осевым линиям.
        private static EscapeRefs BuildEscapeRoute(Transform route, Transform lights, float gateX, QuickhackData unlock,
            GameObject guardPrefab, GameObject dronePrefab, TMP_FontAsset font)
        {
            Sprite wall = SpriteFactory.Wall();
            Sprite door = SpriteFactory.DoorPanel();
            Sprite floor = SpriteFactory.FloorTileSprite();
            Sprite crate = SpriteFactory.Crate();
            const float h = WallThickness * 0.5f;

            void W(float x1, float y1, float x2, float y2) =>
                CreateWall(route, "Wall", new Vector2(x1, y1), new Vector2(x2, y2), wall, WallLayer, 5);

            SpriteRenderer F(string name, Vector2 center, Vector2 size)
            {
                SpriteRenderer sr = CreateSprite(name, route, floor, center, -100);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = size;
                return sr;
            }

            // A: коридор от шлюза до первой двери
            float corridorY = DoorLength * 0.5f + h;
            W(gateX, corridorY, 26f, corridorY);
            W(gateX, -corridorY, 26f, -corridorY);
            F("Floor_A", new Vector2((gateX + 26f) * 0.5f, 0f), new Vector2(26f - gateX, 3.8f));

            // B: комната охраны 14x14 с дверями на западе и севере
            W(26f, -7f - h, 26f, -1.6f);
            W(26f, 1.6f, 26f, 7f + h);
            W(26f - h, -7f, 40f + h, -7f);
            W(40f, -7f - h, 40f, 7f + h);
            W(26f - h, 7f, 34.9f, 7f);
            W(38.1f, 7f, 40f + h, 7f);
            F("Floor_B", new Vector2(33f, 0f), new Vector2(14f, 14f));
            Door doorA = CreateDoor(route, "Door_A", new Vector2(26f, 0f), door, new Vector2(25.4f, 2.2f));
            MakeHackable(doorA, "ДВЕРЬ // СЕКТОР Б", unlock);
            Door doorB = CreateDoor(route, "Door_B", new Vector2(36.5f, 7f), door, new Vector2(38.8f, 6.4f), 0f);
            MakeHackable(doorB, "ДВЕРЬ // СЕРВИСНЫЙ ХОД", unlock);

            // C: короткий коридор на север
            W(34.9f - h, 7f, 34.9f - h, 14f);
            W(38.1f + h, 7f, 38.1f + h, 14f);
            F("Floor_C", new Vector2(36.5f, 10.5f), new Vector2(3.8f, 7f));

            // F: склад 16x12 — вход с юга, выход на восток через запертую дверь
            W(30f - h, 14f, 34.9f, 14f);
            W(38.1f, 14f, 46f + h, 14f);
            W(30f, 14f - h, 30f, 26f + h);
            W(30f - h, 26f, 46f + h, 26f);
            W(46f, 14f - h, 46f, 18.5f);
            W(46f, 21.7f, 46f, 26f + h);
            F("Floor_F", new Vector2(38f, 20f), new Vector2(16f, 12f));
            Door doorC = CreateDoor(route, "Door_C", new Vector2(46f, 20.1f), door, new Vector2(45.4f, 22.3f));
            MakeHackable(doorC, "ДВЕРЬ // ВЫХОД К ПЕРИМЕТРУ", unlock);

            // D: коридор на восток
            W(46f, 18.5f - h, 54f, 18.5f - h);
            W(46f, 21.7f + h, 54f, 21.7f + h);
            F("Floor_D", new Vector2(50f, 20.1f), new Vector2(8f, 3.8f));

            // E: зал 20x20 с выходом на востоке
            W(54f, 10f - h, 54f, 18.5f);
            W(54f, 21.7f, 54f, 30f + h);
            W(54f - h, 10f, 74f + h, 10f);
            W(54f - h, 30f, 74f + h, 30f);
            W(74f, 10f - h, 74f, 17.7f);
            W(74f, 22.5f, 74f, 30f + h);
            F("Floor_E", new Vector2(64f, 20f), new Vector2(20f, 20f));

            // Снаружи — яркий свет: выход видно издалека
            W(74f, 17.7f - h, 78.3f, 17.7f - h);
            W(74f, 22.5f + h, 78.3f, 22.5f + h);
            W(78f, 17.7f - 2f * h, 78f, 22.5f + 2f * h);
            SpriteRenderer outside = F("Floor_Outside", new Vector2(76f, 20.1f), new Vector2(4f, 4.8f));
            outside.sharedMaterial = s_unlit;
            outside.color = new Color(0.9f, 1f, 0.95f);

            var sign = new GameObject("ExitSign").AddComponent<TextMeshPro>();
            sign.transform.SetParent(route);
            sign.transform.position = new Vector2(72.2f, 23.8f);
            sign.font = font;
            sign.text = "ВЫХОД";
            sign.fontSize = 9f;
            sign.alignment = TextAlignmentOptions.Center;
            sign.color = new Color(0.35f, 1f, 0.55f);
            sign.rectTransform.sizeDelta = new Vector2(6f, 2f);
            sign.GetComponent<MeshRenderer>().sortingOrder = 15;

            // Укрытия
            CreateProp("Crate", route, crate, new Vector2(29.5f, 3.2f));
            CreateProp("Crate", route, crate, new Vector2(29.5f, -3.2f));
            CreateProp("Crate", route, crate, new Vector2(34.5f, 0.5f));
            CreateProp("Crate", route, crate, new Vector2(34.5f, -0.5f));
            CreateProp("Console", route, SpriteFactory.Console(), new Vector2(38.5f, -5.6f));
            // Склад: ящики внутри облёта дрона, чтобы он их не задевал
            CreateProp("Crate", route, crate, new Vector2(35.5f, 18f));
            CreateProp("Crate", route, crate, new Vector2(35.5f, 22f));
            CreateProp("Crate", route, crate, new Vector2(40.5f, 20f));
            CreateProp("Crate", route, crate, new Vector2(41.5f, 20f));
            // Зал
            CreateProp("Crate", route, crate, new Vector2(58.5f, 14f));
            CreateProp("Crate", route, crate, new Vector2(58.5f, 26f));
            CreateProp("Crate", route, crate, new Vector2(64f, 16.5f));
            CreateProp("Crate", route, crate, new Vector2(64f, 23.5f));
            CreateProp("Crate", route, crate, new Vector2(69f, 14.5f));
            CreateProp("Crate", route, crate, new Vector2(69f, 25.5f));

            // Охрана корпорации Е
            // B: патруль вдоль восточной стены и двое на постах
            SpawnEnemy(guardPrefab, route, new Vector2(37.5f, -5f), 180f, new Vector2(0f, 9f), 1.2f);
            SpawnEnemy(guardPrefab, route, new Vector2(39f, 5.5f), 200f, Vector2.zero, 0f);
            SpawnEnemy(guardPrefab, route, new Vector2(32f, -5.5f), 150f, Vector2.zero, 0f);
            // F: дрон облетает склад по кругу (точки — смещения от старта), патруль у западной стены, пост у двери
            SpawnPerimeter(dronePrefab, route, new Vector2(32.5f, 24.5f), 0f, 4.5f,
                new Vector2(12f, 0f), new Vector2(12f, -8.7f), new Vector2(0f, -8.7f));
            SpawnEnemy(guardPrefab, route, new Vector2(31.5f, 17f), 90f, new Vector2(0f, 6f), 1.2f);
            SpawnEnemy(guardPrefab, route, new Vector2(44f, 23f), 225f, Vector2.zero, 0f);
            // D: пост за дверью склада
            SpawnEnemy(guardPrefab, route, new Vector2(52.5f, 20.1f), 180f, Vector2.zero, 0f);
            // E: дрон по периметру зала, патруль по центру и двое по углам
            SpawnPerimeter(dronePrefab, route, new Vector2(60f, 28f), 0f, 4.5f,
                new Vector2(11.5f, 0f), new Vector2(11.5f, -16f), new Vector2(-3.5f, -16f), new Vector2(-3.5f, 0f));
            SpawnEnemy(guardPrefab, route, new Vector2(60.5f, 20f), 0f, new Vector2(7f, 0f), 1.3f);
            SpawnEnemy(guardPrefab, route, new Vector2(66f, 11.5f), 120f, Vector2.zero, 0f);
            SpawnEnemy(guardPrefab, route, new Vector2(71f, 28f), 225f, Vector2.zero, 0f);

            // Тот, кто стреляет в спину: появляется у входа в зал в самом конце
            EnemyAI assassin = SpawnEnemy(guardPrefab, route, new Vector2(54.8f, 20.1f), 0f, Vector2.zero, 0f);
            assassin.name = "Assassin";

            // Свет: обычный и тревожный
            AddLight(lights, "Route_A", new Vector2(19.6f, 0f), new Color(0.5f, 0.55f, 0.8f), 5f, 0.7f);
            AddLight(lights, "Route_B", new Vector2(33f, 0f), new Color(0.55f, 0.6f, 0.85f), 10f, 0.9f);
            AddLight(lights, "Route_C", new Vector2(36.5f, 10.5f), new Color(0.5f, 0.55f, 0.8f), 4f, 0.7f);
            AddLight(lights, "Route_F", new Vector2(38f, 20f), new Color(0.55f, 0.6f, 0.85f), 10f, 0.85f);
            AddLight(lights, "Route_D", new Vector2(50f, 20.1f), new Color(0.5f, 0.55f, 0.8f), 5f, 0.7f);
            AddLight(lights, "Route_E", new Vector2(64f, 20f), new Color(0.55f, 0.6f, 0.85f), 12f, 0.8f);
            // Дневной свет снаружи заливает зал — выход видно ещё от входа
            AddLight(lights, "Exit_Daylight", new Vector2(76.5f, 20.1f), new Color(0.95f, 1f, 0.95f), 18f, 4f).falloffIntensity = 0.3f;
            AddLight(lights, "Exit_Doorway", new Vector2(73f, 20.1f), new Color(0.9f, 1f, 0.92f), 8f, 4f).falloffIntensity = 0.3f;
            AddLight(lights, "Exit_Sign", new Vector2(72.2f, 23.8f), new Color(0.35f, 1f, 0.55f), 3f, 1.2f);

            var alarms = new List<Light2D>
            {
                AddLight(lights, "Alarm_A", new Vector2(19.6f, 0f), new Color(1f, 0.1f, 0.15f), 7f, 1.8f),
                AddLight(lights, "Alarm_B", new Vector2(33f, 0f), new Color(1f, 0.1f, 0.15f), 11f, 1.8f),
                AddLight(lights, "Alarm_F", new Vector2(38f, 20f), new Color(1f, 0.1f, 0.15f), 11f, 1.6f),
                AddLight(lights, "Alarm_D", new Vector2(50f, 20.1f), new Color(1f, 0.1f, 0.15f), 6f, 1.6f),
                AddLight(lights, "Alarm_E", new Vector2(64f, 20f), new Color(1f, 0.1f, 0.15f), 13f, 1.6f),
            };

            var start = new GameObject("EscapeStart").transform;
            start.SetParent(route);
            start.position = new Vector2(15.4f, 0f);

            return new EscapeRefs
            {
                Start = start,
                FirstDoor = doorA,
                Corridor = CreateZone(route, "Zone_Corridor", new Vector2(22f, 0f), new Vector2(2f, 3.2f)),
                GuardRoom = CreateZone(route, "Zone_GuardRoom", new Vector2(27.6f, 0f), new Vector2(1.6f, 3.2f)),
                DroneCorridor = CreateZone(route, "Zone_DroneCorridor", new Vector2(36.5f, 12.5f), new Vector2(3.2f, 1.4f)),
                Hall = CreateZone(route, "Zone_Hall", new Vector2(55.6f, 20.1f), new Vector2(1.6f, 3.6f)),
                Exit = CreateZone(route, "Zone_Exit", new Vector2(70.5f, 20.1f), new Vector2(1.6f, 4.8f)),
                Assassin = assassin,
                Alarms = alarms,
            };
        }

        internal static TriggerZone CreateZone(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            BoxCollider2D col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
            return go.AddComponent<TriggerZone>();
        }

        internal static EnemyAI SpawnEnemy(GameObject prefab, Transform parent, Vector2 position, float facing, Vector2 patrol, float speed)
        {
            GameObject enemy = Spawn(prefab, parent, position);
            enemy.transform.Find("Body").rotation = Quaternion.Euler(0f, 0f, facing);
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            Configure(ai, so =>
            {
                P(so, "_patrolOffset").vector2Value = patrol;
                P(so, "_patrolSpeed").floatValue = speed;
            });
            return ai;
        }

        // Обход по кругу через точки perimeter (относительно старта): дроны и патрульные
        internal static EnemyAI SpawnPerimeter(GameObject prefab, Transform parent, Vector2 position, float facing, float speed,
            params Vector2[] perimeter)
        {
            EnemyAI ai = SpawnEnemy(prefab, parent, position, facing, Vector2.zero, speed);
            Configure(ai, so => SetVectorArray(so, "_perimeter", perimeter));
            return ai;
        }

        private static void SetVectorArray(SerializedObject so, string name, Vector2[] values)
        {
            SerializedProperty array = P(so, name);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).vector2Value = values[i];
        }

        private static Vector2 HexVertex(int k)
        {
            float angle = (30f + 60f * k) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * LabRadius;
        }

        internal static void CreateWall(Transform parent, string name, Vector2 a, Vector2 b, Sprite sprite, int layer, int order, bool lit = true)
        {
            Vector2 d = b - a;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            SpriteRenderer sr = CreateSprite(name, parent, sprite, (a + b) * 0.5f, order, lit, angle);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(d.magnitude, WallThickness);
            sr.gameObject.layer = layer;
            BoxCollider2D col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = sr.size;
        }

        // angle 90 — вертикальная дверь, 0 — горизонтальная. Панель уезжает вдоль своей длины.
        private static Door CreateDoor(Transform parent, string name, Vector2 position, Sprite panelSprite, Vector2 lampPosition,
            float angle = 90f, float length = DoorLength)
        {
            var root = new GameObject(name) { layer = WallLayer };
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(length, WallThickness);

            SpriteRenderer panel = CreateSprite("Panel", root.transform, panelSprite, Vector2.zero, 4);
            panel.drawMode = SpriteDrawMode.Tiled;
            panel.size = new Vector2(length, WallThickness);

            SpriteRenderer lamp = CreateSprite(name + "_Lamp", parent, s_white, lampPosition, 12, false);
            Light2D lampLight = AddLight(lamp.transform, "Light", lampPosition, Color.red, 2.5f, 1.2f);

            Door door = root.AddComponent<Door>();
            Configure(door, so =>
            {
                P(so, "_collider").objectReferenceValue = col;
                P(so, "_panel").objectReferenceValue = panel.transform;
                P(so, "_openOffset").vector2Value = new Vector2(length, 0f);
                P(so, "_statusLamp").objectReferenceValue = lamp;
                P(so, "_statusLight").objectReferenceValue = lampLight;
            });
            return door;
        }

        private static void CreateProp(string name, Transform parent, Sprite sprite, Vector2 position)
        {
            SpriteRenderer sr = CreateSprite(name, parent, sprite, position, 3);
            sr.gameObject.layer = WallLayer;
            BoxCollider2D col = sr.gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 0.75f);
        }

        private static TriggerZone CreateMarker(Transform parent, Sprite sprite, Vector2 position)
        {
            var go = new GameObject("Marker");
            go.transform.SetParent(parent);
            go.transform.position = position;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            SpriteRenderer sr = CreateSprite("Ring", go.transform, sprite, Vector2.zero, -80, false);
            sr.color = Cyan;
            Light2D light = AddLight(go.transform, "Light", position, Cyan, 2.2f, 1f);

            Pulse pulse = sr.gameObject.AddComponent<Pulse>();
            Configure(pulse, so =>
            {
                P(so, "_renderer").objectReferenceValue = sr;
                P(so, "_light").objectReferenceValue = light;
            });

            return go.AddComponent<TriggerZone>();
        }

        // ---------------------------------------------------------------- Префабы

        // Общие для туториала и миссий: данные взломов, эффекты, следы на полу и пули
        internal struct CommonPrefabs
        {
            public QuickhackData Unlock;
            public QuickhackData ShortCircuit;
            public GameObject Sparks;
            public GameObject Explosion;
            public GameObject Electric;
            public GameObject Blood;
            public GameObject Muzzle;
            public GameObject Remains;
            public GameObject BloodPool;
            public Bullet PlayerBullet;
            public Bullet EnemyBullet;
        }

        // Вызывать после EditorSceneManager.NewScene и LoadCommonAssets
        internal static CommonPrefabs CreateCommonPrefabs()
        {
            MainMenuSceneBuilder.EnsureFolder(PrefabsDir);
            MainMenuSceneBuilder.EnsureFolder(QuickhacksDir);
            Material particleMat = MainMenuSceneBuilder.GetOrCreateMaterial("ParticleUnlit", new Material(s_unlit));
            GameObject sparks = BurstPrefab("FX_Sparks", Yellow, Color.white, 10, 3f, 8f, 0.1f, 0.3f, 0.06f, 0.12f, particleMat);
            return new CommonPrefabs
            {
                // Цены под 15 слотов RAM: та же доля памяти, что была при 4 слотах (1 и 2)
                Unlock = CreateQuickhack("Unlock", "ВЗЛОМ ЗАМКА", 4, 0.6f, QuickhackEffect.Unlock),
                ShortCircuit = CreateQuickhack("ShortCircuit", "КОРОТКОЕ ЗАМЫКАНИЕ", 7, 1f, QuickhackEffect.ShortCircuit),
                Sparks = sparks,
                Explosion = BurstPrefab("FX_Explosion", new Color(1f, 0.55f, 0.15f), Magenta, 40, 2f, 9f, 0.3f, 0.7f, 0.08f, 0.2f, particleMat),
                Electric = BurstPrefab("FX_ShortCircuit", Cyan, Color.white, 40, 1f, 7f, 0.3f, 0.9f, 0.05f, 0.12f, particleMat),
                // «Синяя кровь» — проводящая жидкость для имплантов, у людей этого мира кровь синяя
                Blood = BurstPrefab("FX_Blood", BloodLight, BloodDark, 30, 1f, 6f, 0.3f, 0.8f, 0.08f, 0.16f, particleMat),
                Muzzle = BurstPrefab("FX_Muzzle", new Color(1f, 0.9f, 0.5f), Color.white, 6, 4f, 10f, 0.04f, 0.09f, 0.06f, 0.1f, particleMat),
                Remains = RemainsPrefab("Remains_Scorch", SpriteFactory.Scorch()),
                BloodPool = RemainsPrefab("Remains_Blood", SpriteFactory.BloodPool()),
                PlayerBullet = BulletPrefab("Bullet_Player", SpriteFactory.Bullet("bullet_player", new Color(1f, 0.95f, 0.6f)), 32f, sparks),
                // Вражеские пули медленнее игроцких — от них можно увернуться
                EnemyBullet = BulletPrefab("Bullet_Turret", SpriteFactory.Bullet("bullet_turret", new Color(1f, 0.3f, 0.35f)), 23f, sparks),
            };
        }

        // Игрок со всеми механиками туториала: ходьба, прицел, стрельба, RAM и взлом.
        // legs — кадры ног: стоит, левый шаг, проход, правый шаг
        // magazine — патронов в магазине (0 — без перезарядки; в миссиях магазин задаёт WeaponData)
        internal static GameObject PlayerPrefab(string name, Sprite sprite, Sprite[] legs, float legsDistancePerFrame, float radius,
            CommonPrefabs common, int magazine = 0, Sprite corpse = null)
        {
            Bullet bullet = common.PlayerBullet;
            GameObject muzzleFx = common.Muzzle;
            GameObject deathFx = common.Blood;

            var root = new GameObject(name) { layer = PlayerLayer };
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            root.AddComponent<CircleCollider2D>().radius = radius;

            AddLegs(root, rb, legs[0], legs[1], legs[2], legs[3], legsDistancePerFrame);

            SpriteRenderer body = CreateSprite("Body", root.transform, sprite, Vector2.zero, 10);
            body.gameObject.layer = PlayerLayer;
            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(body.transform, false);
            muzzle.localPosition = new Vector3(0.55f, 0f, 0f);
            AddLight(root.transform, "Light", Vector2.zero, new Color(0.6f, 0.9f, 1f), 3f, 0.6f);

            Health health = root.AddComponent<Health>();
            Configure(health, so =>
            {
                P(so, "_maxHealth").intValue = 1;
                P(so, "_destroyOnDeath").boolValue = false;
                P(so, "_deathFxPrefab").objectReferenceValue = deathFx;
                P(so, "_flashRenderer").objectReferenceValue = body;
                P(so, "_corpseSprite").objectReferenceValue = corpse;
            });

            Weapon weapon = root.AddComponent<Weapon>();
            Configure(weapon, so =>
            {
                P(so, "_bulletPrefab").objectReferenceValue = bullet;
                P(so, "_muzzle").objectReferenceValue = muzzle;
                P(so, "_cooldown").floatValue = 0.3f;
                P(so, "_spreadDegrees").floatValue = 3f;
                P(so, "_hitMask").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << DoorLayer) | (1 << EnemyLayer);
                P(so, "_muzzleFxPrefab").objectReferenceValue = muzzleFx;
                P(so, "_magazineSize").intValue = magazine;
            });

            // Перезарядка-мини-игра; работает, только если у оружия есть магазин (в миссиях — из WeaponData)
            ActiveReload reload = root.AddComponent<ActiveReload>();
            Configure(reload, so =>
            {
                P(so, "_weapon").objectReferenceValue = weapon;
                P(so, "_health").objectReferenceValue = health;
                P(so, "_pixel").objectReferenceValue = s_white;
                P(so, "_material").objectReferenceValue = s_unlit;
            });

            RamPool ram = root.AddComponent<RamPool>();
            Configure(ram, so =>
            {
                P(so, "_maxRam").intValue = RamSlots;
                // Полная шкала восстанавливается примерно за 20 секунд
                P(so, "_secondsPerSlot").floatValue = 1.3f;
            });
            HackingController hacking = root.AddComponent<HackingController>();
            Configure(hacking, so =>
            {
                P(so, "_ram").objectReferenceValue = ram;
                P(so, "_health").objectReferenceValue = health;
                P(so, "_sightBlockers").intValue = 1 << WallLayer;
            });

            PlayerController controller = root.AddComponent<PlayerController>();
            Configure(controller, so =>
            {
                P(so, "_body").objectReferenceValue = body.transform;
                P(so, "_weapon").objectReferenceValue = weapon;
                P(so, "_health").objectReferenceValue = health;
                P(so, "_bodyRenderer").objectReferenceValue = body;
            });

            return SavePrefab(root, name);
        }

        private static GameObject DronePrefab(Sprite sprite, GameObject deathFx, GameObject remains)
        {
            var root = new GameObject("TrainingDrone") { layer = EnemyLayer };
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            root.AddComponent<CircleCollider2D>().radius = 0.4f;

            SpriteRenderer body = CreateSprite("Body", root.transform, sprite, Vector2.zero, 10);
            body.gameObject.layer = EnemyLayer;
            AddLight(root.transform, "Light", Vector2.zero, new Color(1f, 0.2f, 0.3f), 1.6f, 0.7f);

            Health health = root.AddComponent<Health>();
            Configure(health, so =>
            {
                P(so, "_maxHealth").intValue = 2;
                P(so, "_deathFxPrefab").objectReferenceValue = deathFx;
                P(so, "_remainsPrefab").objectReferenceValue = remains;
                P(so, "_flashRenderer").objectReferenceValue = body;
            });

            TrainingBot bot = root.AddComponent<TrainingBot>();
            Configure(bot, so => P(so, "_body").objectReferenceValue = body.transform);

            return SavePrefab(root, "TrainingDrone");
        }

        private static GameObject TargetPrefab(Sprite sprite, GameObject deathFx, GameObject remains)
        {
            var root = new GameObject("TrainingTarget") { layer = EnemyLayer };
            root.AddComponent<CircleCollider2D>().radius = 0.42f;
            SpriteRenderer body = CreateSprite("Body", root.transform, sprite, Vector2.zero, 8);
            body.gameObject.layer = EnemyLayer;

            Health health = root.AddComponent<Health>();
            Configure(health, so =>
            {
                P(so, "_maxHealth").intValue = 1;
                P(so, "_deathFxPrefab").objectReferenceValue = deathFx;
                P(so, "_remainsPrefab").objectReferenceValue = remains;
            });

            return SavePrefab(root, "TrainingTarget");
        }

        private static GameObject TurretPrefab(Sprite baseSprite, Sprite headSprite, Bullet bullet, GameObject muzzleFx)
        {
            var root = new GameObject("Turret") { layer = EnemyLayer };
            root.AddComponent<CircleCollider2D>().radius = 0.4f;
            CreateSprite("Base", root.transform, baseSprite, Vector2.zero, 9).gameObject.layer = EnemyLayer;
            SpriteRenderer head = CreateSprite("Head", root.transform, headSprite, Vector2.zero, 11);
            head.gameObject.layer = EnemyLayer;
            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(head.transform, false);
            muzzle.localPosition = new Vector3(0.6f, 0f, 0f);
            AddLight(head.transform, "Light", Vector2.zero, new Color(1f, 0.2f, 0.25f), 2f, 0.8f);

            Health health = root.AddComponent<Health>();
            Configure(health, so =>
            {
                P(so, "_maxHealth").intValue = 999;
                P(so, "_destroyOnDeath").boolValue = false;
            });

            Weapon weapon = root.AddComponent<Weapon>();
            Configure(weapon, so =>
            {
                P(so, "_bulletPrefab").objectReferenceValue = bullet;
                P(so, "_muzzle").objectReferenceValue = muzzle;
                P(so, "_cooldown").floatValue = 0.1f;
                P(so, "_spreadDegrees").floatValue = 3f;
                P(so, "_hitMask").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << PlayerLayer);
                P(so, "_muzzleFxPrefab").objectReferenceValue = muzzleFx;
            });

            Turret turret = root.AddComponent<Turret>();
            Configure(turret, so =>
            {
                P(so, "_head").objectReferenceValue = head.transform;
                P(so, "_weapon").objectReferenceValue = weapon;
            });

            return SavePrefab(root, "Turret");
        }

        // Враг корпорации Е: видит, реагирует, стреляет; взламывается «Коротким замыканием»
        internal static GameObject EnemyPrefab(string name, string deviceName, Sprite sprite, int health, float cooldown, float reaction,
            Bullet bullet, GameObject muzzleFx, GameObject deathFx, GameObject remains, QuickhackData shortCircuit, GameObject electricFx,
            bool chases, Sprite corpse = null)
        {
            var root = new GameObject(name) { layer = EnemyLayer };
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            CircleCollider2D col = root.AddComponent<CircleCollider2D>();
            col.radius = 0.38f;

            SpriteRenderer body = CreateSprite("Body", root.transform, sprite, Vector2.zero, 10);
            body.gameObject.layer = EnemyLayer;
            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(body.transform, false);
            muzzle.localPosition = new Vector3(0.6f, 0f, 0f);
            AddLight(root.transform, "Light", Vector2.zero, new Color(1f, 0.45f, 0.15f), 1.6f, 0.6f);

            Health hp = root.AddComponent<Health>();
            Configure(hp, so =>
            {
                P(so, "_maxHealth").intValue = health;
                P(so, "_deathFxPrefab").objectReferenceValue = deathFx;
                P(so, "_remainsPrefab").objectReferenceValue = remains;
                P(so, "_flashRenderer").objectReferenceValue = body;
                P(so, "_corpseSprite").objectReferenceValue = corpse;
            });

            Weapon weapon = root.AddComponent<Weapon>();
            Configure(weapon, so =>
            {
                P(so, "_bulletPrefab").objectReferenceValue = bullet;
                P(so, "_muzzle").objectReferenceValue = muzzle;
                P(so, "_cooldown").floatValue = cooldown;
                P(so, "_spreadDegrees").floatValue = 6f;
                P(so, "_hitMask").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << DoorLayer) | (1 << PlayerLayer);
                P(so, "_muzzleFxPrefab").objectReferenceValue = muzzleFx;
            });

            EnemyAI ai = root.AddComponent<EnemyAI>();
            Configure(ai, so =>
            {
                P(so, "_body").objectReferenceValue = body.transform;
                P(so, "_weapon").objectReferenceValue = weapon;
                P(so, "_health").objectReferenceValue = hp;
                P(so, "_reactionTime").floatValue = reaction;
                P(so, "_sightBlockers").intValue = (1 << WallLayer) | (1 << DoorLayer);
                P(so, "_moveBlockers").intValue = (1 << WallLayer) | (1 << GlassLayer);
                P(so, "_bodyRadius").floatValue = col.radius;
                // Чуть казуальнее: видят не так далеко и вплотную со спины замечают ближе
                P(so, "_viewDistance").floatValue = 9.5f;
                P(so, "_closeRange").floatValue = 2.2f;
                // Охрана преследует и дольше ищет; дрон держит периметр, зато быстрее крутится
                P(so, "_chases").boolValue = chases;
                P(so, "_forgetTime").floatValue = chases ? 5f : 3f;
                P(so, "_turnSpeed").floatValue = chases ? 420f : 620f;
            });

            HackableDevice hack = root.AddComponent<HackableDevice>();
            Configure(hack, so =>
            {
                P(so, "_deviceName").stringValue = deviceName;
                SetArray(so, "_quickhacks", shortCircuit);
                P(so, "_collider").objectReferenceValue = col;
                SetArray(so, "_highlightRenderers", body);
                P(so, "_shortCircuitFxPrefab").objectReferenceValue = electricFx;
                // Преследуют только люди (охрана, бандиты): от замыкания они бьются в судорогах, дроны гибнут сразу
                P(so, "_isHuman").boolValue = chases;
                P(so, "_glitchMaterial").objectReferenceValue = s_unlit;
            });

            return SavePrefab(root, name);
        }

        private static Bullet BulletPrefab(string name, Sprite sprite, float speed, GameObject impactFx)
        {
            var root = new GameObject(name);
            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = s_unlit;
            sr.sortingOrder = 20;
            Bullet bullet = root.AddComponent<Bullet>();
            Configure(bullet, so =>
            {
                P(so, "_speed").floatValue = speed;
                P(so, "_impactFxPrefab").objectReferenceValue = impactFx;
            });
            return SavePrefab(root, name).GetComponent<Bullet>();
        }

        private static GameObject RemainsPrefab(string name, Sprite sprite)
        {
            var root = new GameObject(name);
            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = s_lit;
            sr.sortingOrder = -70;
            return SavePrefab(root, name);
        }

        internal static GameObject BurstPrefab(string name, Color colorA, Color colorB, int count, float speedMin, float speedMax,
            float lifeMin, float lifeMax, float sizeMin, float sizeMax, Material material)
        {
            var root = new GameObject(name);
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = count * 2;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;

            ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 4f;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 30;

            return SavePrefab(root, name);
        }

        // Ноги под корпусом, как в Hotline Miami: смотрят по ходьбе, кадры — стоит, левый шаг, проход, правый шаг.
        // distancePerFrame — сколько юнитов пути на один кадр (больше — медленнее шаг)
        internal static void AddLegs(GameObject root, Rigidbody2D rb, Sprite idle, Sprite left, Sprite pass, Sprite right, float distancePerFrame)
        {
            SpriteRenderer legs = CreateSprite("Legs", root.transform, idle, Vector2.zero, 9);
            legs.gameObject.layer = root.layer;
            LegsAnimator animator = root.AddComponent<LegsAnimator>();
            Configure(animator, so =>
            {
                P(so, "_rb").objectReferenceValue = rb;
                P(so, "_legs").objectReferenceValue = legs;
                SetArray(so, "_frames", idle, left, pass, right, pass);
                P(so, "_distancePerFrame").floatValue = distancePerFrame;
            });
        }

        internal static GameObject SavePrefab(GameObject root, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        internal static GameObject Spawn(GameObject prefab, Transform parent, Vector2 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.position = position;
            return go;
        }

        private static Health SpawnDrone(GameObject prefab, Transform parent, Vector2 position, Vector2 patrol, float speed)
        {
            GameObject drone = Spawn(prefab, parent, position);
            Configure(drone.GetComponent<TrainingBot>(), so =>
            {
                P(so, "_patrolOffset").vector2Value = patrol;
                P(so, "_speed").floatValue = speed;
            });
            return drone.GetComponent<Health>();
        }

        internal static QuickhackData CreateQuickhack(string fileName, string displayName, int cost, float upload, QuickhackEffect effect)
        {
            string path = $"{QuickhacksDir}/{fileName}.asset";
            var hack = AssetDatabase.LoadAssetAtPath<QuickhackData>(path);
            if (hack == null && System.IO.File.Exists(path))
            {
                // Ассет есть на диске, но ещё не подгружен (например, сразу после перекомпиляции)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                hack = AssetDatabase.LoadAssetAtPath<QuickhackData>(path);
                if (hack == null)
                {
                    throw new InvalidOperationException($"Не удалось загрузить {path} — сцена не пересобрана, чтобы не потерять ссылки на взломы.");
                }
            }

            if (hack == null)
            {
                hack = ScriptableObject.CreateInstance<QuickhackData>();
                AssetDatabase.CreateAsset(hack, path);
                // Значения задаём только при создании — дальше их тюнят в инспекторе
                Configure(hack, so =>
                {
                    P(so, "_displayName").stringValue = displayName;
                    P(so, "_ramCost").intValue = cost;
                    P(so, "_uploadTime").floatValue = upload;
                    P(so, "_effect").enumValueIndex = (int)effect;
                });
            }

            return hack;
        }

        // Каждое устройство должно ссылаться на сохранённые ассеты взломов, иначе в сцене окажется пустая ссылка
        internal static void ValidateQuickhacks()
        {
            foreach (HackableDevice device in Object.FindObjectsByType<HackableDevice>())
            {
                foreach (QuickhackData hack in device.Quickhacks)
                {
                    if (hack == null || !EditorUtility.IsPersistent(hack))
                    {
                        throw new InvalidOperationException($"У {device.name} взлом не сохранён как ассет — сцена не сохранена.");
                    }
                }
            }
        }

        // ---------------------------------------------------------------- HUD

        internal struct HudRefs
        {
            public SubtitleBox Subtitles;
            public ObjectivePanel Objective;
            public HackingHud HackingHud;
            public ScreenFader Fader;
            public TutorialEndScreen EndScreen;
            public RectTransform LetterboxTop;
            public RectTransform LetterboxBottom;
            public AmmoHud Ammo;
            public ScoreCounter Score;
        }

        // Полный боевой HUD: RAM, взлом, задача, субтитры, перекрестие, затемнение и итоговый экран.
        // endTitle — заголовок итогового экрана, nextScene — куда ведёт кнопка «Вперёд»
        internal static HudRefs BuildHud(HackingController hacking, TMP_FontAsset mono, TMP_FontAsset audiowide,
            string endTitle = "ТУТОРИАЛ ОКОНЧЕН", string nextScene = Interlude01SceneBuilder.SceneName)
        {
            RectTransform canvasRect = BuildCanvas();

            // Режим сканирования
            Image scan = Img("ScanOverlay", canvasRect, new Color(0.15f, 0.9f, 1f, 0.12f));
            MainMenuSceneBuilder.Stretch(scan.rectTransform);
            CanvasGroup scanGroup = Group(scan.gameObject, 0f);
            TextMeshProUGUI scanLabel = Text("Label", scan.rectTransform, mono, "РЕЖИМ СКАНИРОВАНИЯ", 26f, Cyan, TextAlignmentOptions.Center);
            Anchor(scanLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 40f));

            ObjectivePanel objectivePanel = BuildObjective(canvasRect, mono);

            // RAM
            // Узкие слоты, чтобы все 15 поместились в одну строку
            const float slotStep = 26f;
            float ramWidth = RamSlots * slotStep;
            RectTransform ram = Anchor(MainMenuSceneBuilder.CreateRect("RAM", canvasRect), Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(ramWidth, 80f));
            CanvasGroup ramGroup = Group(ram.gameObject, 0f);
            // Подложка с неоновой кромкой и подписью импланта
            Image ramBack = Img("Back", ram, new Color(0.02f, 0.04f, 0.08f, 0.75f));
            Anchor(ramBack.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-16f, -14f), new Vector2(ramWidth + 26f, 112f));
            Image ramEdge = Img("Edge", ram, Magenta);
            Anchor(ramEdge.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-16f, -14f), new Vector2(4f, 112f));
            TextMeshProUGUI ramHeader = Text("Header", ram, mono, "НЕЙРОИМПЛАНТ // ОЗУ", 16f, Magenta, TextAlignmentOptions.BottomLeft);
            Anchor(ramHeader.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 72f), new Vector2(ramWidth, 22f));
            TextMeshProUGUI ramLabel = Text("Label", ram, audiowide, $"RAM {RamSlots}/{RamSlots}", 26f, Cyan, TextAlignmentOptions.BottomLeft);
            ramLabel.richText = true;
            Anchor(ramLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 30f), new Vector2(ramWidth, 40f));
            var slots = new Image[RamSlots];
            for (int i = 0; i < slots.Length; i++)
            {
                // Пивот в центре — слот "подпрыгивает" при заряде и трате
                Image bg = Img($"Slot{i}", ram, new Color(0.05f, 0.15f, 0.18f, 0.9f));
                Anchor(bg.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(i * slotStep + (slotStep - 6f) * 0.5f, 11f), new Vector2(slotStep - 6f, 22f));
                Image fill = Img("Fill", bg.rectTransform, Cyan);
                MainMenuSceneBuilder.Stretch(fill.rectTransform);
                MakeFilled(fill);
                slots[i] = fill;
            }

            // Панель взломов у цели
            Image hackPanel = Img("HackPanel", canvasRect, new Color(0.03f, 0.02f, 0.07f, 0.9f));
            Anchor(hackPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(520f, 130f));
            CanvasGroup hackGroup = Group(hackPanel.gameObject, 0f);
            Image accent = Img("Accent", hackPanel.rectTransform, Yellow);
            Anchor(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, 130f));
            TextMeshProUGUI hackTitle = Text("Title", hackPanel.rectTransform, mono, "УСТРОЙСТВО", 24f, Yellow, TextAlignmentOptions.TopLeft);
            Anchor(hackTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -12f), new Vector2(480f, 32f));
            TextMeshProUGUI hackList = Text("List", hackPanel.rectTransform, mono, string.Empty, 24f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(hackList.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -52f), new Vector2(480f, 70f));

            // Загрузка взлома
            RectTransform upload = Anchor(MainMenuSceneBuilder.CreateRect("Upload", canvasRect), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 50f));
            CanvasGroup uploadGroup = Group(upload.gameObject, 0f);
            TextMeshProUGUI uploadLabel = Text("Label", upload, mono, "ЗАГРУЗКА", 20f, Cyan, TextAlignmentOptions.Bottom);
            Anchor(uploadLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(340f, 30f));
            Image uploadBg = Img("Bar", upload, new Color(0.05f, 0.15f, 0.18f, 0.9f));
            Anchor(uploadBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(340f, 12f));
            Image uploadFill = Img("Fill", uploadBg.rectTransform, Cyan);
            MainMenuSceneBuilder.Stretch(uploadFill.rectTransform);
            MakeFilled(uploadFill);

            TextMeshProUGUI message = Text("Message", canvasRect, mono, string.Empty, 34f, new Color(1f, 0.25f, 0.3f), TextAlignmentOptions.Center);
            Anchor(message.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(900f, 50f));

            Image warning = BuildWarningWindow(canvasRect, mono, out CanvasGroup warningGroup, out TextMeshProUGUI warningText);

            HackingHud hackingHud = canvasRect.gameObject.AddComponent<HackingHud>();
            Configure(hackingHud, so =>
            {
                P(so, "_hacking").objectReferenceValue = hacking;
                P(so, "_canvasRect").objectReferenceValue = canvasRect;
                P(so, "_ramGroup").objectReferenceValue = ramGroup;
                SetArray(so, "_ramSlots", slots);
                P(so, "_ramLabel").objectReferenceValue = ramLabel;
                P(so, "_scanOverlay").objectReferenceValue = scanGroup;
                P(so, "_hackPanel").objectReferenceValue = hackPanel.rectTransform;
                P(so, "_hackPanelGroup").objectReferenceValue = hackGroup;
                P(so, "_hackTitle").objectReferenceValue = hackTitle;
                P(so, "_hackList").objectReferenceValue = hackList;
                P(so, "_uploadPanel").objectReferenceValue = upload;
                P(so, "_uploadGroup").objectReferenceValue = uploadGroup;
                P(so, "_uploadFill").objectReferenceValue = uploadFill;
                P(so, "_uploadLabel").objectReferenceValue = uploadLabel;
                P(so, "_message").objectReferenceValue = message;
                P(so, "_warningPanel").objectReferenceValue = warning.rectTransform;
                P(so, "_warningGroup").objectReferenceValue = warningGroup;
                P(so, "_warningText").objectReferenceValue = warningText;
            });

            AmmoHud ammoHud = BuildAmmo(canvasRect, hacking.GetComponent<Weapon>(), hacking.GetComponent<ActiveReload>());

            // Киношные полосы под субтитрами, чтобы не закрывать их
            BuildLetterbox(canvasRect, out RectTransform letterboxTop, out RectTransform letterboxBottom);

            // Субтитры голоса
            Image subtitles = Img("Subtitles", canvasRect, new Color(0f, 0f, 0f, 0.7f));
            Anchor(subtitles.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1000f, 150f));
            CanvasGroup subtitleGroup = Group(subtitles.gameObject, 0f);
            // Полоса слева — цвета говорящего (красит SubtitleBox)
            Image subtitleAccent = Img("Accent", subtitles.rectTransform, Magenta);
            Anchor(subtitleAccent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, 150f));
            TextMeshProUGUI speaker = Text("Speaker", subtitles.rectTransform, mono, "ОПЕРАТОР", 22f, Magenta, TextAlignmentOptions.TopLeft);
            Anchor(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -14f), new Vector2(600f, 30f));
            TextMeshProUGUI line = Text("Line", subtitles.rectTransform, mono, string.Empty, 32f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -50f), new Vector2(940f, 90f));
            line.textWrappingMode = TextWrappingModes.Normal;
            SubtitleBox subtitleBox = subtitles.gameObject.AddComponent<SubtitleBox>();
            Configure(subtitleBox, so =>
            {
                P(so, "_group").objectReferenceValue = subtitleGroup;
                P(so, "_speakerLabel").objectReferenceValue = speaker;
                P(so, "_lineLabel").objectReferenceValue = line;
                P(so, "_accent").objectReferenceValue = subtitleAccent;
            });

            // Перекрестие — под затемнением, чтобы исчезало на экране смерти
            Crosshair crosshair = BuildCrosshair(canvasRect, hacking.GetComponent<Weapon>(), hacking);
            Image crosshairImage = crosshair.GetComponent<Image>();

            BuildHint(canvasRect, mono, "ESC — пауза    ENTER / SPACE / ЛКМ — пропустить реплику");

            // Очки и серии — под затемнением (на экране смерти не видны)
            ScoreCounter scoreCounter = BuildScore(canvasRect, audiowide, hacking.GetComponent<Health>());

            // Затемнение и экран смерти — поверх всего
            ScreenFader screenFader = BuildFader(canvasRect, mono, new Color(1f, 0.2f, 0.3f));

            // Итоговый экран — поверх всего, включая затемнение
            Image endPanel = Img("EndScreen", canvasRect, new Color(0.02f, 0f, 0.05f, 0.7f));
            MainMenuSceneBuilder.Stretch(endPanel.rectTransform);
            CanvasGroup endGroup = Group(endPanel.gameObject, 0f);
            TextMeshProUGUI endTitleLabel = Text("Title", endPanel.rectTransform, mono, endTitle, 72f, new Color(1f, 0.85f, 0.97f), TextAlignmentOptions.Center);
            Anchor(endTitleLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 270f), new Vector2(1400f, 100f));
            Image divider = Img("Divider", endPanel.rectTransform, Magenta);
            Anchor(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 205f), new Vector2(1000f, 3f));

            // Слева — набегающий счёт и статистика, справа — ранг-печать
            TMP_FontAsset marker = MainMenuSceneBuilder.GetOrCreateFontAsset("PermanentMarker-Regular");
            TextMeshProUGUI scoreCaption = Text("ScoreCaption", endPanel.rectTransform, audiowide, "SCORE", 32f, Magenta, TextAlignmentOptions.Left);
            Anchor(scoreCaption.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-480f, 140f), new Vector2(600f, 40f));
            TextMeshProUGUI scoreShadow = Text("ScoreShadow", endPanel.rectTransform, marker, "0", 120f, new Color(0.45f, 0.05f, 0.35f), TextAlignmentOptions.Left);
            Anchor(scoreShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-474f, 46f), new Vector2(700f, 140f));
            TextMeshProUGUI scoreValue = Text("Score", endPanel.rectTransform, marker, "0", 120f, new Color(1f, 0.9f, 0.97f), TextAlignmentOptions.Left);
            Anchor(scoreValue.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-480f, 52f), new Vector2(700f, 140f));
            TextMeshProUGUI stats = Text("Stats", endPanel.rectTransform, audiowide, string.Empty, 26f, new Color(0.35f, 1f, 1f), TextAlignmentOptions.Left);
            Anchor(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-480f, -50f), new Vector2(800f, 40f));

            RectTransform rankRoot = MainMenuSceneBuilder.CreateRect("Rank", endPanel.rectTransform);
            Anchor(rankRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 10f), new Vector2(320f, 320f));
            CanvasGroup rankGroup = Group(rankRoot.gameObject, 0f);
            TextMeshProUGUI rankCaption = Text("Caption", rankRoot, audiowide, "RANK", 32f, Magenta, TextAlignmentOptions.Center);
            Anchor(rankCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(320f, 40f));
            TextMeshProUGUI rankShadow = Text("Shadow", rankRoot, marker, "S", 300f, new Color(0.45f, 0.05f, 0.35f), TextAlignmentOptions.Center);
            Anchor(rankShadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, -40f), new Vector2(320f, 320f));
            TextMeshProUGUI rankLetter = Text("Letter", rankRoot, marker, "S", 300f, Color.white, TextAlignmentOptions.Center);
            Anchor(rankLetter.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(320f, 320f));

            Image buttonImage = Img("Continue", endPanel.rectTransform, Color.white);
            buttonImage.raycastTarget = true;
            Anchor(buttonImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -230f), new Vector2(380f, 76f));
            buttonImage.gameObject.AddComponent<Outline>().effectColor = Magenta;
            Button button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.12f, 0.04f, 0.16f, 0.95f);
            colors.highlightedColor = new Color(0.75f, 0.15f, 0.6f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.5f, 0.1f, 0.4f, 1f);
            button.colors = colors;
            TextMeshProUGUI buttonLabel = Text("Label", buttonImage.rectTransform, mono, "ВПЕРЁД  >", 36f, Color.white, TextAlignmentOptions.Center);
            MainMenuSceneBuilder.Stretch(buttonLabel.rectTransform);

            TutorialEndScreen endScreen = endPanel.gameObject.AddComponent<TutorialEndScreen>();
            Configure(endScreen, so =>
            {
                P(so, "_group").objectReferenceValue = endGroup;
                P(so, "_continueButton").objectReferenceValue = button;
                P(so, "_crosshair").objectReferenceValue = crosshairImage.gameObject;
                P(so, "_nextScene").stringValue = nextScene;
                P(so, "_scoreLabel").objectReferenceValue = scoreValue;
                P(so, "_scoreShadow").objectReferenceValue = scoreShadow;
                P(so, "_statsLabel").objectReferenceValue = stats;
                P(so, "_rankLabel").objectReferenceValue = rankLetter;
                P(so, "_rankShadow").objectReferenceValue = rankShadow;
                P(so, "_rankGroup").objectReferenceValue = rankGroup;
            });

            BuildPauseMenu(canvasRect, mono);

            return new HudRefs
            {
                Subtitles = subtitleBox, Objective = objectivePanel, HackingHud = hackingHud, Fader = screenFader, EndScreen = endScreen,
                LetterboxTop = letterboxTop, LetterboxBottom = letterboxBottom, Ammo = ammoHud, Score = scoreCounter,
            };
        }

        // Пауза по Esc — поверх всего: тёмная вуаль, заголовок и три кнопки в стиле итогового экрана
        internal static PauseMenu BuildPauseMenu(RectTransform canvasRect, TMP_FontAsset mono)
        {
            Image panel = Img("PauseMenu", canvasRect, new Color(0.02f, 0f, 0.05f, 0.8f));
            MainMenuSceneBuilder.Stretch(panel.rectTransform);
            panel.raycastTarget = true;
            CanvasGroup group = Group(panel.gameObject, 0f);

            TextMeshProUGUI title = Text("Title", panel.rectTransform, mono, "ПАУЗА", 80f, new Color(1f, 0.85f, 0.97f), TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1000f, 100f));
            Anchor(Img("Divider", panel.rectTransform, Magenta).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 135f), new Vector2(560f, 3f));

            Button resume = PauseButton(panel.rectTransform, mono, "ПРОДОЛЖИТЬ", 40f);
            Button restart = PauseButton(panel.rectTransform, mono, "ЗАНОВО", -60f);
            Button menu = PauseButton(panel.rectTransform, mono, "ГЛАВНОЕ МЕНЮ", -160f);
            TextMeshProUGUI hint = Text("Hint", panel.rectTransform, mono, "ESC — продолжить", 22f, new Color(1f, 0.7f, 0.9f, 0.6f), TextAlignmentOptions.Center);
            Anchor(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(600f, 30f));

            PauseMenu pause = panel.gameObject.AddComponent<PauseMenu>();
            Configure(pause, so =>
            {
                P(so, "_group").objectReferenceValue = group;
                P(so, "_resumeButton").objectReferenceValue = resume;
                P(so, "_restartButton").objectReferenceValue = restart;
                P(so, "_menuButton").objectReferenceValue = menu;
            });
            return pause;
        }

        private static Button PauseButton(RectTransform parent, TMP_FontAsset mono, string label, float y)
        {
            Image image = Img(label, parent, Color.white);
            image.raycastTarget = true;
            Anchor(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(460f, 76f));
            image.gameObject.AddComponent<Outline>().effectColor = Magenta;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.12f, 0.04f, 0.16f, 0.95f);
            colors.highlightedColor = new Color(0.75f, 0.15f, 0.6f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.5f, 0.1f, 0.4f, 1f);
            button.colors = colors;
            MainMenuSceneBuilder.Stretch(Text("Label", image.rectTransform, mono, label, 34f, Color.white, TextAlignmentOptions.Center).rectTransform);
            return button;
        }

        // Счёт и серии как в Hotline Miami: справа сверху счёт маркером с маджентовой тенью, под ним — номер
        // серии крупно с неоновым раздвоением, очки серии и тающая полоска окна (3 с); очки над убитыми
        private static ScoreCounter BuildScore(RectTransform canvasRect, TMP_FontAsset audiowide, Health player)
        {
            TMP_FontAsset marker = MainMenuSceneBuilder.GetOrCreateFontAsset("PermanentMarker-Regular");
            Color shadowColor = new Color(0.45f, 0.05f, 0.35f);

            RectTransform root = MainMenuSceneBuilder.CreateRect("Score", canvasRect);
            MainMenuSceneBuilder.Stretch(root);
            CanvasGroup group = Group(root.gameObject, 1f);
            group.blocksRaycasts = false;

            // Всплывающие очки — шаблон в центре холста, ScoreCounter двигает копии за убитыми
            TextMeshProUGUI popup = Text("PopupTemplate", root, marker, "+400", 44f, Color.white, TextAlignmentOptions.Center);
            Anchor(popup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 70f));
            popup.outlineWidth = 0.25f;
            popup.outlineColor = new Color32(40, 0, 30, 255);

            // Счёт
            RectTransform scoreBox = MainMenuSceneBuilder.CreateRect("ScoreBox", root);
            Anchor(scoreBox, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -36f), new Vector2(520f, 110f));
            TextMeshProUGUI caption = Text("Caption", scoreBox, audiowide, "SCORE", 24f, Magenta, TextAlignmentOptions.TopRight);
            Anchor(caption.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(400f, 30f));
            TextMeshProUGUI scoreShadow = Text("Shadow", scoreBox, marker, "0", 72f, shadowColor, TextAlignmentOptions.TopRight);
            Anchor(scoreShadow.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(5f, -31f), new Vector2(520f, 90f));
            TextMeshProUGUI scoreText = Text("Value", scoreBox, marker, "0", 72f, new Color(1f, 0.92f, 0.98f), TextAlignmentOptions.TopRight);
            Anchor(scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(520f, 90f));

            // Серия
            // Колонка справа под счётом: «5x» крупно, под ней «COMBO» и очки серии, ниже — полоска окна
            RectTransform comboBox = MainMenuSceneBuilder.CreateRect("Combo", root);
            Anchor(comboBox, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -150f), new Vector2(420f, 190f));
            CanvasGroup comboGroup = Group(comboBox.gameObject, 0f);
            TextMeshProUGUI ghostA = Text("GhostA", comboBox, marker, "2x", 120f, new Color(1f, 0.15f, 0.4f, 0.6f), TextAlignmentOptions.TopRight);
            TextMeshProUGUI ghostB = Text("GhostB", comboBox, marker, "2x", 120f, new Color(0.2f, 1f, 1f, 0.6f), TextAlignmentOptions.TopRight);
            TextMeshProUGUI comboText = Text("Value", comboBox, marker, "2x", 120f, Color.white, TextAlignmentOptions.TopRight);
            foreach (TextMeshProUGUI t in new[] { ghostA, ghostB, comboText })
            {
                Anchor(t.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(420f, 130f));
            }
            TextMeshProUGUI comboLabel = Text("Label", comboBox, audiowide, "COMBO", 28f, Magenta, TextAlignmentOptions.Right);
            Anchor(comboLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -128f), new Vector2(160f, 40f));
            TextMeshProUGUI streak = Text("Streak", comboBox, marker, "+800", 38f, new Color(1f, 0.9f, 0.35f), TextAlignmentOptions.Right);
            Anchor(streak.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -124f), new Vector2(250f, 48f));
            Image barTrack = Img("BarTrack", comboBox, new Color(0.25f, 0.05f, 0.2f, 0.9f));
            Anchor(barTrack.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -176f), new Vector2(420f, 8f));
            Image bar = Img("Bar", barTrack.rectTransform, new Color(0.35f, 1f, 1f));
            bar.rectTransform.anchorMin = Vector2.zero;
            bar.rectTransform.anchorMax = Vector2.one;
            bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            bar.rectTransform.offsetMin = Vector2.zero;
            bar.rectTransform.offsetMax = Vector2.zero;

            ScoreCounter counter = root.gameObject.AddComponent<ScoreCounter>();
            Configure(counter, so =>
            {
                P(so, "_player").objectReferenceValue = player;
                P(so, "_group").objectReferenceValue = group;
                P(so, "_scoreText").objectReferenceValue = scoreText;
                P(so, "_scoreShadow").objectReferenceValue = scoreShadow;
                P(so, "_scoreBox").objectReferenceValue = scoreBox;
                P(so, "_comboGroup").objectReferenceValue = comboGroup;
                P(so, "_comboBox").objectReferenceValue = comboBox;
                P(so, "_comboText").objectReferenceValue = comboText;
                P(so, "_comboGhostA").objectReferenceValue = ghostA;
                P(so, "_comboGhostB").objectReferenceValue = ghostB;
                P(so, "_streakText").objectReferenceValue = streak;
                P(so, "_comboBar").objectReferenceValue = bar.rectTransform;
                P(so, "_popupTemplate").objectReferenceValue = popup;
            });
            return counter;
        }

        // Патроны как в Hotline Miami: «16/16rnds» на чёрной плашке справа внизу, с тёмной тенью
        private static AmmoHud BuildAmmo(RectTransform canvasRect, Weapon weapon, ActiveReload reload)
        {
            TMP_FontAsset marker = MainMenuSceneBuilder.GetOrCreateFontAsset("PermanentMarker-Regular");
            Image box = Img("Ammo", canvasRect, new Color(0f, 0f, 0f, 0.85f));
            Anchor(box.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 70f), new Vector2(330f, 84f));
            CanvasGroup group = Group(box.gameObject, 0f);
            // Полоска остатка патронов по низу плашки: тает с каждым выстрелом
            Image track = Img("AmmoTrack", box.rectTransform, new Color(0.25f, 0.05f, 0.2f, 0.9f));
            Anchor(track.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, new Vector2(330f, 8f));
            Image edge = Img("AmmoBar", track.rectTransform, Magenta);
            MainMenuSceneBuilder.Stretch(edge.rectTransform);
            MakeFilled(edge);
            TextMeshProUGUI shadow = Text("Shadow", box.rectTransform, marker, "16/16rnds", 54f, new Color(0.35f, 0.05f, 0.3f), TextAlignmentOptions.Center);
            Anchor(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(4f, -4f), new Vector2(330f, 84f));
            TextMeshProUGUI text = Text("Text", box.rectTransform, marker, "16/16rnds", 54f, new Color(1f, 0.72f, 0.9f), TextAlignmentOptions.Center);
            Anchor(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330f, 84f));

            AmmoHud ammo = box.gameObject.AddComponent<AmmoHud>();
            Configure(ammo, so =>
            {
                P(so, "_weapon").objectReferenceValue = weapon;
                P(so, "_reload").objectReferenceValue = reload;
                P(so, "_group").objectReferenceValue = group;
                P(so, "_box").objectReferenceValue = box.rectTransform;
                P(so, "_text").objectReferenceValue = text;
                P(so, "_shadow").objectReferenceValue = shadow;
                P(so, "_bar").objectReferenceValue = edge;
            });
            return ammo;
        }

        // Холст HUD 1920x1080 и EventSystem (для кнопок)
        internal static RectTransform BuildCanvas()
        {
            var go = new GameObject("HUD", typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            return (RectTransform)go.transform;
        }

        // Задача в левом верхнем углу
        internal static ObjectivePanel BuildObjective(RectTransform canvasRect, TMP_FontAsset mono)
        {
            RectTransform objective = Anchor(MainMenuSceneBuilder.CreateRect("Objective", canvasRect), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -36f), new Vector2(1000f, 90f));
            CanvasGroup objectiveGroup = Group(objective.gameObject, 0f);
            TextMeshProUGUI objectiveHeader = Text("Header", objective, mono, "ЗАДАЧА", 20f, Magenta, TextAlignmentOptions.TopLeft);
            Anchor(objectiveHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1000f, 28f));
            TextMeshProUGUI objectiveLabel = Text("Label", objective, mono, string.Empty, 30f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(objectiveLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 44f));
            ObjectivePanel objectivePanel = objective.gameObject.AddComponent<ObjectivePanel>();
            Configure(objectivePanel, so =>
            {
                P(so, "_group").objectReferenceValue = objectiveGroup;
                P(so, "_label").objectReferenceValue = objectiveLabel;
            });
            return objectivePanel;
        }

        // Киношные полосы для катсцен (высота 0 — спрятаны)
        internal static void BuildLetterbox(RectTransform canvasRect, out RectTransform top, out RectTransform bottom)
        {
            top = Img("LetterboxTop", canvasRect, Color.black).rectTransform;
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = Vector2.zero;
            bottom = Img("LetterboxBottom", canvasRect, Color.black).rectTransform;
            bottom.anchorMin = new Vector2(0f, 0f);
            bottom.anchorMax = new Vector2(1f, 0f);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = Vector2.zero;
            bottom.sizeDelta = Vector2.zero;
        }

        // Перекрестие вместо системного курсора. Оружие и взлом необязательны (дома героя без них)
        internal static Crosshair BuildCrosshair(RectTransform canvasRect, Weapon weapon, HackingController hacking)
        {
            Image image = MainMenuSceneBuilder.Create<Image>("Crosshair", canvasRect);
            image.sprite = SpriteFactory.Crosshair();
            image.raycastTarget = false;
            Anchor(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(45f, 45f));
            Crosshair crosshair = image.gameObject.AddComponent<Crosshair>();
            Configure(crosshair, so =>
            {
                P(so, "_canvasRect").objectReferenceValue = canvasRect;
                P(so, "_image").objectReferenceValue = image;
                P(so, "_weapon").objectReferenceValue = weapon;
                P(so, "_hacking").objectReferenceValue = hacking;
            });
            return crosshair;
        }

        internal static void BuildHint(RectTransform canvasRect, TMP_FontAsset mono, string text)
        {
            TextMeshProUGUI hint = Text("Hint", canvasRect, mono, text, 18f, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.BottomRight);
            Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(700f, 30f));
        }

        // Затемнение, вспышки и титры поверх чёрного
        internal static ScreenFader BuildFader(RectTransform canvasRect, TMP_FontAsset mono, Color titleColor)
        {
            RectTransform fader = MainMenuSceneBuilder.CreateRect("Fader", canvasRect);
            MainMenuSceneBuilder.Stretch(fader);
            Image black = Img("Black", fader, Color.black);
            MainMenuSceneBuilder.Stretch(black.rectTransform);
            Image flash = Img("Flash", fader, new Color(1f, 0f, 0f, 0f));
            MainMenuSceneBuilder.Stretch(flash.rectTransform);
            TextMeshProUGUI title = Text("Title", fader, mono, string.Empty, 64f, titleColor, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1600f, 90f));
            TextMeshProUGUI subtitle = Text("Subtitle", fader, mono, string.Empty, 28f, new Color(0.85f, 0.85f, 0.9f), TextAlignmentOptions.Center);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(1600f, 50f));
            ScreenFader screenFader = fader.gameObject.AddComponent<ScreenFader>();
            Configure(screenFader, so =>
            {
                P(so, "_black").objectReferenceValue = black;
                P(so, "_flash").objectReferenceValue = flash;
                P(so, "_title").objectReferenceValue = title;
                P(so, "_subtitle").objectReferenceValue = subtitle;
            });
            return screenFader;
        }

        internal static Image Img(string name, RectTransform parent, Color color)
        {
            Image img = MainMenuSceneBuilder.Create<Image>(name, parent);
            img.sprite = s_white;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void MakeFilled(Image img)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        // Окно-ошибка над игроком: «нельзя стрелять во время взлома»
        public static Image BuildWarningWindow(RectTransform canvasRect, TMP_FontAsset mono, out CanvasGroup group, out TextMeshProUGUI body)
        {
            Color red = new Color(1f, 0.25f, 0.3f);
            Image panel = Img("Warning", canvasRect, new Color(0.06f, 0.01f, 0.03f, 0.92f));
            Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(760f, 140f));
            group = Group(panel.gameObject, 0f);

            Image bar = Img("Bar", panel.rectTransform, red);
            Anchor(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(760f, 6f));
            Image bottom = Img("BarBottom", panel.rectTransform, red);
            Anchor(bottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(760f, 2f));

            TextMeshProUGUI header = Text("Header", panel.rectTransform, mono, "ОШИБКА // ВЗЛОМ ПРЕРВАН", 22f, red, TextAlignmentOptions.TopLeft);
            Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(712f, 30f));
            body = Text("Body", panel.rectTransform, mono, "НЕЛЬЗЯ СТРЕЛЯТЬ ВО ВРЕМЯ ВЗЛОМА", 36f, Color.white, TextAlignmentOptions.Center);
            Anchor(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(712f, 60f));
            return panel;
        }

        internal static TextMeshProUGUI Text(string name, RectTransform parent, TMP_FontAsset font, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = MainMenuSceneBuilder.CreateText(name, parent, font, value, size, color);
            text.alignment = alignment;
            return text;
        }

        internal static CanvasGroup Group(GameObject go, float alpha)
        {
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        internal static RectTransform Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // ---------------------------------------------------------------- Общее

        // Общие материалы и белый спрайт — вызывать сразу после EditorSceneManager.NewScene
        internal static void LoadCommonAssets()
        {
            s_lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            s_unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            s_white = SpriteFactory.White();
        }

        internal static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.01f, 0.04f);
            go.AddComponent<AudioListener>();
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            return cam;
        }

        internal static void CreateVolume(string profileName)
        {
            string path = $"{SettingsDir}/{profileName}.asset";
            AssetDatabase.DeleteAsset(path);
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.7f);
            profile.Add<Vignette>(true).intensity.Override(0.38f);
            profile.Add<ChromaticAberration>(true).intensity.Override(0.12f);
            profile.Add<FilmGrain>(true).intensity.Override(0.3f);

            foreach (VolumeComponent component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);

            var go = new GameObject("Global Volume");
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        internal static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Vector2 position, int order, bool lit = true, float angle = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = lit ? s_lit : s_unlit;
            sr.sortingOrder = order;
            return sr;
        }

        internal static Light2D AddLight(Transform parent, string name, Vector2 worldPosition, Color color, float radius, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = worldPosition;
            Light2D light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            light.pointLightInnerRadius = radius * 0.15f;
            light.falloffIntensity = 0.6f;
            return light;
        }

        internal static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(WallLayer).stringValue = "Wall";
            layers.GetArrayElementAtIndex(GlassLayer).stringValue = "Glass";
            layers.GetArrayElementAtIndex(PlayerLayer).stringValue = "Player";
            layers.GetArrayElementAtIndex(EnemyLayer).stringValue = "Enemy";
            layers.GetArrayElementAtIndex(DoorLayer).stringValue = "Door";
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        // Новая сцена встаёт в Build Settings сразу после сцены afterScene
        internal static void AddSceneToBuildSettings(string path, string afterScene = "MainMenu")
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                int afterIndex = scenes.FindIndex(s => s.path.EndsWith($"/{afterScene}.unity"));
                scenes.Insert(afterIndex + 1, new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        internal static void Configure(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static SerializedProperty P(SerializedObject so, string name)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property == null)
            {
                throw new ArgumentException($"Поле {name} не найдено в {so.targetObject.GetType().Name}");
            }

            return property;
        }

        internal static void SetArray(SerializedObject so, string name, params Object[] values)
        {
            SerializedProperty property = P(so, name);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
