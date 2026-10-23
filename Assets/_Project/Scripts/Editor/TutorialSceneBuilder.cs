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
        private const float WallThickness = 0.625f;
        private const float DoorLength = 3.2f;

        private const int WallLayer = 6;
        private const int GlassLayer = 7;
        private const int PlayerLayer = 8;
        private const int EnemyLayer = 9;

        private static readonly Color Cyan = new Color(0.3f, 1f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.8f);
        private static readonly Color Yellow = new Color(1f, 0.85f, 0.3f);

        private static Material s_lit;
        private static Material s_unlit;
        private static Sprite s_white;

        [MenuItem("N89/Build Tutorial Scene")]
        public static void Build()
        {
            EnsureLayers();
            Physics2D.gravity = Vector2.zero;
            MainMenuSceneBuilder.EnsureFolder(PrefabsDir);
            MainMenuSceneBuilder.EnsureFolder(QuickhacksDir);
            MainMenuSceneBuilder.EnsureFolder(SettingsDir);

            s_lit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
            s_unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            s_white = SpriteFactory.White();

            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");
            TMP_FontAsset audiowide = MainMenuSceneBuilder.GetOrCreateFontAsset("Audiowide-Regular");

            // --- Данные взломов ---
            QuickhackData unlock = CreateQuickhack("Unlock", "ВЗЛОМ ЗАМКА", 1, 0.6f, QuickhackEffect.Unlock);
            QuickhackData shortCircuit = CreateQuickhack("ShortCircuit", "КОРОТКОЕ ЗАМЫКАНИЕ", 2, 1f, QuickhackEffect.ShortCircuit);

            // --- Префабы ---
            Material particleMat = MainMenuSceneBuilder.GetOrCreateMaterial("ParticleUnlit", new Material(s_unlit));
            GameObject sparks = BurstPrefab("FX_Sparks", Yellow, Color.white, 10, 3f, 8f, 0.1f, 0.3f, 0.06f, 0.12f, particleMat);
            GameObject explosion = BurstPrefab("FX_Explosion", new Color(1f, 0.55f, 0.15f), Magenta, 40, 2f, 9f, 0.3f, 0.7f, 0.08f, 0.2f, particleMat);
            GameObject electric = BurstPrefab("FX_ShortCircuit", Cyan, Color.white, 40, 1f, 7f, 0.3f, 0.9f, 0.05f, 0.12f, particleMat);
            GameObject blood = BurstPrefab("FX_Blood", new Color(0.8f, 0.05f, 0.1f), new Color(0.4f, 0f, 0.05f), 30, 1f, 6f, 0.3f, 0.8f, 0.08f, 0.16f, particleMat);
            GameObject muzzle = BurstPrefab("FX_Muzzle", new Color(1f, 0.9f, 0.5f), Color.white, 6, 4f, 10f, 0.04f, 0.09f, 0.06f, 0.1f, particleMat);
            GameObject remains = RemainsPrefab(SpriteFactory.Scorch());

            Bullet playerBullet = BulletPrefab("Bullet_Player", SpriteFactory.Bullet("bullet_player", new Color(1f, 0.95f, 0.6f)), 32f, sparks);
            Bullet turretBullet = BulletPrefab("Bullet_Turret", SpriteFactory.Bullet("bullet_turret", new Color(1f, 0.3f, 0.35f)), 26f, sparks);

            GameObject playerPrefab = PlayerPrefab(SpriteFactory.Player(), playerBullet, muzzle, blood);
            GameObject dronePrefab = DronePrefab(SpriteFactory.Drone(), explosion, remains);
            GameObject targetPrefab = TargetPrefab(SpriteFactory.Target(), explosion, remains);
            GameObject turretPrefab = TurretPrefab(SpriteFactory.TurretBase(), SpriteFactory.TurretHead(), turretBullet, muzzle);

            // --- Сцена ---
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera cam = CreateCamera();
            CreateVolume();

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
            AddLight(lights, "Corridor", new Vector2(17.5f, 0f), new Color(0.5f, 0.5f, 0.7f), 5f, 0.5f);

            var alarmLights = new List<Light2D>
            {
                AddLight(lights, "Alarm_Corridor", new Vector2(17.5f, 0f), new Color(1f, 0.1f, 0.15f), 8f, 2f),
                AddLight(lights, "Alarm_East", new Vector2(10.8f, 0f), new Color(1f, 0.1f, 0.15f), 7f, 2f),
                AddLight(lights, "Alarm_Center", new Vector2(0f, 0f), new Color(1f, 0.1f, 0.15f), 12f, 1.2f),
            };
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

            Transform lab = new GameObject("Lab").transform;
            BuildLab(lab, out Door hackDoor, out Door exitGate, unlock);

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

            Turret[] turrets =
            {
                Spawn(turretPrefab, gameplay, new Vector2(21.3f, 0.9f)).GetComponent<Turret>(),
                Spawn(turretPrefab, gameplay, new Vector2(21.3f, -0.9f)).GetComponent<Turret>(),
            };
            foreach (Turret turret in turrets)
            {
                turret.transform.Find("Head").rotation = Quaternion.Euler(0f, 0f, 180f);
            }

            GameObject escapeGo = new GameObject("EscapeZone");
            escapeGo.transform.SetParent(gameplay);
            escapeGo.transform.position = new Vector2(15.6f, 0f);
            BoxCollider2D escapeCollider = escapeGo.AddComponent<BoxCollider2D>();
            escapeCollider.isTrigger = true;
            escapeCollider.size = new Vector2(1.4f, DoorLength);
            TriggerZone escapeZone = escapeGo.AddComponent<TriggerZone>();

            // --- Камера и игрок ---
            CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
            Configure(follow, so => P(so, "_target").objectReferenceValue = player.transform);
            PlayerController playerController = player.GetComponent<PlayerController>();
            Configure(playerController, so => P(so, "_cameraFollow").objectReferenceValue = follow);
            cam.transform.position = new Vector3(0f, -9.6f, -10f);

            // --- UI ---
            HudRefs hud = BuildHud(player.GetComponent<HackingController>(), mono, audiowide);

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
                P(so, "_fader").objectReferenceValue = hud.Fader;
                SetArray(so, "_moveMarkers", markers);
                SetArray(so, "_targets", targets);
                SetArray(so, "_drones", drones);
                P(so, "_hackDoor").objectReferenceValue = hackDoor;
                P(so, "_glassDrone").objectReferenceValue = glassDrone;
                P(so, "_exitGate").objectReferenceValue = exitGate;
                P(so, "_escapeZone").objectReferenceValue = escapeZone;
                SetArray(so, "_turrets", turrets);
                SetArray(so, "_alarmLights", alarmLights.ToArray());
                P(so, "_globalLight").objectReferenceValue = globalLight;
            });

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Туториал собран: {ScenePath}");
        }

        // ---------------------------------------------------------------- Лаборатория

        private static void BuildLab(Transform lab, out Door hackDoor, out Door exitGate, QuickhackData unlock)
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
            float half = DoorLength * 0.5f;
            CreateWall(lab, "OuterWall", new Vector2(eastX, -LabRadius * 0.5f - 0.25f), new Vector2(eastX, -half), wall, WallLayer, 5);
            CreateWall(lab, "OuterWall", new Vector2(eastX, half), new Vector2(eastX, LabRadius * 0.5f + 0.25f), wall, WallLayer, 5);

            // Восточная комната (за взламываемой дверью)
            CreateWall(lab, "InnerWall", new Vector2(8f, half), new Vector2(8f, 10.9f), wall, WallLayer, 5);
            CreateWall(lab, "InnerWall", new Vector2(8f, -10.9f), new Vector2(8f, -half), wall, WallLayer, 5);
            Door lockedDoor = CreateDoor(lab, "HackDoor", new Vector2(8f, 0f), door, new Vector2(7.4f, 2.2f));
            HackableDevice doorHack = lockedDoor.gameObject.AddComponent<HackableDevice>();
            Configure(doorHack, so =>
            {
                P(so, "_deviceName").stringValue = "ДВЕРЬ // ЭЛЕКТРОЗАМОК";
                SetArray(so, "_quickhacks", unlock);
                P(so, "_collider").objectReferenceValue = lockedDoor.GetComponent<Collider2D>();
                SetArray(so, "_highlightRenderers", lockedDoor.transform.Find("Panel").GetComponent<SpriteRenderer>());
            });
            hackDoor = lockedDoor;

            // Западная комната за стеклом
            CreateWall(lab, "InnerWall", new Vector2(-8f, 3f), new Vector2(-8f, 10.9f), wall, WallLayer, 5);
            CreateWall(lab, "InnerWall", new Vector2(-8f, -10.9f), new Vector2(-8f, -3f), wall, WallLayer, 5);
            CreateWall(lab, "Glass", new Vector2(-8f, -3f), new Vector2(-8f, 3f), glass, GlassLayer, 6, false);

            // Шлюз и коридор за ним
            exitGate = CreateDoor(lab, "ExitGate", new Vector2(eastX, 0f), door, new Vector2(eastX - 0.6f, 2.2f));

            float corridorEnd = 22.4f;
            float wallY = half + WallThickness * 0.5f;
            CreateWall(lab, "CorridorWall", new Vector2(eastX, wallY), new Vector2(corridorEnd, wallY), wall, WallLayer, 5);
            CreateWall(lab, "CorridorWall", new Vector2(eastX, -wallY), new Vector2(corridorEnd, -wallY), wall, WallLayer, 5);
            CreateWall(lab, "CorridorWall", new Vector2(corridorEnd, -wallY - 0.3f), new Vector2(corridorEnd, wallY + 0.3f), wall, WallLayer, 5);

            SpriteRenderer corridorFloor = CreateSprite("CorridorFloor", lab, SpriteFactory.FloorTileSprite(), new Vector2((eastX + corridorEnd) * 0.5f, 0f), -100);
            corridorFloor.drawMode = SpriteDrawMode.Tiled;
            corridorFloor.size = new Vector2(corridorEnd - eastX, DoorLength);
        }

        private static Vector2 HexVertex(int k)
        {
            float angle = (30f + 60f * k) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * LabRadius;
        }

        private static void CreateWall(Transform parent, string name, Vector2 a, Vector2 b, Sprite sprite, int layer, int order, bool lit = true)
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

        private static Door CreateDoor(Transform parent, string name, Vector2 position, Sprite panelSprite, Vector2 lampPosition)
        {
            var root = new GameObject(name) { layer = WallLayer };
            root.transform.SetParent(parent);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(DoorLength, WallThickness);

            SpriteRenderer panel = CreateSprite("Panel", root.transform, panelSprite, Vector2.zero, 4);
            panel.drawMode = SpriteDrawMode.Tiled;
            panel.size = new Vector2(DoorLength, WallThickness);

            SpriteRenderer lamp = CreateSprite(name + "_Lamp", parent, s_white, lampPosition, 12, false);
            Light2D lampLight = AddLight(lamp.transform, "Light", lampPosition, Color.red, 2.5f, 1.2f);

            Door door = root.AddComponent<Door>();
            Configure(door, so =>
            {
                P(so, "_collider").objectReferenceValue = col;
                P(so, "_panel").objectReferenceValue = panel.transform;
                P(so, "_openOffset").vector2Value = new Vector2(DoorLength, 0f);
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

        private static GameObject PlayerPrefab(Sprite sprite, Bullet bullet, GameObject muzzleFx, GameObject deathFx)
        {
            var root = new GameObject("Player") { layer = PlayerLayer };
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            root.AddComponent<CircleCollider2D>().radius = 0.32f;

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
            });

            Weapon weapon = root.AddComponent<Weapon>();
            Configure(weapon, so =>
            {
                P(so, "_bulletPrefab").objectReferenceValue = bullet;
                P(so, "_muzzle").objectReferenceValue = muzzle;
                P(so, "_cooldown").floatValue = 0.16f;
                P(so, "_hitMask").intValue = (1 << WallLayer) | (1 << GlassLayer) | (1 << EnemyLayer);
                P(so, "_muzzleFxPrefab").objectReferenceValue = muzzleFx;
            });

            RamPool ram = root.AddComponent<RamPool>();
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

            return SavePrefab(root, "Player");
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

        private static GameObject RemainsPrefab(Sprite sprite)
        {
            var root = new GameObject("Remains_Scorch");
            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = s_lit;
            sr.sortingOrder = -70;
            return SavePrefab(root, "Remains_Scorch");
        }

        private static GameObject BurstPrefab(string name, Color colorA, Color colorB, int count, float speedMin, float speedMax,
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

        private static GameObject SavePrefab(GameObject root, string name)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject Spawn(GameObject prefab, Transform parent, Vector2 position)
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

        private static QuickhackData CreateQuickhack(string fileName, string displayName, int cost, float upload, QuickhackEffect effect)
        {
            string path = $"{QuickhacksDir}/{fileName}.asset";
            var hack = AssetDatabase.LoadAssetAtPath<QuickhackData>(path);
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

        // ---------------------------------------------------------------- HUD

        private struct HudRefs
        {
            public SubtitleBox Subtitles;
            public ObjectivePanel Objective;
            public HackingHud HackingHud;
            public ScreenFader Fader;
        }

        private static HudRefs BuildHud(HackingController hacking, TMP_FontAsset mono, TMP_FontAsset audiowide)
        {
            var go = new GameObject("HUD", typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRect = (RectTransform)go.transform;

            // Режим сканирования
            Image scan = Img("ScanOverlay", canvasRect, new Color(0.15f, 0.9f, 1f, 0.12f));
            MainMenuSceneBuilder.Stretch(scan.rectTransform);
            CanvasGroup scanGroup = Group(scan.gameObject, 0f);
            TextMeshProUGUI scanLabel = Text("Label", scan.rectTransform, mono, "РЕЖИМ СКАНИРОВАНИЯ", 26f, Cyan, TextAlignmentOptions.Center);
            Anchor(scanLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 40f));

            // Задача
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

            // RAM
            RectTransform ram = Anchor(MainMenuSceneBuilder.CreateRect("RAM", canvasRect), Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(320f, 80f));
            CanvasGroup ramGroup = Group(ram.gameObject, 0f);
            TextMeshProUGUI ramLabel = Text("Label", ram, audiowide, "RAM 4/4", 26f, Cyan, TextAlignmentOptions.BottomLeft);
            Anchor(ramLabel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 30f), new Vector2(320f, 40f));
            var slots = new Image[4];
            for (int i = 0; i < slots.Length; i++)
            {
                Image bg = Img($"Slot{i}", ram, new Color(0.05f, 0.15f, 0.18f, 0.9f));
                Anchor(bg.rectTransform, Vector2.zero, Vector2.zero, new Vector2(i * 62f, 0f), new Vector2(54f, 22f));
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

            HackingHud hackingHud = go.AddComponent<HackingHud>();
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
            });

            // Субтитры голоса
            Image subtitles = Img("Subtitles", canvasRect, new Color(0f, 0f, 0f, 0.7f));
            Anchor(subtitles.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1300f, 150f));
            CanvasGroup subtitleGroup = Group(subtitles.gameObject, 0f);
            TextMeshProUGUI speaker = Text("Speaker", subtitles.rectTransform, mono, "ОПЕРАТОР", 22f, Magenta, TextAlignmentOptions.TopLeft);
            Anchor(speaker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -14f), new Vector2(600f, 30f));
            TextMeshProUGUI line = Text("Line", subtitles.rectTransform, mono, string.Empty, 32f, Color.white, TextAlignmentOptions.TopLeft);
            Anchor(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -50f), new Vector2(1240f, 90f));
            line.textWrappingMode = TextWrappingModes.Normal;
            SubtitleBox subtitleBox = subtitles.gameObject.AddComponent<SubtitleBox>();
            Configure(subtitleBox, so =>
            {
                P(so, "_group").objectReferenceValue = subtitleGroup;
                P(so, "_speakerLabel").objectReferenceValue = speaker;
                P(so, "_lineLabel").objectReferenceValue = line;
            });

            TextMeshProUGUI hint = Text("Hint", canvasRect, mono, "ESC — меню    ENTER — пропустить реплику", 18f, new Color(1f, 1f, 1f, 0.4f), TextAlignmentOptions.BottomRight);
            Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(700f, 30f));

            // Затемнение и экран смерти — поверх всего
            RectTransform fader = MainMenuSceneBuilder.CreateRect("Fader", canvasRect);
            MainMenuSceneBuilder.Stretch(fader);
            Image black = Img("Black", fader, Color.black);
            MainMenuSceneBuilder.Stretch(black.rectTransform);
            Image flash = Img("Flash", fader, new Color(1f, 0f, 0f, 0f));
            MainMenuSceneBuilder.Stretch(flash.rectTransform);
            TextMeshProUGUI title = Text("Title", fader, mono, string.Empty, 64f, new Color(1f, 0.2f, 0.3f), TextAlignmentOptions.Center);
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

            return new HudRefs { Subtitles = subtitleBox, Objective = objectivePanel, HackingHud = hackingHud, Fader = screenFader };
        }

        private static Image Img(string name, RectTransform parent, Color color)
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

        private static TextMeshProUGUI Text(string name, RectTransform parent, TMP_FontAsset font, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = MainMenuSceneBuilder.CreateText(name, parent, font, value, size, color);
            text.alignment = alignment;
            return text;
        }

        private static CanvasGroup Group(GameObject go, float alpha)
        {
            CanvasGroup group = go.AddComponent<CanvasGroup>();
            group.alpha = alpha;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private static RectTransform Anchor(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // ---------------------------------------------------------------- Общее

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.01f, 0.04f);
            go.AddComponent<AudioListener>();
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            return cam;
        }

        private static void CreateVolume()
        {
            string path = SettingsDir + "/TutorialVolume.asset";
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

        private static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Vector2 position, int order, bool lit = true, float angle = 0f)
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

        private static Light2D AddLight(Transform parent, string name, Vector2 worldPosition, Color color, float radius, float intensity)
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

        private static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(WallLayer).stringValue = "Wall";
            layers.GetArrayElementAtIndex(GlassLayer).stringValue = "Glass";
            layers.GetArrayElementAtIndex(PlayerLayer).stringValue = "Player";
            layers.GetArrayElementAtIndex(EnemyLayer).stringValue = "Enemy";
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                int menuIndex = scenes.FindIndex(s => s.path.EndsWith("MainMenu.unity"));
                scenes.Insert(menuIndex + 1, new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void Configure(Object target, Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty P(SerializedObject so, string name)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property == null)
            {
                throw new ArgumentException($"Поле {name} не найдено в {so.targetObject.GetType().Name}");
            }

            return property;
        }

        private static void SetArray(SerializedObject so, string name, params Object[] values)
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
