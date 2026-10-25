using System.Collections.Generic;
using N89.Combat;
using N89.Enemies;
using N89.Hacking;
using N89.Missions;
using N89.Player;
using N89.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static N89.EditorTools.Mission01SceneBuilder;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Собирает миссию 3 — башня «Арка Системс»: 4 этажа широких коридоров, охрана и дроны, наверху лаборатория с реликвией.
    // Пока только первый этаж по схеме от команды: герой приезжает на машине, заходит с улицы снизу (в отводок коридора), зачищает этаж и уходит в верхний лифт —
    // там конец (этажей выше ещё нет). Камер на этой миссии нет. Взлома нет — вместо него Сандевистан (TAB, шкала энергии вместо RAM).
    // Координаты — в юнитах схемы, как в миссиях 1–2 (1 юнит = 24 px, мир = схема × S = 0.6), левый нижний угол — (0, 0).
    // Этаж 40.8 x 25: слева зал с ковром буквой Г (сверху уходит вправо широким коридором), посередине коридор к лифтам
    // с отводком вниз, справа снизу склад буквой Г (ящики, скамья). Справа — две кабины лифта: верхняя (выход наверх) и нижняя — не работает, створки закрыты.
    // Запуск: N89 > Build Mission 03 Scene.
    public static class Mission03SceneBuilder
    {
        public const string SceneName = "Mission03";

        private const float W = 40.8f;
        private const float H = 25f;
        // Перегородки схемы
        private const float HallX = 12.3f;      // зал / коридор
        private const float StoreX = 16.25f;    // коридор / склад
        private const float CorridorTop = 18.3f;
        private const float CorridorBottom = 14.4f;
        private const float CabX = 34.4f;       // кабины лифтов
        private const float TopCabTop = 22.9f;
        private const float LowCabBottom = 9.2f;
        private const float EntranceFrom = 13.1f;  // вход снизу в отводок
        private const float EntranceTo = 15.7f;
        private const float PatrolSpeed = 1.4f;

        // Участок вокруг башни: тротуар и глухая стена соседних мегаблоков (снизу шире — там улица и машина)
        private static readonly Vector2 PlotMin = new Vector2(-7f, -9f);
        private static readonly Vector2 PlotMax = new Vector2(W + 7f, H + 7f);
        private const float DroneSpeed = 2.4f;

        private static readonly Color Teal = new Color(0.25f, 0.95f, 0.85f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.75f);
        private static readonly Color Cyan = new Color(0.3f, 1f, 1f);
        private static readonly Color Cold = new Color(0.8f, 0.9f, 1f);

        [MenuItem("N89/Build Mission 03 Scene")]
        public static void Build()
        {
            EnsureLayers();
            Physics2D.gravity = Vector2.zero;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            LoadCommonAssets();
            SetLayout(Vector2.zero, SpriteFactory.HomeWall());
            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");
            TMP_FontAsset audiowide = MainMenuSceneBuilder.GetOrCreateFontAsset("Audiowide-Regular");

            // --- Префабы: герой как в миссиях 1–2, охрана и дроны «Арки» из миссии 2 ---
            CommonPrefabs common = CreateCommonPrefabs();
            WeaponData[] weapons = Interlude01SceneBuilder.CreateWeapons();
            GameObject heroPrefab = PlayerPrefab("HeroCombat", SpriteFactory.HeroRifle(), new[]
            {
                SpriteFactory.HeroLegs("hero_legs_idle", 0f), SpriteFactory.HeroLegs("hero_legs_left", 9f),
                SpriteFactory.HeroLegs("hero_legs_pass", 2f), SpriteFactory.HeroLegs("hero_legs_right", -9f),
            }, 0.65f, 0.32f, common, 0, SpriteFactory.HeroDead());
            GameObject guardPrefab = EnemyPrefab("ArkaGuard", "ОХРАНА // АРКА СИСТЕМС", SpriteFactory.ArkaGuard(), 1, 0.5f, 0.6f,
                common.EnemyBullet, common.Muzzle, common.Blood, common.BloodPool, common.ShortCircuit, common.Electric, true, SpriteFactory.ArkaGuardDead());
            GameObject dronePrefab = EnemyPrefab("ArkaDrone", "БОЕВОЙ ДРОН // АРКА", SpriteFactory.CombatDrone(), 2, 0.75f, 0.6f,
                common.EnemyBullet, common.Muzzle, common.Explosion, common.Remains, common.ShortCircuit, common.Electric, false);

            // --- Сцена ---
            Camera cam = CreateCamera();
            CreateVolume("Mission03Volume");
            BuildLights();

            Transform level = new GameObject("Level").transform;
            // Вокруг башни — тротуар за глухой стеной участка, за ней крыши города (как в миссии 2)
            BuildCity(level, PlotMin * S - Vector2.one * 2f, PlotMax * S + Vector2.one * 2f, new Vector2(-34f, -36f), new Vector2(60f, 50f));
            BuildPlot(level);
            BuildWalls(level);
            BuildDoors(level);
            BuildFurniture(level);

            Transform enemiesRoot = new GameObject("Guards").transform;
            Health[] enemies = SpawnGuards(guardPrefab, dronePrefab, enemiesRoot);

            // Верхний лифт — выход наверх: этаж зачищен — маркер загорается
            Light2D liftMarker = AddLight(level, "LiftMarker", V(37.6f, 20.6f), Cyan, 2.5f, 1.2f);
            Pulsing(liftMarker, 3f, 0.4f, 1.4f);
            Rect liftZone = WorldRect(new Vector2(CabX + 0.6f, CorridorTop + 0.8f), new Vector2(W - 0.6f, TopCabTop - 0.5f));

            // --- Герой: на тротуаре перед входом снизу (дальше радиуса автодвери), рядом его машина ---
            Transform gameplay = new GameObject("Gameplay").transform;
            Vector2 start = V((EntranceFrom + EntranceTo) * 0.5f, -4.5f);
            GameObject hero = Spawn(heroPrefab, gameplay, start);
            hero.transform.Find("Body").rotation = Quaternion.Euler(0f, 0f, 90f);
            hero.transform.Find("Legs").rotation = Quaternion.Euler(0f, 0f, 90f);
            // Пока этажей выше нет, машина — только декор (на ней Рэй уедет, когда миссия будет целиком)
            BuildAirCar(gameplay, V(23f, -4.5f), 0f);

            CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
            Configure(follow, so =>
            {
                P(so, "_target").objectReferenceValue = hero.transform;
                P(so, "_maxLookAhead").floatValue = 3f;
            });
            Configure(hero.GetComponent<PlayerController>(), so => P(so, "_cameraFollow").objectReferenceValue = follow);
            // Имплант на руки — выбор в арсенале: ставим оба, MissionDirector оставит выбранный
            AddGorillaArms(hero, follow, common);
            Mission02SceneBuilder.AddBladeArms(hero, follow, common);
            cam.transform.position = new Vector3(start.x, start.y, -10f);

            // --- UI и сценарий ---
            HudRefs hud = BuildHud(hero.GetComponent<HackingController>(), mono, audiowide, "ЭТАЖ 1 ЗАЧИЩЕН", "MainMenu");
            // Сандевистан вместо взлома: HackingController выключен (TAB и 1–3 его не будят), RAM в HUD прячет MissionDirector
            hero.GetComponent<HackingController>().enabled = false;
            Sandevistan sandevistan = AddSandevistan(hero, (RectTransform)hud.HackingHud.transform, mono, audiowide);
            Banner banner = BuildBanner(hud.Fader.transform, mono);
            WaypointArrow waypoint = BuildWaypoint(hero.transform);

            MissionDirector director = new GameObject("MissionDirector").AddComponent<MissionDirector>();
            Configure(director, so =>
            {
                P(so, "_player").objectReferenceValue = hero.GetComponent<PlayerController>();
                P(so, "_hacking").objectReferenceValue = hero.GetComponent<HackingController>();
                P(so, "_sandevistan").objectReferenceValue = sandevistan;
                P(so, "_weapon").objectReferenceValue = hero.GetComponent<Weapon>();
                P(so, "_heroBody").objectReferenceValue = hero.transform.Find("Body").GetComponent<SpriteRenderer>();
                P(so, "_mission").intValue = 3;
                SetArray(so, "_weapons", weapons);
                SetArray(so, "_enemies", enemies);
                P(so, "_cameraFollow").objectReferenceValue = follow;

                SetArray(so, "_armImplants", new Object[] { hero.GetComponent<GorillaArms>(), hero.GetComponent<BladeArms>() });
                SetStrings(P(so, "_armImplantIds"), "GorillaArms", "BladeArms");
                SetStrings(P(so, "_armImplantTitles"), "РУКИ ГОРИЛЛЫ", "РУКИ-ЛЕЗВИЯ");
                SetStrings(P(so, "_armImplantHints"), "ПКМ — РЫВОК С УДАРОМ   ·   ЗАЖАТЬ ПКМ — БЛОК", "ПКМ — УДАР ЛЕЗВИЕМ   ·   ПО ПУЛЕ — ОТБИТЬ");

                P(so, "_title").stringValue = "МИССИЯ 3";
                P(so, "_subtitle").stringValue = "Башня Арка Системс";
                P(so, "_objectiveText").stringValue = "Зачисти первый этаж";
                P(so, "_clearedText").stringValue = "Первый этаж зачищен";
                P(so, "_stairsText").stringValue = "Поднимись на лифте";
                P(so, "_stairsZone").rectValue = liftZone;
                P(so, "_stairsMarker").objectReferenceValue = liftMarker;
                P(so, "_checkpoint").stringValue = "mission03_floor2";
                P(so, "_banner").objectReferenceValue = banner;
                P(so, "_waypoint").objectReferenceValue = waypoint;

                P(so, "_fader").objectReferenceValue = hud.Fader;
                P(so, "_objective").objectReferenceValue = hud.Objective;
                P(so, "_hackingHud").objectReferenceValue = hud.HackingHud;
                P(so, "_endScreen").objectReferenceValue = hud.EndScreen;
                P(so, "_subtitles").objectReferenceValue = hud.Subtitles;
                P(so, "_letterboxTop").objectReferenceValue = hud.LetterboxTop;
                P(so, "_letterboxBottom").objectReferenceValue = hud.LetterboxBottom;
            });

            ValidateQuickhacks();
            string path = $"Assets/_Project/Scenes/{SceneName}.unity";
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            AddSceneToBuildSettings(path, Interlude03SceneBuilder.SceneName);
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Миссия 3 собрана: {path}");
        }

        // ---------------------------------------------------------------- Сандевистан

        // Компонент на героя и его HUD: на месте RAM (слева снизу) — панель со шкалой энергии,
        // на весь экран — зелёный фильтр с подписью, пока ускорение включено
        private static Sandevistan AddSandevistan(GameObject hero, RectTransform canvasRect, TMP_FontAsset mono, TMP_FontAsset audiowide)
        {
            Color green = new Color(0.2f, 1f, 0.55f);
            const float width = 390f;

            Image tint = Img("SandevistanOverlay", canvasRect, new Color(0.2f, 1f, 0.55f, 0.05f));
            MainMenuSceneBuilder.Stretch(tint.rectTransform);
            tint.rectTransform.SetAsFirstSibling();
            CanvasGroup overlay = Group(tint.gameObject, 0f);
            overlay.blocksRaycasts = false;
            TextMeshProUGUI overlayLabel = Text("Label", tint.rectTransform, mono, "САНДЕВИСТАН // УСКОРЕНИЕ", 26f, green, TextAlignmentOptions.Center);
            Anchor(overlayLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 40f));

            RectTransform panel = Anchor(MainMenuSceneBuilder.CreateRect("Sandevistan", canvasRect), Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(width, 80f));
            Image back = Img("Back", panel, new Color(0.02f, 0.06f, 0.05f, 0.75f));
            Anchor(back.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-16f, -14f), new Vector2(width + 26f, 112f));
            Image edge = Img("Edge", panel, green);
            Anchor(edge.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-16f, -14f), new Vector2(4f, 112f));
            TextMeshProUGUI header = Text("Header", panel, mono, "ВОЕННЫЙ ИМПЛАНТ // САНДЕВИСТАН", 16f, green, TextAlignmentOptions.BottomLeft);
            Anchor(header.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 72f), new Vector2(width, 22f));
            TextMeshProUGUI label = Text("Label", panel, audiowide, "TAB  //  ЭНЕРГИЯ", 26f, Cyan, TextAlignmentOptions.BottomLeft);
            Anchor(label.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 30f), new Vector2(width, 40f));
            Image barBack = Img("Bar", panel, new Color(0.05f, 0.18f, 0.12f, 0.9f));
            Anchor(barBack.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 4f), new Vector2(width, 18f));
            Image fill = Img("Fill", barBack.rectTransform, green);
            MainMenuSceneBuilder.Stretch(fill.rectTransform);
            MakeFilled(fill);

            Sandevistan sandevistan = hero.AddComponent<Sandevistan>();
            Configure(sandevistan, so =>
            {
                P(so, "_body").objectReferenceValue = hero.transform.Find("Body").GetComponent<SpriteRenderer>();
                P(so, "_energyFill").objectReferenceValue = fill;
                P(so, "_overlay").objectReferenceValue = overlay;
            });
            return sandevistan;
        }

        // ---------------------------------------------------------------- Участок

        // Тротуар вокруг башни, глухие стены соседних домов по краю, кадки, вывеска «Арки» над входом, фонари
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

            // Кадки вдоль фасада — низкие, слой Glass (у входа и у машины свободно)
            Sprite plant = SpriteFactory.Plant();
            foreach (Vector2 p in new[] { new Vector2(3f, -1.6f), new Vector2(9.5f, -1.6f), new Vector2(30f, -1.6f), new Vector2(38f, -1.6f),
                         new Vector2(-1.6f, 8f), new Vector2(-1.6f, 18f), new Vector2(W + 1.6f, 6f), new Vector2(W + 1.6f, 18f) })
            {
                Prop(plot, "Planter", plant, p - Vector2.one * 0.75f, p + Vector2.one * 0.75f, 3, GlassLayer);
            }

            float mid = (EntranceFrom + EntranceTo) * 0.5f;
            SpriteRenderer sign = Prop(plot, "ArkaSign", SpriteFactory.NeonSign(), new Vector2(mid - 1.6f, -0.95f), new Vector2(mid + 1.6f, -0.35f), 7, -1, false);
            sign.color = Teal;
            Pulsing(Lamp(plot, "ArkaSign_Light", mid, -0.8f, Teal, 6f, 1f), 2f, 0.7f, 1f);
            Prop(plot, "Doormat", SpriteFactory.Rug(), new Vector2(EntranceFrom, 0.3f), new Vector2(EntranceTo, 1.6f), -97);

            Decor(plot, "Trash", SpriteFactory.TrashPile(), new Vector2(44f, -6f), 2.2f);
            Decor(plot, "Stain", SpriteFactory.OilStain(), new Vector2(30f, -6.5f), 3f);
            Decor(plot, "Butts", SpriteFactory.Butts(), new Vector2(11f, -3.5f), 1f);

            foreach (Vector2 p in new[] { new Vector2(-4f, -6f), new Vector2(18.5f, -6.5f), new Vector2(36f, -6.5f), new Vector2(-4f, 28f),
                         new Vector2(20f, 29f), new Vector2(W + 4f, 22f) })
            {
                Lamp(plot, "Street", p.x, p.y, Cyan, 9f, 0.5f);
            }
        }

        // ---------------------------------------------------------------- Стены

        private static void BuildWalls(Transform parent)
        {
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(parent);
            float t = WallThickness * 0.5f / S;

            // Полы: мрамор коридоров под всем этажом, ковролин в зале, бетон на складе, плитка в кабинах
            Floor(parent, "Floor", SpriteFactory.MarbleFloor(), V(0f, 0f), V(W, H), -100);
            Floor(parent, "Floor_Store", SpriteFactory.ConcreteFloor(), V(StoreX, 0f), V(W, LowCabBottom), -99);
            Floor(parent, "Floor_Store_Top", SpriteFactory.ConcreteFloor(), V(StoreX, LowCabBottom), V(CabX, CorridorBottom), -99);
            Floor(parent, "Floor_Cab_Low", SpriteFactory.FloorTileSprite(), V(CabX, LowCabBottom), V(W, CorridorBottom), -99);
            Floor(parent, "Floor_Cab_Top", SpriteFactory.FloorTileSprite(), V(CabX, CorridorTop), V(W, TopCabTop), -99);

            // Внешние стены; справа сверху — глухой блок над верхней кабиной
            // Вход — снизу в отводок коридора
            Wall(walls, "Outer_Bottom_W", -t, 0f, EntranceFrom, 0f);
            Wall(walls, "Outer_Bottom_E", EntranceTo, 0f, W + t, 0f);
            Wall(walls, "Outer_Left", 0f, -t, 0f, H + t);
            Wall(walls, "Outer_Top", -t, H, CabX, H);
            Wall(walls, "Outer_Right", W, -t, W, TopCabTop);
            Block(walls, "Shaft", new Vector2(CabX, TopCabTop), new Vector2(W + t, H + t));

            // x 12.3: зал / коридор (дверь 10.4..12.7)
            Wall(walls, "V_Hall_1", HallX, -t, HallX, 10.4f);
            Wall(walls, "V_Hall_2", HallX, 12.7f, HallX, CorridorTop);
            // y 18.3: верх зала / коридор (дверь 16.5..19), верхняя кабина (двери 36..39.2)
            Wall(walls, "H_CorridorTop_1", HallX - t, CorridorTop, 16.5f, CorridorTop);
            Wall(walls, "H_CorridorTop_2", 19f, CorridorTop, 36f, CorridorTop);
            Wall(walls, "H_CorridorTop_3", 39.2f, CorridorTop, W, CorridorTop);
            Wall(walls, "V_TopCab", CabX, CorridorTop, CabX, TopCabTop);
            // y 14.4: коридор / склад (дверь 28.75..31.45), нижняя кабина (двери 36..39.2)
            Wall(walls, "H_CorridorBottom_1", StoreX - t, CorridorBottom, 28.75f, CorridorBottom);
            Wall(walls, "H_CorridorBottom_2", 31.45f, CorridorBottom, 36f, CorridorBottom);
            Wall(walls, "H_CorridorBottom_3", 39.2f, CorridorBottom, W, CorridorBottom);
            // x 16.25: отводок коридора / склад (дверь 3.3..5.6)
            Wall(walls, "V_Store_1", StoreX, -t, StoreX, 3.3f);
            Wall(walls, "V_Store_2", StoreX, 5.6f, StoreX, CorridorBottom);
            // Нижняя кабина: стена со складом
            Wall(walls, "V_LowCab", CabX, LowCabBottom - t, CabX, CorridorBottom);
            Wall(walls, "H_LowCab", CabX, LowCabBottom, W, LowCabBottom);
        }

        private static void BuildDoors(Transform parent)
        {
            Transform doors = new GameObject("Doors").transform;
            doors.SetParent(parent);
            AutoDoorAt(doors, "Door_Entrance", true, EntranceFrom, EntranceTo, 0f);
            AutoDoorAt(doors, "Door_Hall_Corridor", false, 10.4f, 12.7f, HallX);
            AutoDoorAt(doors, "Door_Hall_TopCorridor", true, 16.5f, 19f, CorridorTop);
            AutoDoorAt(doors, "Door_Corridor_Store", true, 28.75f, 31.45f, CorridorBottom);
            AutoDoorAt(doors, "Door_Spur_Store", false, 3.3f, 5.6f, StoreX);
            AutoDoorAt(doors, "Lift_Top", true, 36f, 39.2f, CorridorTop);
            // Нижний лифт не работает: створки закрыты намертво (слой Wall)
            Block(doors, "Lift_Low_Closed", new Vector2(36f, CorridorBottom - 0.26f), new Vector2(39.2f, CorridorBottom + 0.26f), SpriteFactory.DoorPanelSci());
        }

        // ---------------------------------------------------------------- Обстановка

        // Высокое — слой Wall, низкое (скамья, ящики) — Glass, ковры и вывески — без коллайдера
        private static void BuildFurniture(Transform parent)
        {
            Transform furniture = new GameObject("Furniture").transform;
            furniture.SetParent(parent);
            Sprite plant = SpriteFactory.Plant();
            Sprite sign = SpriteFactory.NeonSign();

            // Зал: длинный ковёр посередине, растения по углам, вывеска «Арки»
            Prop(furniture, "Carpet", SpriteFactory.Rug(), new Vector2(2.5f, 3.75f), new Vector2(9.8f, 21.25f), -97, -1, true, 90f);
            foreach (Vector2 p in new[] { new Vector2(0.5f, 24f), new Vector2(11.3f, 17.3f), new Vector2(0.5f, 0.5f) })
            {
                Prop(furniture, "Plant", plant, p, p + new Vector2(1f, 1f), 3);
            }
            NeonSign(furniture, sign, new Vector2(6.1f, 24.55f), Teal);
            NeonSign(furniture, sign, new Vector2(23f, 24.55f), Magenta);

            // Коридор к лифтам: кадки вдоль стен (не мешают проходу), табличка у лифтов
            foreach (Vector2 p in new[] { new Vector2(22f, 17.3f), new Vector2(26f, 14.6f), new Vector2(14.9f, 12.9f) })
            {
                Prop(furniture, "Plant", plant, p, p + new Vector2(1f, 1f), 3);
            }
            NeonSign(furniture, sign, new Vector2(35.2f, 16.35f), Cyan, 90f);

            // Склад: три ящика пирамидкой и скамья у правой стены
            Sprite crate = SpriteFactory.Crate();
            Prop(furniture, "Crate_1", crate, new Vector2(20.6f, 3.2f), new Vector2(22.3f, 4.9f), 3, WallLayer);
            Prop(furniture, "Crate_2", crate, new Vector2(22.5f, 3f), new Vector2(24.2f, 4.7f), 3, WallLayer);
            Prop(furniture, "Crate_3", crate, new Vector2(21.8f, 5f), new Vector2(23.5f, 6.7f), 3, WallLayer);
            Prop(furniture, "Bench", SpriteFactory.BenchPress(), new Vector2(36.2f, 2.9f), new Vector2(37.5f, 6.7f), 3, GlassLayer);
            Decor(furniture, "Clutter", SpriteFactory.Clutter(), new Vector2(28f, 12.5f), 1.2f);
            Decor(furniture, "Stain", SpriteFactory.OilStain(), new Vector2(31f, 4f), 2f);
        }

        private static void NeonSign(Transform parent, Sprite sprite, Vector2 schema, Color color, float angle = 0f)
        {
            SpriteRenderer sr = CreateSprite("NeonSign", parent, sprite, ToWorld(schema), 7, false, angle);
            sr.color = color;
            Pulsing(AddLight(sr.transform, "Glow", ToWorld(schema), color, 2.4f, 0.9f), 2.5f, 0.75f, 1f);
        }

        // ---------------------------------------------------------------- Охрана

        // Ромбы на схеме: на пунктирной петле — патруль по ней (по часовой, от своей точки), без петли — пост
        private static Health[] SpawnGuards(GameObject guard, GameObject drone, Transform parent)
        {
            var guards = new List<Health>();

            void Stand(Vector2 position, float facing)
            {
                guards.Add(SpawnEnemy(guard, parent, ToWorld(position), facing, Vector2.zero, 0f).GetComponent<Health>());
            }

            // Старт — на отрезке петли после вершины from; дальше по вершинам петли по кругу
            void OnLoop(GameObject prefab, float speed, Vector2 start, int from, Vector2[] loop)
            {
                var perimeter = new Vector2[loop.Length];
                for (int i = 0; i < loop.Length; i++)
                    perimeter[i] = (loop[(from + 1 + i) % loop.Length] - start) * S;
                Vector2 next = loop[(from + 1) % loop.Length];
                float facing = Mathf.Atan2(next.y - start.y, next.x - start.x) * Mathf.Rad2Deg;
                guards.Add(SpawnPerimeter(prefab, parent, ToWorld(start), facing, speed, perimeter).GetComponent<Health>());
            }

            // Зал буквой Г: петля вокруг ковра и по верхнему коридору до лифтов
            Vector2[] hall = { new(1.7f, 23.3f), new(32.7f, 23.3f), new(32.7f, 19.9f), new(10.7f, 19.9f), new(10.7f, 1.7f), new(1.7f, 1.7f) };
            OnLoop(guard, PatrolSpeed, hall[0], 0, hall);
            OnLoop(drone, DroneSpeed, hall[1], 1, hall);
            OnLoop(guard, PatrolSpeed, hall[2], 2, hall);
            OnLoop(guard, PatrolSpeed, hall[4], 4, hall);
            OnLoop(drone, DroneSpeed, new Vector2(6.25f, 1.7f), 4, hall);
            OnLoop(guard, PatrolSpeed, hall[5], 5, hall);

            // Коридор к лифтам: трое на постах
            Stand(new Vector2(13.9f, 16.4f), 0f);
            Stand(new Vector2(19.9f, 16.4f), -60f);
            Stand(new Vector2(32.3f, 16.4f), 180f);

            // Склад: верхнюю петлю облетает дрон, нижнюю обходят трое
            Vector2[] storeTop = { new(17.9f, 12.8f), new(32.7f, 12.8f), new(32.7f, 10.4f), new(17.9f, 10.4f) };
            OnLoop(drone, DroneSpeed, new Vector2(18.4f, 12.8f), 0, storeTop);
            Vector2[] storeBottom = { new(17.9f, 7.7f), new(39.1f, 7.7f), new(39.1f, 1.7f), new(17.9f, 1.7f) };
            OnLoop(guard, PatrolSpeed, storeBottom[1], 1, storeBottom);
            OnLoop(guard, PatrolSpeed, storeBottom[2], 2, storeBottom);
            OnLoop(guard, PatrolSpeed, storeBottom[3], 3, storeBottom);

            return guards.ToArray();
        }

        // ---------------------------------------------------------------- Свет

        private static void BuildLights()
        {
            Transform lights = new GameObject("Lights").transform;
            Light2D global = new GameObject("Global Light").AddComponent<Light2D>();
            global.transform.SetParent(lights);
            global.lightType = Light2D.LightType.Global;
            global.intensity = 0.3f;
            global.color = new Color(0.5f, 0.52f, 0.78f);

            Color warm = new Color(1f, 0.72f, 0.45f);
            Color red = new Color(1f, 0.2f, 0.25f);

            Pulsing(Lamp(lights, "Hall", 6.1f, 12.5f, Magenta, 16f, 0.55f), 0.8f, 0.45f, 0.7f);
            Lamp(lights, "Hall_Top", 22f, 21.6f, Teal, 13f, 0.55f);
            Lamp(lights, "Corridor", 25f, 16.4f, Cold, 12f, 0.55f);
            Lamp(lights, "Spur", 14.3f, 7f, Cold, 9f, 0.5f);
            Lamp(lights, "Store_Top", 25f, 11.8f, warm, 10f, 0.55f);
            Lamp(lights, "Store", 28f, 4.5f, warm, 14f, 0.6f);
            Pulsing(Lamp(lights, "Lift_Low", 37.6f, 11.8f, new Color(1f, 0.2f, 0.25f), 4f, 0.5f), 0.6f, 0.2f, 0.6f);
            Lamp(lights, "Lift_Top", 37.6f, 20.6f, Cyan, 5f, 0.7f);
        }

        private static void SetStrings(SerializedProperty array, params string[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }
    }
}
