using N89.Interlude;
using N89.Player;
using N89.UI;
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
    // Собирает интерлюдию перед миссией 1: квартира героя (гостиная с кухней, спальня, ванная).
    // Герой спит на диване, просыпается, дальше игрок ходит по квартире.
    // Запуск: N89 > Build Interlude 01 Scene. Пересоздаёт Interlude01.unity, префаб Hero и спрайты квартиры.
    public static class Interlude01SceneBuilder
    {
        public const string SceneName = "Interlude01";
        private const string ScenePath = "Assets/_Project/Scenes/" + SceneName + ".unity";

        // Внутренние границы квартиры: x от -9 до 9, y от -5.5 до 5.5.
        // Слева гостиная с кухней, справа (за стеной x = 3) спальня сверху и ванная снизу.
        private const float Left = -9f;
        private const float Right = 9f;
        private const float Bottom = -5.5f;
        private const float Top = 5.5f;
        private const float Partition = 3f;
        private const float DoorWidth = 1.6f;

        private static readonly Color Cyan = new Color(0.3f, 1f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.75f);
        private static readonly Color CpYellow = new Color(1f, 0.85f, 0.2f);

        [MenuItem("N89/Build Interlude 01 Scene")]
        public static void Build()
        {
            EnsureLayers();
            Physics2D.gravity = Vector2.zero;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadCommonAssets();
            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");

            GameObject heroPrefab = HeroPrefab(SpriteFactory.Hero());

            // --- Сцена ---
            Camera cam = CreateCamera();
            cam.orthographicSize = 5f;
            CreateVolume("Interlude01Volume");

            Transform lights = new GameObject("Lights").transform;
            Light2D globalLight = new GameObject("Global Light").AddComponent<Light2D>();
            globalLight.transform.SetParent(lights);
            globalLight.lightType = Light2D.LightType.Global;
            globalLight.intensity = 0.26f;
            globalLight.color = new Color(0.45f, 0.42f, 0.85f);

            // Неон города за окном, голоэкран, кухня, витрина арсенала, терминал, лампа в ванной
            Pulsing(AddLight(lights, "Window_Magenta", new Vector2(-7.2f, 4.6f), Magenta, 6f, 1f), 0.8f, 0.7f, 1.1f);
            AddLight(lights, "Window_Cyan", new Vector2(-4f, 4.8f), Cyan, 5f, 0.6f);
            AddLight(lights, "Couch_Neon", new Vector2(-5f, 3.5f), Cyan, 2.6f, 0.8f);
            Pulsing(AddLight(lights, "TV", new Vector2(-5f, -4.2f), new Color(0.75f, 0.4f, 1f), 5.5f, 1f), 11f, 0.6f, 1.2f);
            AddLight(lights, "Kitchen", new Vector2(0.6f, 4.2f), CpYellow, 3.2f, 0.8f);
            AddLight(lights, "Arsenal", new Vector2(7.2f, 2.75f), CpYellow, 3.8f, 1.1f);
            AddLight(lights, "Terminal", new Vector2(4.4f, 4.4f), Cyan, 2.8f, 1f);
            Pulsing(AddLight(lights, "Bathroom", new Vector2(6f, -2.6f), new Color(0.6f, 0.95f, 1f), 4.5f, 0.75f), 23f, 0.6f, 0.85f);
            AddLight(lights, "FrontDoor", new Vector2(1f, -5f), Cyan, 1.6f, 0.9f);

            Transform apartment = new GameObject("Apartment").transform;
            BuildRooms(apartment);
            BuildFurniture(apartment);

            // --- Герой и точки катсцены ---
            Transform gameplay = new GameObject("Gameplay").transform;
            Transform sleepPoint = Point(gameplay, "SleepPoint", new Vector2(-5.15f, 4.2f), 0f);
            Transform sitPoint = Point(gameplay, "SitPoint", new Vector2(-5.6f, 4.05f), -90f);
            Transform standPoint = Point(gameplay, "StandPoint", new Vector2(-6.3f, 2.9f), -90f);

            GameObject hero = Spawn(heroPrefab, gameplay, standPoint.position);
            hero.transform.Find("Body").rotation = standPoint.rotation;

            // Камера как в туториале: сдвиг к курсору, Shift — осмотреться
            CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
            Configure(follow, so =>
            {
                P(so, "_target").objectReferenceValue = hero.transform;
                P(so, "_maxLookAhead").floatValue = 3f;
            });
            Configure(hero.GetComponent<PlayerController>(), so => P(so, "_cameraFollow").objectReferenceValue = follow);
            cam.transform.position = new Vector3(sleepPoint.position.x, sleepPoint.position.y, -10f);

            // --- UI ---
            RectTransform canvasRect = BuildCanvas();
            ObjectivePanel objective = BuildObjective(canvasRect, mono);
            BuildLetterbox(canvasRect, out RectTransform letterboxTop, out RectTransform letterboxBottom);
            Crosshair crosshair = BuildCrosshair(canvasRect, null, null);
            BuildHint(canvasRect, mono, "SHIFT — осмотреться    ESC — меню");
            ScreenFader fader = BuildFader(canvasRect, mono, new Color(1f, 0.85f, 0.97f));

            // --- Режиссёр ---
            InterludeDirector director = new GameObject("InterludeDirector").AddComponent<InterludeDirector>();
            Configure(director, so =>
            {
                P(so, "_hero").objectReferenceValue = hero.GetComponent<PlayerController>();
                P(so, "_heroBody").objectReferenceValue = hero.transform.Find("Body").GetComponent<SpriteRenderer>();
                P(so, "_cameraFollow").objectReferenceValue = follow;
                P(so, "_sleepingSprite").objectReferenceValue = SpriteFactory.HeroSleeping();
                P(so, "_sittingSprite").objectReferenceValue = SpriteFactory.HeroSitting();
                P(so, "_sleepPoint").objectReferenceValue = sleepPoint;
                P(so, "_sitPoint").objectReferenceValue = sitPoint;
                P(so, "_standPoint").objectReferenceValue = standPoint;
                P(so, "_fader").objectReferenceValue = fader;
                P(so, "_objective").objectReferenceValue = objective;
                P(so, "_crosshair").objectReferenceValue = crosshair.gameObject;
                P(so, "_letterboxTop").objectReferenceValue = letterboxTop;
                P(so, "_letterboxBottom").objectReferenceValue = letterboxBottom;
            });

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath, "Tutorial");
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Интерлюдия 1 собрана: {ScenePath}");
        }

        // ---------------------------------------------------------------- Комнаты

        private static void BuildRooms(Transform parent)
        {
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(parent);
            Sprite wall = SpriteFactory.HomeWall();
            float t = WallThickness * 0.5f;

            // Пол: ламинат в гостиной и спальне, плитка в ванной
            Sprite wood = SpriteFactory.ApartmentFloor();
            TiledSprite("Floor_Living", parent, wood, new Vector2(Left, Bottom), new Vector2(Partition, Top), -100);
            TiledSprite("Floor_Bedroom", parent, wood, new Vector2(Partition, 0f), new Vector2(Right, Top), -100);
            TiledSprite("Floor_Bathroom", parent, SpriteFactory.BathroomFloor(), new Vector2(Partition, Bottom), new Vector2(Right, 0f), -100);

            // Внешние стены. В южной — входная дверь
            float doorX = 1f;
            CreateWall(walls, "Wall_North", new Vector2(Left - t, Top), new Vector2(Right + t, Top), wall, WallLayer, 5);
            CreateWall(walls, "Wall_South_W", new Vector2(Left - t, Bottom), new Vector2(doorX - DoorWidth * 0.5f, Bottom), wall, WallLayer, 5);
            CreateWall(walls, "Wall_South_E", new Vector2(doorX + DoorWidth * 0.5f, Bottom), new Vector2(Right + t, Bottom), wall, WallLayer, 5);
            CreateWall(walls, "Wall_West", new Vector2(Left, Bottom - t), new Vector2(Left, Top + t), wall, WallLayer, 5);
            CreateWall(walls, "Wall_East", new Vector2(Right, Bottom - t), new Vector2(Right, Top + t), wall, WallLayer, 5);

            // Перегородка гостиной с проёмами в ванную (y -3.6..-2) и спальню (y 1.4..3); стена между спальней и ванной
            CreateWall(walls, "Partition_1", new Vector2(Partition, Bottom), new Vector2(Partition, -3.6f), wall, WallLayer, 5);
            CreateWall(walls, "Partition_2", new Vector2(Partition, -3.6f + DoorWidth), new Vector2(Partition, 1.4f), wall, WallLayer, 5);
            CreateWall(walls, "Partition_3", new Vector2(Partition, 1.4f + DoorWidth), new Vector2(Partition, Top), wall, WallLayer, 5);
            CreateWall(walls, "Wall_BedBath", new Vector2(Partition, 0f), new Vector2(Right, 0f), wall, WallLayer, 5);

            // Окно во всю стену над диваном (поверх стены, коллайдер у стены)
            SpriteRenderer window = CreateSprite("Window", walls, SpriteFactory.Window(), new Vector2(-6f, Top), 6, false);
            window.drawMode = SpriteDrawMode.Tiled;
            window.size = new Vector2(5.2f, WallThickness);

            // Входная дверь (пока закрыта)
            SpriteRenderer door = CreateSprite("FrontDoor", walls, SpriteFactory.HomeDoor(), new Vector2(doorX, Bottom), 6);
            door.drawMode = SpriteDrawMode.Tiled;
            door.size = new Vector2(DoorWidth, WallThickness);
            door.gameObject.layer = WallLayer;
            door.gameObject.AddComponent<BoxCollider2D>().size = door.size;
        }

        private static void BuildFurniture(Transform parent)
        {
            Transform furniture = new GameObject("Furniture").transform;
            furniture.SetParent(parent);

            // Гостиная
            CreateSprite("Rug", furniture, SpriteFactory.Rug(), new Vector2(-5f, 1.8f), -90);
            Prop("Couch", furniture, SpriteFactory.Couch(), new Vector2(-5f, 4.35f), new Vector2(3.4f, 1.3f));
            Prop("CoffeeTable", furniture, SpriteFactory.CoffeeTable(), new Vector2(-5f, 1.9f), new Vector2(1.6f, 1f));
            Prop("TV", furniture, SpriteFactory.Tv(), new Vector2(-5f, -4.85f), new Vector2(2.9f, 0.5f));
            Prop("Speaker", furniture, SpriteFactory.Speaker(), new Vector2(-8.3f, 4.75f), new Vector2(0.7f, 0.7f));
            Prop("Speaker", furniture, SpriteFactory.Speaker(), new Vector2(-1.9f, -4.9f), new Vector2(0.7f, 0.7f));
            Prop("Clutter", furniture, SpriteFactory.Clutter(), new Vector2(-8f, -2.8f), Vector2.zero);
            Prop("Clutter", furniture, SpriteFactory.Clutter(), new Vector2(-1.5f, 0.6f), Vector2.zero);
            Sprite crate = SpriteFactory.Crate();
            Prop("Crate", furniture, crate, new Vector2(-8.2f, -4.6f), new Vector2(0.85f, 0.85f));
            Prop("Crate", furniture, crate, new Vector2(-8.2f, -3.7f), new Vector2(0.85f, 0.85f));
            Prop("Crate", furniture, crate, new Vector2(-7.3f, -4.6f), new Vector2(0.85f, 0.85f));

            // Кухня
            Prop("Fridge", furniture, SpriteFactory.Fridge(), new Vector2(-2.3f, 4.75f), new Vector2(1.05f, 1.1f));
            Prop("KitchenCounter", furniture, SpriteFactory.KitchenCounter(), new Vector2(0.6f, 4.85f), new Vector2(4f, 1f));

            // Бывшая спальня: арсенал у восточной стены (пока без взаимодействия) и терминал
            Prop("Arsenal", furniture, SpriteFactory.Arsenal(), new Vector2(7.55f, 2.75f), new Vector2(2.2f, 3.45f));
            Prop("Terminal", furniture, SpriteFactory.Terminal(), new Vector2(4.4f, 4.6f), new Vector2(1.7f, 0.85f));
            Prop("Clutter", furniture, SpriteFactory.Clutter(), new Vector2(5f, 1f), Vector2.zero);

            // Неоновые вывески на стенах (белый спрайт, цвет — у рендерера)
            Sprite sign = SpriteFactory.NeonSign();
            NeonSign(furniture, sign, new Vector2(-8.55f, -0.5f), -90f, Magenta);
            NeonSign(furniture, sign, new Vector2(-0.6f, -5.05f), 0f, Cyan);
            NeonSign(furniture, sign, new Vector2(5.8f, 0.45f), 0f, CpYellow);

            // Ванная: раковина с зеркалом и унитаз у восточной стены, душ в углу
            Prop("SinkMirror", furniture, SpriteFactory.SinkMirror(), new Vector2(8.4f, -2.6f), new Vector2(1.2f, 0.8f), -90f);
            Prop("Toilet", furniture, SpriteFactory.Toilet(), new Vector2(8.25f, -1.2f), new Vector2(0.8f, 1f), -90f);
            Prop("Shower", furniture, SpriteFactory.Shower(), new Vector2(7.9f, -4.4f), Vector2.zero);
        }

        // Мебель: спрайт и, если задан размер, твёрдый коллайдер на слое стен
        private static void Prop(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 colliderSize, float angle = 0f)
        {
            SpriteRenderer sr = CreateSprite(name, parent, sprite, position, 3, true, angle);
            if (colliderSize == Vector2.zero)
            {
                sr.sortingOrder = -50;
                return;
            }

            sr.gameObject.layer = WallLayer;
            sr.gameObject.AddComponent<BoxCollider2D>().size = colliderSize;
        }

        private static void NeonSign(Transform parent, Sprite sprite, Vector2 position, float angle, Color color)
        {
            SpriteRenderer sr = CreateSprite("NeonSign", parent, sprite, position, 7, false, angle);
            sr.color = color;
            Pulsing(AddLight(sr.transform, "Glow", position, color, 2.4f, 0.9f), 2.5f, 0.75f, 1f);
        }

        private static void TiledSprite(string name, Transform parent, Sprite sprite, Vector2 min, Vector2 max, int order)
        {
            SpriteRenderer sr = CreateSprite(name, parent, sprite, (min + max) * 0.5f, order);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = max - min;
        }

        private static Transform Point(Transform parent, string name, Vector2 position, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            return go.transform;
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

        // ---------------------------------------------------------------- Префаб

        // Герой дома: без оружия и здоровья, но целится за мышью, как в миссиях
        private static GameObject HeroPrefab(Sprite sprite)
        {
            var root = new GameObject("Hero") { layer = PlayerLayer };
            Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            root.AddComponent<CircleCollider2D>().radius = 0.32f;

            SpriteRenderer body = CreateSprite("Body", root.transform, sprite, Vector2.zero, 10);
            body.gameObject.layer = PlayerLayer;
            AddLight(root.transform, "Light", Vector2.zero, new Color(0.7f, 0.75f, 1f), 2.5f, 0.35f);

            PlayerController controller = root.AddComponent<PlayerController>();
            Configure(controller, so =>
            {
                P(so, "_body").objectReferenceValue = body.transform;
                P(so, "_bodyRenderer").objectReferenceValue = body;
                P(so, "_moveSpeed").floatValue = 4.5f;
            });

            return SavePrefab(root, "Hero");
        }
    }
}
