using N89.Combat;
using N89.Implants;
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
using Button = UnityEngine.UI.Button;
using Graphic = UnityEngine.UI.Graphic;
using Image = UnityEngine.UI.Image;
using LayoutElement = UnityEngine.UI.LayoutElement;
using Outline = UnityEngine.UI.Outline;
using VerticalLayoutGroup = UnityEngine.UI.VerticalLayoutGroup;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Собирает интерлюдию перед миссией 1: квартира героя (гостиная с кухней, спальня, ванная).
    // Герой спит на диване, просыпается, дальше игрок ходит по квартире.
    // Запуск: N89 > Build Interlude 01 Scene. Пересоздаёт Interlude01.unity, префаб Hero и спрайты квартиры.
    public static class Interlude01SceneBuilder
    {
        public const string SceneName = "Interlude01";

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
            BuildApartment("Interlude01Volume");
            SaveScene(SceneName, "Tutorial");
        }

        // Квартира целиком, герой, UI и режиссёр со сценарием интерлюдии 1. Другие интерлюдии
        // донастраивают режиссёра поверх и сохраняют сцену под своим именем (SaveScene)
        internal static InterludeDirector BuildApartment(string volumeName)
        {
            EnsureLayers();
            Physics2D.gravity = Vector2.zero;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadCommonAssets();
            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");

            GameObject heroPrefab = HeroPrefab(SpriteFactory.Hero());

            // --- Сцена ---
            Camera cam = CreateCamera();
            cam.orthographicSize = 5f;
            CreateVolume(volumeName);

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
            Transform apartment = new GameObject("Apartment").transform;
            BuildRooms(apartment, out Door frontDoor);
            BuildFurniture(apartment);
            TriggerZone exitZone = BuildCorridor(apartment, lights);

            // --- Герой и точки катсцены ---
            Transform gameplay = new GameObject("Gameplay").transform;
            Transform sleepPoint = Point(gameplay, "SleepPoint", new Vector2(-5.15f, 4.2f), 0f);
            Transform sitPoint = Point(gameplay, "SitPoint", new Vector2(-5.6f, 4.05f), -90f);
            Transform standPoint = Point(gameplay, "StandPoint", new Vector2(-6.3f, 2.9f), -90f);

            // Брифинг: зона ванной и точка камеры (весь план квартиры — ванная справа, зал под панелью)
            TriggerZone bathroomZone = CreateZone(gameplay, "BathroomZone", new Vector2(6f, -2.75f), new Vector2(5.2f, 4.8f));
            Transform messageCamPoint = Point(gameplay, "MessageCamPoint", new Vector2(0.3f, -1.2f), 0f);
            // Площадка перед витриной арсенала
            TriggerZone arsenalZone = CreateZone(gameplay, "ArsenalZone", new Vector2(5.7f, 2.75f), new Vector2(1.4f, 3.2f));
            // Площадка у входной двери изнутри
            TriggerZone doorZone = CreateZone(gameplay, "DoorZone", new Vector2(1f, Bottom + 1f), new Vector2(2f, 1.4f));

            GameObject hero = Spawn(heroPrefab, gameplay, standPoint.position);
            hero.transform.Find("Body").rotation = standPoint.rotation;
            hero.transform.Find("Legs").rotation = standPoint.rotation;

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
            MessengerPanel messenger = BuildMessenger(canvasRect, mono);
            ArsenalPanel arsenal = BuildArsenal(canvasRect, mono, CreateWeapons(), CreateImplants());
            Crosshair crosshair = BuildCrosshair(canvasRect, null, null);
            BuildHint(canvasRect, mono, "WASD — ходьба    SHIFT — осмотреться    ESC — меню");
            ScreenFader fader = BuildFader(canvasRect, mono, new Color(1f, 0.85f, 0.97f));
            LoadingScreen loading = BuildLoadingScreen(canvasRect, mono);

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
                P(so, "_bathroomZone").objectReferenceValue = bathroomZone;
                P(so, "_messenger").objectReferenceValue = messenger;
                P(so, "_messageCamPoint").objectReferenceValue = messageCamPoint;
                P(so, "_arsenalZone").objectReferenceValue = arsenalZone;
                P(so, "_arsenal").objectReferenceValue = arsenal;
                P(so, "_frontDoor").objectReferenceValue = frontDoor;
                P(so, "_doorZone").objectReferenceValue = doorZone;
                P(so, "_exitZone").objectReferenceValue = exitZone;
                P(so, "_loading").objectReferenceValue = loading;
                P(so, "_letterboxTop").objectReferenceValue = letterboxTop;
                P(so, "_letterboxBottom").objectReferenceValue = letterboxBottom;
            });
            return director;
        }

        internal static void SaveScene(string sceneName, string afterScene)
        {
            string path = $"Assets/_Project/Scenes/{sceneName}.unity";
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
            AddSceneToBuildSettings(path, afterScene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Интерлюдия собрана: {path}");
        }

        // ---------------------------------------------------------------- Комнаты

        private static void BuildRooms(Transform parent, out Door frontDoor)
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

            // Входная дверь: закрыта (лампа красная), пока не выбрано снаряжение; панель уезжает вправо, под стену
            var doorRoot = new GameObject("FrontDoor") { layer = WallLayer };
            doorRoot.transform.SetParent(walls);
            doorRoot.transform.position = new Vector2(doorX, Bottom);
            BoxCollider2D doorCollider = doorRoot.AddComponent<BoxCollider2D>();
            doorCollider.size = new Vector2(DoorWidth, WallThickness);
            SpriteRenderer panel = CreateSprite("Panel", doorRoot.transform, SpriteFactory.HomeDoor(), Vector2.zero, 4);
            panel.drawMode = SpriteDrawMode.Tiled;
            panel.size = doorCollider.size;
            // Лампа справа от двери — слева висит неоновая вывеска
            Vector2 lampPosition = new Vector2(doorX + DoorWidth * 0.5f + 0.35f, Bottom + 0.45f);
            SpriteRenderer lamp = CreateSprite("FrontDoor_Lamp", walls, SpriteFactory.White(), lampPosition, 7, false);
            lamp.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            Light2D lampLight = AddLight(lamp.transform, "Light", lampPosition, Color.red, 1.8f, 0.9f);
            frontDoor = doorRoot.AddComponent<Door>();
            Configure(frontDoor, so =>
            {
                P(so, "_collider").objectReferenceValue = doorCollider;
                P(so, "_panel").objectReferenceValue = panel.transform;
                P(so, "_openOffset").vector2Value = new Vector2(DoorWidth, 0f);
                P(so, "_statusLamp").objectReferenceValue = lamp;
                P(so, "_statusLight").objectReferenceValue = lampLight;
            });
        }

        // Коридор мегаблока за входной дверью: тусклые мигающие лампы, мусор, в конце — выход
        private static TriggerZone BuildCorridor(Transform parent, Transform lights)
        {
            const float left = -0.6f, right = 2.6f, end = -15.5f;
            Transform corridor = new GameObject("Corridor").transform;
            corridor.SetParent(parent);
            Sprite wall = SpriteFactory.HomeWall();
            float t = WallThickness * 0.5f;

            SpriteRenderer floor = CreateSprite("Floor", corridor, SpriteFactory.ApartmentFloor(), new Vector2((left + right) * 0.5f, (Bottom + end) * 0.5f), -100);
            floor.drawMode = SpriteDrawMode.Tiled;
            floor.size = new Vector2(right - left, Bottom - end);
            floor.color = new Color(0.7f, 0.72f, 0.8f);

            CreateWall(corridor, "Wall_W", new Vector2(left, Bottom), new Vector2(left, end - t), wall, WallLayer, 5);
            CreateWall(corridor, "Wall_E", new Vector2(right, Bottom), new Vector2(right, end - t), wall, WallLayer, 5);
            CreateWall(corridor, "Wall_End", new Vector2(left - t, end), new Vector2(right + t, end), wall, WallLayer, 5);

            Prop("Clutter", corridor, SpriteFactory.Clutter(), new Vector2(-0.05f, -9.2f), Vector2.zero);
            Prop("Crate", corridor, SpriteFactory.Crate(), new Vector2(2.05f, -11.6f), new Vector2(0.85f, 0.85f));
            NeonSign(corridor, SpriteFactory.NeonSign(), new Vector2(1f, end + 0.45f), 0f, new Color(0.35f, 1f, 0.5f));

            Pulsing(AddLight(lights, "Corridor_1", new Vector2(1f, -8f), new Color(0.75f, 0.9f, 1f), 3.5f, 0.7f), 17f, 0.25f, 0.8f);
            AddLight(lights, "Corridor_2", new Vector2(1f, -12.5f), new Color(1f, 0.6f, 0.4f), 3.5f, 0.6f);

            return CreateZone(corridor, "ExitZone", new Vector2(1f, end + 1.1f), new Vector2(right - left, 1.4f));
        }

        // Экран загрузки миссии: чёрный фон, название, полоска прогресса
        private static LoadingScreen BuildLoadingScreen(RectTransform canvasRect, TMP_FontAsset mono)
        {
            Color yellow = new Color(0.98f, 0.86f, 0.38f);
            Color cyan = new Color(0.3f, 1f, 1f);
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 bottom = new Vector2(0.5f, 0f);

            Image root = Img("Loading", canvasRect, Color.black);
            MainMenuSceneBuilder.Stretch(root.rectTransform);
            CanvasGroup group = Group(root.gameObject, 0f);

            TextMeshProUGUI title = Text("Title", root.rectTransform, mono, string.Empty, 72f, yellow, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, center, center, new Vector2(0f, 70f), new Vector2(1400f, 100f));
            Anchor(Img("Line", root.rectTransform, new Color(1f, 0.33f, 0.33f)).rectTransform, center, center, new Vector2(0f, 10f), new Vector2(700f, 2f));
            TextMeshProUGUI subtitle = Text("Subtitle", root.rectTransform, mono, string.Empty, 28f, Color.white, TextAlignmentOptions.Center);
            Anchor(subtitle.rectTransform, center, center, new Vector2(0f, -34f), new Vector2(1400f, 44f));

            TextMeshProUGUI percent = Text("Percent", root.rectTransform, mono, string.Empty, 22f, cyan, TextAlignmentOptions.Center);
            Anchor(percent.rectTransform, bottom, bottom, new Vector2(0f, 150f), new Vector2(600f, 30f));
            Image bar = Img("Bar", root.rectTransform, new Color(0.3f, 1f, 1f, 0.15f));
            Anchor(bar.rectTransform, bottom, bottom, new Vector2(0f, 120f), new Vector2(900f, 8f));
            Image fill = Img("Fill", bar.rectTransform, cyan);
            MainMenuSceneBuilder.Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;

            LoadingScreen loading = root.gameObject.AddComponent<LoadingScreen>();
            Configure(loading, so =>
            {
                P(so, "_group").objectReferenceValue = group;
                P(so, "_title").objectReferenceValue = title;
                P(so, "_subtitle").objectReferenceValue = subtitle;
                P(so, "_fill").objectReferenceValue = fill;
                P(so, "_percent").objectReferenceValue = percent;
            });
            return loading;
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

        internal static Transform Point(Transform parent, string name, Vector2 position, float angle)
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

        // ---------------------------------------------------------------- Мессенджер

        // Панель сообщений как журнал в Cyberpunk 2077: тёмно-красный фон, красные заголовки,
        // контакт слева, справа лента с жёлтыми сообщениями и бирюзовым вложением. Занимает левые 60% экрана.
        private static MessengerPanel BuildMessenger(RectTransform canvasRect, TMP_FontAsset mono)
        {
            Color red = new Color(1f, 0.33f, 0.33f);
            Color redDim = new Color(0.65f, 0.2f, 0.22f);
            Color yellow = new Color(0.98f, 0.86f, 0.38f);
            Color cyan = new Color(0.4f, 1f, 0.95f);
            const float width = 1150f;

            RectTransform root = MainMenuSceneBuilder.CreateRect("Messenger", canvasRect);
            MainMenuSceneBuilder.Stretch(root);
            CanvasGroup group = Group(root.gameObject, 0f);

            Image panel = Img("Panel", root, new Color(0.07f, 0.015f, 0.03f, 0.95f));
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.sizeDelta = new Vector2(width, 0f);
            panelRect.anchoredPosition = new Vector2(-width, 0f);

            Image edge = Img("Edge", panelRect, red);
            edge.rectTransform.anchorMin = new Vector2(1f, 0f);
            edge.rectTransform.anchorMax = new Vector2(1f, 1f);
            edge.rectTransform.pivot = new Vector2(1f, 0.5f);
            edge.rectTransform.sizeDelta = new Vector2(3f, 0f);

            // Шапка
            TextMeshProUGUI header = Text("Header", panelRect, mono, "СООБЩЕНИЯ", 34f, red, TextAlignmentOptions.BottomLeft);
            Anchor(header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -50f), new Vector2(500f, 50f));
            TextMeshProUGUI protocol = Text("Protocol", panelRect, mono, "NEURO NETPROTOCOL 3889_M  //  КОРПОРАЦИЯ Е", 14f, redDim, TextAlignmentOptions.BottomRight);
            Anchor(protocol.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -60f), new Vector2(600f, 24f));
            Anchor(Img("HeaderLine", panelRect, red).rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -110f), new Vector2(width - 100f, 2f));

            // Контакт слева: выбранный (красная заливка) и тема переписки под ним
            Image contact = Img("Contact", panelRect, new Color(0.55f, 0.09f, 0.12f, 1f));
            Anchor(contact.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -150f), new Vector2(370f, 72f));
            contact.gameObject.AddComponent<Outline>().effectColor = red;
            Anchor(Img("Marker", contact.rectTransform, red).rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(6f, 72f));
            TextMeshProUGUI contactLabel = Text("Name", contact.rectTransform, mono, "БОСС", 30f, cyan, TextAlignmentOptions.Left);
            Anchor(contactLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(330f, 50f));

            Image topic = Img("Topic", panelRect, new Color(0.25f, 0.04f, 0.06f, 1f));
            Anchor(topic.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(80f, -236f), new Vector2(340f, 64f));
            topic.gameObject.AddComponent<Outline>().effectColor = redDim;
            TextMeshProUGUI topicLabel = Text("Label", topic.rectTransform, mono, string.Empty, 17f, cyan, TextAlignmentOptions.Left);
            Anchor(topicLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(240f, 50f));
            topicLabel.textWrappingMode = TextWrappingModes.Normal;
            Image newTag = Img("New", topic.rectTransform, new Color(0.25f, 0.04f, 0.06f, 1f));
            Anchor(newTag.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(70f, 26f));
            newTag.gameObject.AddComponent<Outline>().effectColor = yellow;
            MainMenuSceneBuilder.Stretch(Text("Label", newTag.rectTransform, mono, "НОВОЕ", 14f, yellow, TextAlignmentOptions.Center).rectTransform);

            // Лента сообщений справа
            TextMeshProUGUI threadHeader = Text("ThreadHeader", panelRect, mono, "ЗАЩИЩЁННЫЙ КАНАЛ  //  ШИФРОВАНИЕ AES-4096", 14f, redDim, TextAlignmentOptions.BottomLeft);
            Anchor(threadHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(460f, -130f), new Vector2(640f, 22f));
            Anchor(Img("ThreadLine", panelRect, redDim).rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(460f, -158f), new Vector2(640f, 1f));

            RectTransform thread = MainMenuSceneBuilder.CreateRect("Thread", panelRect);
            Anchor(thread, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(460f, -180f), new Vector2(640f, 760f));
            VerticalLayoutGroup threadLayout = thread.gameObject.AddComponent<VerticalLayoutGroup>();
            threadLayout.spacing = 20f;
            threadLayout.childAlignment = TextAnchor.UpperLeft;
            threadLayout.childControlWidth = true;
            threadLayout.childControlHeight = true;
            threadLayout.childForceExpandWidth = true;
            threadLayout.childForceExpandHeight = false;

            // Шаблон сообщения: тёмная плашка в жёлтой рамке, текст подстраивает высоту
            Image bubble = Img("BubbleTemplate", thread, new Color(0.1f, 0.04f, 0.03f, 1f));
            bubble.gameObject.AddComponent<Outline>().effectColor = yellow;
            VerticalLayoutGroup bubbleLayout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            bubbleLayout.padding = new RectOffset(28, 28, 20, 20);
            bubbleLayout.childControlWidth = true;
            bubbleLayout.childControlHeight = true;
            bubbleLayout.childForceExpandWidth = true;
            bubbleLayout.childForceExpandHeight = false;
            TextMeshProUGUI bubbleText = Text("Text", bubble.rectTransform, mono, string.Empty, 28f, yellow, TextAlignmentOptions.TopLeft);
            bubbleText.textWrappingMode = TextWrappingModes.Normal;

            // Вложение: файл с координатами
            Image attachment = Img("Attachment", thread, new Color(0.02f, 0.1f, 0.11f, 1f));
            attachment.gameObject.AddComponent<Outline>().effectColor = cyan;
            LayoutElement attachmentSize = attachment.gameObject.AddComponent<LayoutElement>();
            attachmentSize.preferredHeight = 96f;
            Image icon = Img("Icon", attachment.rectTransform, cyan);
            Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(48f, 56f));
            Anchor(Img("Fold", icon.rectTransform, new Color(0.02f, 0.12f, 0.13f)).rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(14f, 14f));
            Anchor(Img("Pin", icon.rectTransform, new Color(0.02f, 0.12f, 0.13f)).rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -4f), new Vector2(14f, 14f));
            TextMeshProUGUI attachmentLabel = Text("Name", attachment.rectTransform, mono, string.Empty, 26f, cyan, TextAlignmentOptions.BottomLeft);
            Anchor(attachmentLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(96f, 2f), new Vector2(500f, 34f));
            TextMeshProUGUI attachmentInfo = Text("Info", attachment.rectTransform, mono, "ФАЙЛ ПОЛУЧЕН  //  КООРДИНАТЫ ЦЕЛИ", 16f, new Color(0.4f, 1f, 0.95f, 0.6f), TextAlignmentOptions.TopLeft);
            Anchor(attachmentInfo.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(96f, -4f), new Vector2(500f, 24f));

            // Индикатор "печатает" и подсказка закрытия
            TextMeshProUGUI typing = Text("Typing", panelRect, mono, string.Empty, 20f, redDim, TextAlignmentOptions.BottomLeft);
            Anchor(typing.rectTransform, Vector2.zero, Vector2.zero, new Vector2(460f, 60f), new Vector2(500f, 30f));

            RectTransform close = MainMenuSceneBuilder.CreateRect("CloseHint", panelRect);
            Anchor(close, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-50f, 50f), new Vector2(260f, 44f));
            CanvasGroup closeGroup = Group(close.gameObject, 0f);
            Image key = Img("Key", close, new Color(0.07f, 0.015f, 0.03f, 1f));
            Anchor(key.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(100f, 40f));
            key.gameObject.AddComponent<Outline>().effectColor = cyan;
            MainMenuSceneBuilder.Stretch(Text("Label", key.rectTransform, mono, "ENTER", 18f, cyan, TextAlignmentOptions.Center).rectTransform);
            TextMeshProUGUI closeLabel = Text("Label", close, mono, "Закрыть", 26f, red, TextAlignmentOptions.Left);
            Anchor(closeLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(114f, 0f), new Vector2(150f, 40f));

            MessengerPanel messenger = root.gameObject.AddComponent<MessengerPanel>();
            Configure(messenger, so =>
            {
                P(so, "_group").objectReferenceValue = group;
                P(so, "_panel").objectReferenceValue = panelRect;
                P(so, "_contactLabel").objectReferenceValue = contactLabel;
                P(so, "_topicLabel").objectReferenceValue = topicLabel;
                P(so, "_thread").objectReferenceValue = thread;
                P(so, "_bubbleTemplate").objectReferenceValue = bubble.gameObject;
                P(so, "_attachment").objectReferenceValue = attachment.gameObject;
                P(so, "_attachmentLabel").objectReferenceValue = attachmentLabel;
                P(so, "_typingLabel").objectReferenceValue = typing;
                P(so, "_closeHint").objectReferenceValue = closeGroup;
            });
            bubble.gameObject.SetActive(false);
            attachment.gameObject.SetActive(false);
            return messenger;
        }

        // ---------------------------------------------------------------- Арсенал

        private const string WeaponsDir = "Assets/_Project/ScriptableObjects/Weapons";
        private const string ImplantsDir = "Assets/_Project/ScriptableObjects/Implants";

        internal static WeaponData[] CreateWeapons()
        {
            MainMenuSceneBuilder.EnsureFolder(WeaponsDir);
            return new[]
            {
                CreateWeapon("Smg", "КИБЕР-ПП", ArtOr("Weapons/weapon_smg", SpriteFactory.IconSmg), SpriteFactory.HeroSmg(), 0.07f, 7f, 1, 14f, 16, new[] { 5, 2, 2, 2 },
                    "Компактный пистолет-пулемёт с умным спуском. Стреляет быстрее всех и почти не дёргается в руках, " +
                    "но пули разлетаются веером и быстро теряют силу. Для тех, кто врывается в комнату первым."),
                CreateWeapon("Rifle", "АВТОМАТ", ArtOr("Weapons/weapon_rifle", SpriteFactory.IconRifle), SpriteFactory.HeroRifle(), 0.13f, 2.5f, 1, 30f, 25, new[] { 3, 3, 4, 5 },
                    "Штурмовая винтовка корпоративного образца. Ровная, точная очередь на любой дистанции. " +
                    "Золотая середина: не подведёт ни в узком коридоре, ни в большом зале."),
                CreateWeapon("Shotgun", "ДРОБОВИК", ArtOr("Weapons/weapon_shotgun", SpriteFactory.IconShotgun), SpriteFactory.HeroShotgun(), 0.7f, 14f, 8, 8f, 6, new[] { 1, 5, 1, 1 },
                    "Помповое ружьё. Один выстрел — веер из восьми дробин, который сносит всех вблизи. " +
                    "Между выстрелами — пауза на перезарядку, а издалека дробь почти бесполезна."),
            };
        }

        private static ImplantData[] CreateImplants()
        {
            MainMenuSceneBuilder.EnsureFolder(ImplantsDir);
            const string key = "<color=#FFE04D>";
            return new[]
            {
                CreateImplant("GorillaArms", "РУКИ ГОРИЛЛЫ", ArtOr("Implants/implant_gorilla_arms", SpriteFactory.IconGorillaArms), 1,
                    "Кибернетические руки с гидравлическим приводом.\n\n" +
                    $"{key}[ПКМ] — РЫВОК.</color> Короткий бросок вперёд с ударом в конце. Удар дробит цель и отшвыривает её далеко назад.\n\n" +
                    $"{key}[Зажать ПКМ] — БЛОК.</color> Руки закрывают тебя от пуль. Блок держит 3 попадания — шкала над героем " +
                    "показывает запас. После третьего блок ломается и восстанавливается 7 секунд."),
                CreateImplant("BladeArms", "РУКИ-ЛЕЗВИЯ", ArtOr("Implants/implant_blade_arms", () => SpriteFactory.IconLockedImplant("implant_mission2")), 2,
                    "Выдвижные клинки в предплечьях. Кромка режет плоть и броню одинаково легко.\n\n" +
                    $"{key}[ПКМ] — УДАР ЛЕЗВИЕМ.</color> Взмах клинком перед собой. Попал по врагу — разрубает его пополам.\n\n" +
                    $"{key}[ПКМ] по пуле — ОТБИТЬ.</color> Взмахни, когда пуля рядом, — клинок отобьёт её."),
                CreateImplant("Mission3Implant", "ЗАСЕКРЕЧЕНО", ArtOr("Implants/implant_mission3", () => SpriteFactory.IconLockedImplant("implant_mission3")), 3,
                    "Данные об импланте засекречены. Станет доступен перед миссией 3."),
            };
        }

        // Импортированный арт важнее временного: если PNG лежит в Art/Sprites/<path>.png — берём его,
        // иначе рисуем заглушку кодом
        private static Sprite ArtOr(string importedPath, System.Func<Sprite> placeholder)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/_Project/Art/Sprites/{importedPath}.png");
            return sprite != null ? sprite : placeholder();
        }

        // У уже созданного ассета меняем только спрайт-заглушку (пустой или из Generated) — арт, поставленный вручную, не трогаем
        private static void UpdatePlaceholder(Object asset, string field, Sprite current, Sprite sprite)
        {
            if (current != null && !AssetDatabase.GetAssetPath(current).Contains("/Generated/")) return;
            if (current == sprite) return;
            Configure(asset, so => P(so, field).objectReferenceValue = sprite);
            EditorUtility.SetDirty(asset);
        }

        // Ассет создаётся один раз — дальше значения тюнят в инспекторе (как данные взломов)
        private static WeaponData CreateWeapon(string fileName, string displayName, Sprite icon, Sprite heroSprite, float cooldown, float spread,
            int pellets, float range, int magazine, int[] ratings, string description)
        {
            string path = $"{WeaponsDir}/{fileName}.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (weapon != null)
            {
                UpdatePlaceholder(weapon, "_icon", weapon.Icon, icon);
                UpdatePlaceholder(weapon, "_heroSprite", weapon.HeroSprite, heroSprite);
                return weapon;
            }

            weapon = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(weapon, path);
            Configure(weapon, so =>
            {
                P(so, "_displayName").stringValue = displayName;
                P(so, "_description").stringValue = description;
                P(so, "_icon").objectReferenceValue = icon;
                P(so, "_heroSprite").objectReferenceValue = heroSprite;
                P(so, "_cooldown").floatValue = cooldown;
                P(so, "_spreadDegrees").floatValue = spread;
                P(so, "_pellets").intValue = pellets;
                P(so, "_range").floatValue = range;
                P(so, "_magazineSize").intValue = magazine;
                P(so, "_fireRateRating").intValue = ratings[0];
                P(so, "_powerRating").intValue = ratings[1];
                P(so, "_accuracyRating").intValue = ratings[2];
                P(so, "_rangeRating").intValue = ratings[3];
            });
            return weapon;
        }

        private static ImplantData CreateImplant(string fileName, string displayName, Sprite icon, int mission, string description)
        {
            string path = $"{ImplantsDir}/{fileName}.asset";
            var implant = AssetDatabase.LoadAssetAtPath<ImplantData>(path);
            if (implant != null)
            {
                UpdatePlaceholder(implant, "_icon", implant.Icon, icon);
                return implant;
            }

            implant = ScriptableObject.CreateInstance<ImplantData>();
            AssetDatabase.CreateAsset(implant, path);
            Configure(implant, so =>
            {
                P(so, "_displayName").stringValue = displayName;
                P(so, "_description").stringValue = description;
                P(so, "_icon").objectReferenceValue = icon;
                P(so, "_mission").intValue = mission;
            });
            return implant;
        }

        // Экран арсенала: полупрозрачный фон, два барабана с "рыбьим глазом" по центру,
        // слева описание оружия, справа — импланта, внизу ENTER
        private static ArsenalPanel BuildArsenal(RectTransform canvasRect, TMP_FontAsset mono, WeaponData[] weapons, ImplantData[] implants)
        {
            Color red = new Color(1f, 0.33f, 0.33f);
            Color yellow = new Color(0.98f, 0.86f, 0.38f);
            Color cyan = new Color(0.3f, 1f, 1f);
            Vector2 center = new Vector2(0.5f, 0.5f);

            RectTransform root = MainMenuSceneBuilder.CreateRect("Arsenal", canvasRect);
            MainMenuSceneBuilder.Stretch(root);
            CanvasGroup group = Group(root.gameObject, 0f);
            MainMenuSceneBuilder.Stretch(Img("Dim", root, new Color(0.02f, 0f, 0.05f, 0.62f)).rectTransform);

            TextMeshProUGUI title = Text("Title", root, mono, "АРСЕНАЛ", 40f, yellow, TextAlignmentOptions.Center);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 56f));
            Anchor(Img("TitleLine", root, red).rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1500f, 2f));

            DrumSelector weaponDrum = BuildDrum(root, mono, "WeaponDrum", new Vector2(-200f, 20f), weapons.Length,
                out Button weaponUp, out Button weaponDown);
            DrumSelector implantDrum = BuildDrum(root, mono, "ImplantDrum", new Vector2(200f, 20f), implants.Length,
                out Button implantUp, out Button implantDown);

            // Описание оружия (слева) с полосками характеристик
            RectTransform weaponPanel = DescriptionPanel(root, "WeaponInfo", new Vector2(-660f, 20f), red);
            PanelText(weaponPanel, mono, "Header", "ОРУЖИЕ", 18f, red, new Vector2(30f, -24f), 30f);
            TextMeshProUGUI weaponName = PanelText(weaponPanel, mono, "Name", string.Empty, 36f, yellow, new Vector2(30f, -56f), 50f);
            Anchor(Img("Line", weaponPanel, red).rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -116f), new Vector2(420f, 1f));
            TextMeshProUGUI weaponDescription = PanelText(weaponPanel, mono, "Description", string.Empty, 22f, Color.white, new Vector2(30f, -134f), 300f);
            var statLabels = new TextMeshProUGUI[4];
            var statSegments = new Image[20];
            for (int row = 0; row < 4; row++)
            {
                float y = -470f - row * 44f;
                statLabels[row] = PanelText(weaponPanel, mono, $"Stat{row}", string.Empty, 18f, new Color(0.85f, 0.85f, 0.9f), new Vector2(30f, y), 30f);
                for (int i = 0; i < 5; i++)
                {
                    Image segment = Img($"Seg{row}_{i}", weaponPanel, cyan);
                    Anchor(segment.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(270f + i * 36f, y - 8f), new Vector2(30f, 14f));
                    statSegments[row * 5 + i] = segment;
                }
            }

            // Описание импланта (справа)
            RectTransform implantPanel = DescriptionPanel(root, "ImplantInfo", new Vector2(660f, 20f), cyan);
            PanelText(implantPanel, mono, "Header", "КИБЕРИМПЛАНТ", 18f, cyan, new Vector2(30f, -24f), 30f);
            TextMeshProUGUI implantName = PanelText(implantPanel, mono, "Name", string.Empty, 36f, yellow, new Vector2(30f, -56f), 50f);
            TextMeshProUGUI implantStatus = PanelText(implantPanel, mono, "Status", string.Empty, 18f, cyan, new Vector2(30f, -108f), 28f);
            Anchor(Img("Line", implantPanel, cyan).rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -142f), new Vector2(420f, 1f));
            TextMeshProUGUI implantDescription = PanelText(implantPanel, mono, "Description", string.Empty, 21f, Color.white, new Vector2(30f, -160f), 520f);

            // Линии от панелей к барабанам, как на схеме
            ConnectorLine(root, new Vector2(-420f, 300f), new Vector2(-335f, 200f), red);
            ConnectorLine(root, new Vector2(-420f, -260f), new Vector2(-335f, -160f), red);
            ConnectorLine(root, new Vector2(420f, 300f), new Vector2(335f, 200f), cyan);
            ConnectorLine(root, new Vector2(420f, -260f), new Vector2(335f, -160f), cyan);

            // ENTER, предупреждение и подсказка по управлению — от низа экрана, чтобы не наезжать при любом соотношении сторон
            Vector2 bottom = new Vector2(0.5f, 0f);
            TextMeshProUGUI warning = Text("Warning", root, mono, string.Empty, 24f, red, TextAlignmentOptions.Center);
            Anchor(warning.rectTransform, bottom, bottom, new Vector2(0f, 176f), new Vector2(1200f, 34f));
            warning.alpha = 0f;

            Image confirmImage = Img("Confirm", root, new Color(0.08f, 0.04f, 0.02f, 1f));
            confirmImage.raycastTarget = true;
            Anchor(confirmImage.rectTransform, bottom, bottom, new Vector2(0f, 64f), new Vector2(340f, 58f));
            confirmImage.gameObject.AddComponent<Outline>().effectColor = yellow;
            Button confirm = confirmImage.gameObject.AddComponent<Button>();
            confirm.targetGraphic = confirmImage;
            MainMenuSceneBuilder.Stretch(Text("Label", confirmImage.rectTransform, mono, "ENTER   ПОДТВЕРДИТЬ", 24f, yellow, TextAlignmentOptions.Center).rectTransform);

            TextMeshProUGUI controls = Text("Controls", root, mono, "W/S, колесо — крутить    A/D, мышь — выбрать барабан    ENTER — подтвердить",
                16f, new Color(1f, 1f, 1f, 0.45f), TextAlignmentOptions.Center);
            Anchor(controls.rectTransform, bottom, bottom, new Vector2(0f, 128f), new Vector2(1400f, 26f));

            ArsenalPanel arsenal = root.gameObject.AddComponent<ArsenalPanel>();
            Configure(arsenal, so =>
            {
                P(so, "_group").objectReferenceValue = group;
                P(so, "_title").objectReferenceValue = title;
                P(so, "_weaponDrum").objectReferenceValue = weaponDrum;
                P(so, "_implantDrum").objectReferenceValue = implantDrum;
                SetArray(so, "_weapons", weapons);
                SetArray(so, "_implants", implants);
                P(so, "_weaponName").objectReferenceValue = weaponName;
                P(so, "_weaponDescription").objectReferenceValue = weaponDescription;
                SetArray(so, "_statSegments", statSegments);
                SetArray(so, "_statLabels", statLabels);
                P(so, "_implantName").objectReferenceValue = implantName;
                P(so, "_implantStatus").objectReferenceValue = implantStatus;
                P(so, "_implantDescription").objectReferenceValue = implantDescription;
                P(so, "_warning").objectReferenceValue = warning;
                P(so, "_confirm").objectReferenceValue = confirm;
                P(so, "_weaponUp").objectReferenceValue = weaponUp;
                P(so, "_weaponDown").objectReferenceValue = weaponDown;
                P(so, "_implantUp").objectReferenceValue = implantUp;
                P(so, "_implantDown").objectReferenceValue = implantDown;
            });
            return arsenal;
        }

        // Барабан: эллипс-рамка, окно выбора по центру, стрелки ▲▼ и элементы (иконка + подпись)
        private static DrumSelector BuildDrum(RectTransform parent, TMP_FontAsset mono, string name, Vector2 position, int count,
            out Button up, out Button down)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            RectTransform drum = MainMenuSceneBuilder.CreateRect(name, parent);
            Anchor(drum, center, center, position, new Vector2(300f, 760f));

            Image frame = Img("Frame", drum, Color.white);
            frame.sprite = SpriteFactory.DrumFrame();
            MainMenuSceneBuilder.Stretch(frame.rectTransform);

            Image windowTop = Img("WindowTop", drum, Color.white);
            Anchor(windowTop.rectTransform, center, center, new Vector2(0f, 98f), new Vector2(250f, 2f));
            Image windowBottom = Img("WindowBottom", drum, Color.white);
            Anchor(windowBottom.rectTransform, center, center, new Vector2(0f, -98f), new Vector2(250f, 2f));

            Sprite arrow = SpriteFactory.ArrowUp();
            up = ArrowButton(drum, arrow, "Up", new Vector2(0f, 300f), 0f);
            down = ArrowButton(drum, arrow, "Down", new Vector2(0f, -300f), 180f);

            var items = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                RectTransform item = MainMenuSceneBuilder.CreateRect($"Item{i}", drum);
                Anchor(item, center, center, Vector2.zero, new Vector2(260f, 170f));
                item.gameObject.AddComponent<CanvasGroup>();
                Image icon = Img("Icon", item, Color.white);
                icon.preserveAspect = true;
                Anchor(icon.rectTransform, center, center, new Vector2(0f, 18f), new Vector2(240f, 112f));
                TextMeshProUGUI label = Text("Name", item, mono, string.Empty, 22f, Color.white, TextAlignmentOptions.Center);
                Anchor(label.rectTransform, center, center, new Vector2(0f, -62f), new Vector2(260f, 30f));
                items[i] = item;
            }

            Graphic upArrow = up.targetGraphic;
            Graphic downArrow = down.targetGraphic;
            DrumSelector selector = drum.gameObject.AddComponent<DrumSelector>();
            Configure(selector, so =>
            {
                SetArray(so, "_items", items);
                P(so, "_frame").objectReferenceValue = frame;
                SetArray(so, "_accents", windowTop, windowBottom, upArrow, downArrow);
            });
            return selector;
        }

        private static Button ArrowButton(RectTransform parent, Sprite sprite, string name, Vector2 position, float angle)
        {
            Image image = Img(name, parent, Color.white);
            image.sprite = sprite;
            image.raycastTarget = true;
            Anchor(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(48f, 30f));
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static RectTransform DescriptionPanel(RectTransform parent, string name, Vector2 position, Color edge)
        {
            Image panel = Img(name, parent, new Color(0.06f, 0.02f, 0.05f, 1f));
            Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(480f, 700f));
            panel.color = new Color(0.06f, 0.02f, 0.05f, 0.85f);
            Image border = Img("Border", panel.rectTransform, edge);
            border.rectTransform.anchorMin = new Vector2(0f, 1f);
            border.rectTransform.anchorMax = new Vector2(1f, 1f);
            border.rectTransform.pivot = new Vector2(0.5f, 1f);
            border.rectTransform.sizeDelta = new Vector2(0f, 4f);
            return panel.rectTransform;
        }

        private static TextMeshProUGUI PanelText(RectTransform panel, TMP_FontAsset mono, string name, string value, float size, Color color,
            Vector2 position, float height)
        {
            TextMeshProUGUI text = Text(name, panel, mono, value, size, color, TextAlignmentOptions.TopLeft);
            Anchor(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), position, new Vector2(420f, height));
            text.textWrappingMode = TextWrappingModes.Normal;
            text.richText = true;
            return text;
        }

        // Тонкая линия между двумя точками (координаты от центра экрана)
        private static void ConnectorLine(RectTransform parent, Vector2 a, Vector2 b, Color color)
        {
            Image line = Img("Connector", parent, new Color(color.r, color.g, color.b, 0.7f));
            Vector2 d = b - a;
            Anchor(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), (a + b) * 0.5f, new Vector2(d.magnitude, 2f));
            line.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
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

            // Ноги под корпусом: смотрят по ходьбе, корпус — за мышью
            AddLegs(root, rb, SpriteFactory.HeroLegs("hero_legs_idle", 0f), SpriteFactory.HeroLegs("hero_legs_left", 9f),
                SpriteFactory.HeroLegs("hero_legs_pass", 2f), SpriteFactory.HeroLegs("hero_legs_right", -9f), 0.5f);

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
