using System.Collections.Generic;
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
using UnityEngine.UI;
using static N89.EditorTools.Mission01SceneBuilder;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Собирает миссию 3 — башня «Арка Системс»: 4 этажа широких коридоров, охрана и дроны, наверху лаборатория с реликвией.
    // Этажи по схемам от команды: герой приезжает на машине, заходит с улицы снизу (в отводок коридора), зачищает этаж,
    // на верхнем лифте поднимается на второй (BuildFloor2), третий (BuildFloor3, засада) и в лабораторию (BuildFloor4:
    // противостояние, начальник, сейф с relic), потом на лифте вниз и к машине. Камер на этой миссии нет. Взлома нет — вместо него E.N.A (TAB, шкала энергии вместо RAM).
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

        // Второй этаж (схема «2 floor»): та же сетка, но коробка сдвинута в мире, левая треть схемы — пустота
        private static readonly Vector2 Floor2Origin = new Vector2(100f, 0f);
        private const float F2Left = 10.15f;      // левая стена этажа
        private const float F2CorridorX = 15.1f;  // левый коридор / комнаты
        private const float F2RoomBottom = 18.4f; // низ верхней комнаты, верх коридора к лифтам
        private const float F2HallTop = 13.5f;    // верх нижнего зала, низ коридора к лифтам
        private const float F2CabTop = 22.7f;
        private const float F2LowCabBottom = 9.4f;

        // Третий этаж (схема «3 floor»): ещё дальше в мире; слева снизу — пустота (x < 7.25, y < 5.85)
        private static readonly Vector2 Floor3Origin = new Vector2(200f, 0f);
        private const float F3CorridorBottom = 5.85f; // низ коридора, верх нижнего зала
        private const float F3CorridorTop = 9.15f;    // верх коридора, низ комнат
        private const float F3UpperBottom = 13.4f;    // коридор к лифтам: низ
        private const float F3UpperTop = 17f;         // коридор к лифтам: верх
        private const float F3CabX = 34f;
        private const float F3CabTop = 21.1f;
        private const float F3LowCabX = 34.6f;

        // Четвёртый этаж (схема «последний этаж»): лаборатория слева, коридор к лифтам справа, остальное — пустота
        private static readonly Vector2 Floor4Origin = new Vector2(300f, 0f);
        private const float LabLeft = 5.4f, LabRight = 24.9f, LabBottom = 9.15f, LabTop = 20.6f;
        private const float F4CorrBottom = 13.3f, F4CorrTop = 16.8f, F4CabX = 34.5f, F4CabTop = 21f, F4LowCabBottom = 9.4f;
        public const string RelicPath = "Assets/_Project/Art/Sprites/Items/relic.png";

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
                common.EnemyBullet, common.Muzzle, common.Blood, common.BloodPool, common.ShortCircuit, common.Electric, true, SpriteFactory.ArkaGuardDead(), new Color(0.2f, 0.22f, 0.27f));
            GameObject dronePrefab = EnemyPrefab("ArkaDrone", "БОЕВОЙ ДРОН // АРКА", SpriteFactory.CombatDrone(), 2, 0.75f, 0.6f,
                common.EnemyBullet, common.Muzzle, common.Explosion, common.Remains, common.ShortCircuit, common.Electric, false);
            // Солдаты Корпорации Е: врываются в башню, если Рэй откажется отдать relic
            GameObject soldierPrefab = EnemyPrefab("CorpSoldier", "СОЛДАТ // КОРПОРАЦИЯ Е", SpriteFactory.Guard(), 1, 0.45f, 0.5f,
                common.EnemyBullet, common.Muzzle, common.Blood, common.BloodPool, common.ShortCircuit, common.Electric, true, SpriteFactory.GuardDead());

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

            // --- Второй этаж: своя коробка далеко справа в мире, чтобы этажи не видели друг друга ---
            SetLayout(Floor2Origin, SpriteFactory.HomeWall());
            FloorRefs floor2 = BuildFloor2(level, guardPrefab, dronePrefab);
            SetLayout(Floor3Origin, SpriteFactory.HomeWall());
            FloorRefs floor3 = BuildFloor3(level, guardPrefab, dronePrefab);
            SetLayout(Floor4Origin, SpriteFactory.HomeWall());
            Floor4Refs floor4 = BuildFloor4(level, guardPrefab);
            SetLayout(Vector2.zero, SpriteFactory.HomeWall());

            // С relic Рэй спускается на лифте: внизу — у кабины первого этажа, дальше к машине
            Transform returnSpawn = new GameObject("ReturnSpawn").transform;
            returnSpawn.SetParent(level);
            returnSpawn.position = V(37.6f, 20.6f);

            // Отказ отдать relic: корпорация занимает этажи 1–2 (группы неактивны до выбора), у входа ждёт отряд
            GameObject corpFloor1 = SpawnCorp(level, soldierPrefab, "Corp_Floor1",
                new Vector2(25f, 16.4f), new Vector2(30f, 16f), new Vector2(25f, 11.5f), new Vector2(30f, 4f),
                new Vector2(14.3f, 6f), new Vector2(6f, 12f), new Vector2(6f, 20f));
            SetLayout(Floor2Origin, SpriteFactory.HomeWall());
            GameObject corpFloor2 = SpawnCorp(level, soldierPrefab, "Corp_Floor2",
                new Vector2(28f, 15.9f), new Vector2(20f, 16.5f), new Vector2(12.5f, 8f), new Vector2(12.5f, 18f),
                new Vector2(25f, 6f), new Vector2(36f, 4f), new Vector2(27f, 21.5f));
            SetLayout(Vector2.zero, SpriteFactory.HomeWall());
            GameObject exitSquad = SpawnCorp(level, soldierPrefab, "Corp_ExitSquad",
                new Vector2(9.5f, -6.8f), new Vector2(12f, -7.2f), new Vector2(14.4f, -7.4f), new Vector2(16.8f, -7.2f), new Vector2(19.3f, -6.8f));
            exitSquad.transform.position = V(14.4f, -7.2f);
            foreach (Transform soldier in exitSquad.transform) soldier.position -= exitSquad.transform.position;
            Transform executioner = exitSquad.transform.GetChild(2);
            // Машины Корпорации Е: тёмные, садятся у входа в катсцене после отказа
            var corpCars = new AirCar[2];
            corpCars[0] = BuildAirCar(level, V(4.5f, -5.6f), 0f);
            corpCars[1] = BuildAirCar(level, V(30.5f, -5.6f), 180f);
            foreach (AirCar corpCar in corpCars)
            {
                corpCar.name = "CorpCar";
                corpCar.transform.Find("Body").GetComponent<SpriteRenderer>().color = new Color(0.32f, 0.3f, 0.36f);
                corpCar.gameObject.SetActive(false);
            }
            Transform streetCamPoint = new GameObject("StreetCamPoint").transform;
            streetCamPoint.SetParent(level);
            streetCamPoint.position = V(16f, -4.6f);

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
            // На ней Рэй и уедет с relic (MissionDirector.Extract)
            AirCar car = BuildAirCar(gameplay, V(23f, -4.5f), 0f);

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
            HudRefs hud = BuildHud(hero.GetComponent<HackingController>(), mono, audiowide, "МИССИЯ 3 ВЫПОЛНЕНА", "MainMenu");
            // E.N.A вместо взлома: HackingController выключен (TAB и 1–3 его не будят), RAM в HUD прячет MissionDirector
            hero.GetComponent<HackingController>().enabled = false;
            Sandevistan sandevistan = AddSandevistan(hero, (RectTransform)hud.HackingHud.transform, mono, audiowide);
            Banner banner = BuildBanner(hud.Fader.transform, mono);
            MessengerPanel messenger = Interlude01SceneBuilder.BuildMessenger((RectTransform)hud.HackingHud.transform, mono);
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
                SetStrings(P(so, "_armImplantTitles"), "GORILLA HANDS", "BLADE HANDS");
                SetStrings(P(so, "_armImplantHints"), "ПКМ — РЫВОК С УДАРОМ   ·   ЗАЖАТЬ ПКМ — БЛОК", "ПКМ — УДАР ЛЕЗВИЕМ   ·   ПО ПУЛЕ — ОТБИТЬ");

                P(so, "_title").stringValue = "МИССИЯ 3";
                P(so, "_subtitle").stringValue = "Башня Арка Системс";
                P(so, "_objectiveText").stringValue = "Зачисти первый этаж";
                P(so, "_clearedText").stringValue = "Relic у тебя";
                P(so, "_stairsText").stringValue = "Поднимись на лифте";
                P(so, "_stairsZone").rectValue = liftZone;
                P(so, "_stairsMarker").objectReferenceValue = liftMarker;
                // Лифт привозит на второй этаж; смерть там отматывает к кабине второго этажа
                SetArray(so, "_floor2Enemies", floor2.Enemies);
                P(so, "_floor2Spawn").objectReferenceValue = floor2.Spawn;
                P(so, "_floor2Facing").floatValue = -90f;
                P(so, "_floor2Text").stringValue = "Зачисти второй этаж";
                P(so, "_floor2ExitZone").rectValue = floor2.ExitZone;
                P(so, "_floor2ExitMarker").objectReferenceValue = floor2.ExitMarker;
                P(so, "_checkpoint").stringValue = "mission03_floor2";
                // Третий этаж: лифт со второго, засада из нижнего лифта, свой чекпоинт
                SerializedProperty upper = P(so, "_upperFloors");
                upper.arraySize = 2;
                SerializedProperty f3 = upper.GetArrayElementAtIndex(0);
                SetObjects(f3.FindPropertyRelative("Enemies"), floor3.Enemies);
                f3.FindPropertyRelative("Spawn").objectReferenceValue = floor3.Spawn;
                f3.FindPropertyRelative("Facing").floatValue = -90f;
                f3.FindPropertyRelative("Text").stringValue = "Зачисти третий этаж";
                f3.FindPropertyRelative("Checkpoint").stringValue = "mission03_floor3";
                f3.FindPropertyRelative("ExitZone").rectValue = floor3.ExitZone;
                f3.FindPropertyRelative("ExitMarker").objectReferenceValue = floor3.ExitMarker;
                f3.FindPropertyRelative("AmbushDoor").objectReferenceValue = floor3.AmbushDoor;
                SetObjects(f3.FindPropertyRelative("Ambushers"), floor3.Ambushers);
                // Четвёртый этаж — финал: двое в коридоре, противостояние в лаборатории, начальник, сейф с relic
                SerializedProperty f4 = upper.GetArrayElementAtIndex(1);
                SetObjects(f4.FindPropertyRelative("Enemies"), floor4.Floor.Enemies);
                f4.FindPropertyRelative("Spawn").objectReferenceValue = floor4.Floor.Spawn;
                f4.FindPropertyRelative("Facing").floatValue = -90f;
                f4.FindPropertyRelative("Text").stringValue = "Зачисти лабораторию";
                f4.FindPropertyRelative("Checkpoint").stringValue = "mission03_floor4";
                f4.FindPropertyRelative("ExitZone").rectValue = floor4.Floor.ExitZone;
                f4.FindPropertyRelative("ExitMarker").objectReferenceValue = floor4.Floor.ExitMarker;
                f4.FindPropertyRelative("AmbushDoor").objectReferenceValue = null;
                SetObjects(f4.FindPropertyRelative("Ambushers"), new Object[0]);
                f4.FindPropertyRelative("Finale").boolValue = true;

                SetArray(so, "_standoffEnemies", floor4.Standoff);
                P(so, "_standoffZone").rectValue = floor4.StandoffZone;
                P(so, "_standoffTitle").stringValue = "ОНИ ЖДУТ";
                P(so, "_standoffHint").stringValue = "TAB — E.N.A";
                P(so, "_target").objectReferenceValue = floor4.Target;
                P(so, "_targetBody").objectReferenceValue = floor4.TargetBody;
                P(so, "_targetStanding").objectReferenceValue = SpriteFactory.TargetStanding();
                P(so, "_targetAim").objectReferenceValue = SpriteFactory.TargetAim();
                P(so, "_targetDead").objectReferenceValue = SpriteFactory.TargetDead();
                P(so, "_deskPoint").vector2Value = floor4.DeskPoint;
                P(so, "_bloodFxPrefab").objectReferenceValue = common.Blood;
                P(so, "_bloodPoolPrefab").objectReferenceValue = common.BloodPool;
                P(so, "_targetSpeaker").stringValue = "НАЧАЛЬНИК";
                P(so, "_targetGun").objectReferenceValue = floor4.Gun;
                SetStrings(P(so, "_targetLines"), "Я тебя грохну нахер!");
                P(so, "_keyItem").objectReferenceValue = floor4.Key;
                P(so, "_safe").objectReferenceValue = floor4.Safe;
                P(so, "_safeBody").objectReferenceValue = floor4.SafeBody;
                P(so, "_safeOpen").objectReferenceValue = SpriteFactory.WallSafeOpen();
                P(so, "_relicItem").objectReferenceValue = floor4.Relic;
                P(so, "_keyText").stringValue = "Возьми ключ от сейфа";
                P(so, "_safeText").stringValue = "Открой сейф";
                P(so, "_relicTitle").stringValue = "RELIC";
                P(so, "_relicSubtitle").stringValue = "КОНСТРУКТ У ТЕБЯ";
                SetStrings(P(so, "_relicLines"), "...", "Вот ты какой.");

                // Отход: на лифте вниз, к машине на улице
                P(so, "_car").objectReferenceValue = car;
                P(so, "_returnZone").rectValue = floor4.Floor.ExitZone;
                P(so, "_returnSpawn").objectReferenceValue = returnSpawn;
                P(so, "_returnFacing").floatValue = -90f;
                P(so, "_returnText").stringValue = "Спустись на лифте";
                P(so, "_carText").stringValue = "Сядь в машину";

                // Спуск с relic: на третьем этаже пишет Босс — выбор, дальше концовки
                P(so, "_messenger").objectReferenceValue = messenger;
                P(so, "_bossContact").stringValue = "БОСС";
                P(so, "_dialogTopic").stringValue = "RELIC";
                SetStrings(P(so, "_dialogLines"),
                    "Ты достал реликвию?",
                    "Босс, а кем я был до всего этого?",
                    "Ты всегда был и остаёшься нашим верным солдатом. Так ты раздобыл реликвию?",
                    "Меня, кажется, звали Брайс.",
                    "Твоя задача — раздобыть реликвию и отдать её мне. Отдашь по-хорошему?");
                SerializedProperty fromHero = P(so, "_dialogFromHero");
                fromHero.arraySize = 5;
                for (int i = 0; i < 5; i++) fromHero.GetArrayElementAtIndex(i).boolValue = i % 2 == 1;
                SetStrings(P(so, "_dialogChoices"), "Отдать relic", "Отказаться");
                P(so, "_corpFloor2").objectReferenceValue = corpFloor2;
                P(so, "_corpFloor1").objectReferenceValue = corpFloor1;
                P(so, "_exitSquad").objectReferenceValue = exitSquad;
                P(so, "_executioner").objectReferenceValue = executioner;
                P(so, "_exitZone").rectValue = WorldRect(new Vector2(EntranceFrom - 0.5f, -3.2f), new Vector2(EntranceTo + 0.5f, -0.5f));
                P(so, "_insidePoint").vector2Value = V((EntranceFrom + EntranceTo) * 0.5f, 3.5f);
                P(so, "_heroLying").objectReferenceValue = SpriteFactory.HeroDead();
                P(so, "_heroHit").objectReferenceValue = SpriteFactory.HeroHit();
                P(so, "_endingScene").stringValue = EndingSceneBuilder.SceneName;
                SetObjects(P(so, "_corpCars"), corpCars);
                P(so, "_streetCamPoint").objectReferenceValue = streetCamPoint;
                P(so, "_descendText").stringValue = "Спустись на лифте";
                P(so, "_corpArrivalLine").stringValue = "Жаль, Рэй. Ты был хорошим псом.";
                P(so, "_heroArm").objectReferenceValue = SpriteFactory.HeroArm();
                P(so, "_heroDying").objectReferenceValue = SpriteFactory.HeroDying();
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

        // ---------------------------------------------------------------- E.N.A

        // Компонент на героя и его HUD: на месте RAM (слева снизу) — панель со шкалой энергии,
        // на весь экран — синий фильтр с подписью, пока ускорение включено
        private static Sandevistan AddSandevistan(GameObject hero, RectTransform canvasRect, TMP_FontAsset mono, TMP_FontAsset audiowide)
        {
            Color yellow = new Color(1f, 0.85f, 0.2f);
            Color blue = new Color(0.25f, 0.6f, 1f);
            const float width = 390f;

            Image tint = Img("SandevistanOverlay", canvasRect, new Color(0.25f, 0.6f, 1f, 0.06f));
            MainMenuSceneBuilder.Stretch(tint.rectTransform);
            tint.rectTransform.SetAsFirstSibling();
            CanvasGroup overlay = Group(tint.gameObject, 0f);
            overlay.blocksRaycasts = false;
            TextMeshProUGUI overlayLabel = Text("Label", tint.rectTransform, mono, "E.N.A // УСКОРЕНИЕ", 26f, yellow, TextAlignmentOptions.Center);
            Anchor(overlayLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 40f));

            RectTransform panel = Anchor(MainMenuSceneBuilder.CreateRect("Sandevistan", canvasRect), Vector2.zero, Vector2.zero, new Vector2(40f, 40f), new Vector2(width, 80f));
            Image back = Img("Back", panel, new Color(0.02f, 0.04f, 0.09f, 0.75f));
            Anchor(back.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-16f, -14f), new Vector2(width + 26f, 112f));
            Image edge = Img("Edge", panel, blue);
            Anchor(edge.rectTransform, Vector2.zero, Vector2.zero, new Vector2(-16f, -14f), new Vector2(4f, 112f));
            TextMeshProUGUI header = Text("Header", panel, mono, "E.N.A // ELECTRIC NEURONS AMPLIFIER", 16f, blue, TextAlignmentOptions.BottomLeft);
            Anchor(header.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 72f), new Vector2(width, 22f));
            TextMeshProUGUI label = Text("Label", panel, audiowide, "TAB  //  ЭНЕРГИЯ", 26f, Cyan, TextAlignmentOptions.BottomLeft);
            Anchor(label.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 30f), new Vector2(width, 40f));
            Image barBack = Img("Bar", panel, new Color(0.05f, 0.1f, 0.22f, 0.9f));
            Anchor(barBack.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 4f), new Vector2(width, 18f));
            Image fill = Img("Fill", barBack.rectTransform, yellow);
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

        // ---------------------------------------------------------------- Четвёртый этаж

        private struct Floor4Refs
        {
            public FloorRefs Floor;
            public EnemyAI[] Standoff;
            public Rect StandoffZone;
            public Transform Target;
            public SpriteRenderer TargetBody;
            public SpriteRenderer Gun;
            public Vector2 DeskPoint;
            public SpriteRenderer Key;
            public Transform Safe;
            public SpriteRenderer SafeBody;
            public SpriteRenderer Relic;
        }

        // Схема «последний этаж»: лаборатория (шкафы вдоль верхней стены, стол начальника, ящики с техникой, сейф
        // в нижней стене — в нём relic) и коридор от её двери к лифтам. В коридоре двое у двери, в лаборатории шестеро
        // ждут с оружием наготове (противостояние — MissionDirector.Standoff), за столом — начальник (не враг: умирает
        // в катсцене, как главарь миссии 1). Вызывается при SetLayout(Floor4Origin, …) — все координаты схемы
        private static Floor4Refs BuildFloor4(Transform parent, GameObject guard)
        {
            Transform root = new GameObject("Floor4").transform;
            root.SetParent(parent);
            float t = WallThickness * 0.5f / S;

            BuildCity(root, V(LabLeft - 1f, LabBottom - 1f), V(W + 1f, F4CabTop + 1f), Floor4Origin + new Vector2(-15f, -15f), Floor4Origin + new Vector2(45f, 35f));

            Floor(root, "Floor_Lab", SpriteFactory.MarbleFloor(), V(LabLeft, LabBottom), V(LabRight, LabTop), -100);
            Floor(root, "Floor_Corridor", SpriteFactory.MarbleFloor(), V(LabRight, F4CorrBottom), V(W, F4CorrTop), -100);
            Floor(root, "Floor_Cab_Top", SpriteFactory.FloorTileSprite(), V(F4CabX, F4CorrTop), V(W, F4CabTop), -100);
            Floor(root, "Floor_Cab_Low", SpriteFactory.FloorTileSprite(), V(F4CabX, F4LowCabBottom), V(W, F4CorrBottom), -100);

            // Стены
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(root);
            Wall(walls, "Lab_Bottom", LabLeft - t, LabBottom, LabRight + t, LabBottom);
            Wall(walls, "Lab_Top", LabLeft - t, LabTop, LabRight + t, LabTop);
            Wall(walls, "Lab_Left", LabLeft, LabBottom, LabLeft, LabTop);
            // Дверь лаборатории — справа (13.9..16.3)
            Wall(walls, "Lab_Right_1", LabRight, LabBottom, LabRight, 13.9f);
            Wall(walls, "Lab_Right_2", LabRight, 16.3f, LabRight, LabTop);
            Wall(walls, "Corr_Bottom_1", LabRight, F4CorrBottom, 36.3f, F4CorrBottom);
            Wall(walls, "Corr_Bottom_2", 38.65f, F4CorrBottom, W, F4CorrBottom);
            Wall(walls, "Corr_Top_1", LabRight, F4CorrTop, 36.3f, F4CorrTop);
            Wall(walls, "Corr_Top_2", 38.65f, F4CorrTop, W, F4CorrTop);
            Wall(walls, "Right", W, F4LowCabBottom - t, W, F4CabTop + t);
            Wall(walls, "TopCab_Left", F4CabX, F4CorrTop, F4CabX, F4CabTop);
            Wall(walls, "TopCab_Top", F4CabX - t, F4CabTop, W, F4CabTop);
            Wall(walls, "LowCab_Left", F4CabX, F4LowCabBottom, F4CabX, F4CorrBottom);
            Wall(walls, "LowCab_Bottom", F4CabX - t, F4LowCabBottom, W, F4LowCabBottom);

            Transform doors = new GameObject("Doors").transform;
            doors.SetParent(root);
            AutoDoorAt(doors, "Door_Lab", false, 13.9f, 16.3f, LabRight);
            // По обе стороны двери стоят охранники — от них она не открывается, только от героя
            Configure(doors.Find("Door_Lab").GetComponent<AutoDoor>(), so => P(so, "_actors").intValue = 1 << PlayerLayer);
            AutoDoorAt(doors, "Lift_Top", true, 36.3f, 38.65f, F4CorrTop);
            Block(doors, "Lift_Low_Closed", new Vector2(36.3f, F4CorrBottom - 0.26f), new Vector2(38.65f, F4CorrBottom + 0.26f), SpriteFactory.DoorPanelSci());

            // Обстановка: шкафы вдоль верхней стены и в левом углу, стол начальника и кресло, ящики с техникой
            Transform furniture = new GameObject("Furniture").transform;
            furniture.SetParent(root);
            Sprite cabinet = SpriteFactory.Lockers();
            Block(furniture, "Cabinet_1", new Vector2(7.7f, 19.9f), new Vector2(10.9f, LabTop - 0.2f), cabinet);
            Block(furniture, "Cabinet_2", new Vector2(11.6f, 19.9f), new Vector2(15.3f, LabTop - 0.2f), cabinet);
            Block(furniture, "Cabinet_3", new Vector2(16f, 19.9f), new Vector2(19.8f, LabTop - 0.2f), cabinet);
            Block(furniture, "Cabinet_Left", new Vector2(LabLeft + 0.2f, 16.9f), new Vector2(6.1f, 19f), cabinet);
            Prop(furniture, "Desk", SpriteFactory.BossDesk(), new Vector2(7.95f, 13.5f), new Vector2(9.8f, 17.4f), 3, GlassLayer, true, 90f);
            Prop(furniture, "Chair", SpriteFactory.OfficeChair(), new Vector2(6.5f, 14.6f), new Vector2(7.7f, 15.9f), 2, -1, true, 0f);
            Sprite crate = SpriteFactory.Crate();
            Prop(furniture, "TechCrate_1", crate, new Vector2(5.6f, 11.7f), new Vector2(7f, 12.9f), 3, WallLayer);
            Prop(furniture, "TechCrate_2", crate, new Vector2(6.2f, 10.5f), new Vector2(7.4f, 11.6f), 3, WallLayer);
            Prop(furniture, "TechCrate_3", crate, new Vector2(5.6f, 9.3f), new Vector2(7f, 10.4f), 3, WallLayer);
            Prop(furniture, "Server_1", SpriteFactory.Console(), new Vector2(21f, 19.1f), new Vector2(22.3f, 20.3f), 3, WallLayer);
            NeonSign(furniture, SpriteFactory.NeonSign(), new Vector2(15f, 9.6f), Cyan, 180f);
            NeonSign(furniture, SpriteFactory.NeonSign(), new Vector2(35.2f, 15f), Cyan, 90f);

            // Сейф в нижней стене, relic внутри (виден, когда сейф открыт)
            SpriteRenderer safe = Prop(furniture, "WallSafe", SpriteFactory.WallSafe(), new Vector2(7.6f, LabBottom - 0.15f), new Vector2(9.3f, LabBottom + 0.5f), 6);
            Sprite relicSprite = AssetDatabase.LoadAssetAtPath<Sprite>(RelicPath);
            SpriteRenderer relic = CreateSprite("Relic", furniture, relicSprite, V(8.45f, LabBottom + 0.7f), 12, false, 20f);
            relic.transform.localScale = Vector3.one * (0.8f / relicSprite.bounds.size.x);
            AddLight(relic.transform, "Glow", relic.transform.position, Cyan, 1.8f, 1.2f);
            relic.gameObject.SetActive(false);
            SpriteRenderer key = CreateSprite("SafeKey", furniture, SpriteFactory.KeyCard(), V(7f, 14f), 12, false, 15f);
            AddLight(key.transform, "Glow", key.transform.position, new Color(1f, 0.85f, 0.2f), 1.2f, 1f);
            key.gameObject.SetActive(false);

            // Начальник за столом, лицом к двери
            var target = new GameObject("Chief") { layer = GlassLayer };
            target.transform.SetParent(root);
            target.transform.position = V(7.1f, 15.25f);
            target.AddComponent<CircleCollider2D>().radius = 0.36f;
            SpriteRenderer targetBody = CreateSprite("Body", target.transform, SpriteFactory.TargetSitting(), Vector2.zero, 10, true, 0f);
            // Пистолет на столе под рукой: в катсцене начальник тянется к нему
            SpriteRenderer gun = CreateSprite("ChiefPistol", furniture, SpriteFactory.Pistol(), V(8.4f, 16.4f), 11, true, 160f);

            // Свет
            Transform lights = new GameObject("Lights").transform;
            lights.SetParent(root);
            Lamp(lights, "Lab", 15f, 15f, Cold, 22f, 0.8f);
            Lamp(lights, "Lab_Door", 21f, 15f, Cold, 10f, 0.6f);
            Lamp(lights, "Lab_Desk", 8.5f, 15f, new Color(1f, 0.72f, 0.45f), 9f, 0.7f);
            Pulsing(Lamp(lights, "Lab_Neon", 9f, 15f, Magenta, 9f, 0.5f), 0.8f, 0.4f, 0.7f);
            Lamp(lights, "Safe", 8.45f, 10f, Cyan, 3f, 0.5f);
            Lamp(lights, "Corridor", 30f, 15f, Cold, 11f, 0.55f);
            Lamp(lights, "Lift_Top", 37.5f, 19f, Cyan, 5f, 0.7f);
            Pulsing(Lamp(lights, "Lift_Low", 37.5f, 11.3f, new Color(1f, 0.2f, 0.25f), 4f, 0.5f), 0.6f, 0.2f, 0.6f);
            Light2D marker = AddLight(lights, "LiftMarker", V(37.5f, 19f), Cyan, 2.5f, 1.2f);
            Pulsing(marker, 3f, 0.4f, 1.4f);

            Transform spawn = new GameObject("Floor4Spawn").transform;
            spawn.SetParent(root);
            spawn.position = V(37.5f, 19.2f);

            // Охрана: двое в коридоре у двери лаборатории — лицом к лифтам; шестеро в лаборатории — ждут
            Transform guards = new GameObject("Guards_Floor4").transform;
            guards.SetParent(root);
            var enemies = new List<Health>();
            var standoff = new List<EnemyAI>();
            EnemyAI Add(Vector2 p, float facing)
            {
                EnemyAI ai = SpawnEnemy(guard, guards, V(p.x, p.y), facing, Vector2.zero, 0f);
                enemies.Add(ai.GetComponent<Health>());
                return ai;
            }
            Add(new Vector2(26.4f, 15.6f), 0f);
            Add(new Vector2(27.6f, 14.5f), 0f);
            foreach (Vector2 p in new[] { new Vector2(23.2f, 19.3f), new Vector2(22.2f, 16.9f), new Vector2(20f, 15.7f),
                         new Vector2(22.3f, 14.7f), new Vector2(20.5f, 13.4f), new Vector2(23.2f, 10.8f) })
            {
                standoff.Add(Add(p, 0f));
            }

            return new Floor4Refs
            {
                Floor = new FloorRefs
                {
                    Enemies = enemies.ToArray(),
                    Spawn = spawn,
                    ExitZone = WorldRect(new Vector2(F4CabX + 0.6f, F4CorrTop + 0.8f), new Vector2(W - 0.6f, F4CabTop - 0.5f)),
                    ExitMarker = marker,
                    Ambushers = new EnemyAI[0],
                },
                Standoff = standoff.ToArray(),
                // Чуть глубже двери: противостояние начинается, когда герой уже внутри
                StandoffZone = WorldRect(new Vector2(LabLeft, LabBottom), new Vector2(LabRight - 0.6f, LabTop)),
                Target = target.transform,
                TargetBody = targetBody,
                Gun = gun,
                DeskPoint = V(8.9f, 15.4f),
                Key = key,
                Safe = safe.transform,
                SafeBody = safe,
                Relic = relic,
            };
        }

        // ---------------------------------------------------------------- Третий этаж

        // Схема «3 floor»: слева сверху две комнаты (офис с диванами и столами; комната с выступом и скамьями у стены),
        // справа сверху — склад с ящиками (дрон летает вдоль его левой части, в коридор — стекло), под ними — длинный
        // коридор во всю ширину, снизу — длинный зал (стекло в коридор, два патруля навстречу) и зал справа.
        // Лифты: герой приезжает в верхнюю кабину и уходит в неё же; в нижней ждёт охранник — двери открываются,
        // когда герой выходит из кабины (засада, MissionDirector.Ambush).
        // Вызывается при SetLayout(Floor3Origin, …) — все координаты схемы
        private static FloorRefs BuildFloor3(Transform parent, GameObject guard, GameObject drone)
        {
            Transform root = new GameObject("Floor3").transform;
            root.SetParent(parent);
            float t = WallThickness * 0.5f / S;
            Material particleMat = MainMenuSceneBuilder.GetOrCreateMaterial("ParticleUnlit", new Material(UnlitMaterial));
            GameObject glassFx = BurstPrefab("FX_Glass", new Color(0.6f, 0.95f, 1f), Color.white, 30, 2f, 7f, 0.25f, 0.6f, 0.05f, 0.1f, particleMat);

            BuildCity(root, V(-1f, -1f), V(W + 1f, H + 1f), Floor3Origin + new Vector2(-18f, -22f), Floor3Origin + new Vector2(48f, 38f));

            // Полы: мрамор под всем этажом (кроме пустоты слева снизу), по комнатам свои
            Floor(root, "Floor", SpriteFactory.MarbleFloor(), V(0f, F3CorridorBottom), V(W, H), -100);
            Floor(root, "Floor_Bottom", SpriteFactory.MarbleFloor(), V(7.25f, 0f), V(W, F3CorridorBottom), -100);
            Floor(root, "Floor_Office", SpriteFactory.Carpet(), V(0f, F3CorridorTop), V(13.75f, 19.85f), -99);
            Floor(root, "Floor_Office_Top", SpriteFactory.Carpet(), V(0f, 19.85f), V(10.55f, H), -99);
            Floor(root, "Floor_Room", SpriteFactory.WoodFloor(), V(13.75f, F3CorridorTop), V(21.55f, H), -99);
            Floor(root, "Floor_Room_Top", SpriteFactory.WoodFloor(), V(10.55f, 19.85f), V(13.75f, H), -99);
            Floor(root, "Floor_Store", SpriteFactory.ConcreteFloor(), V(21.55f, F3UpperTop), V(F3CabX, H), -99);
            Floor(root, "Floor_Store_Low", SpriteFactory.ConcreteFloor(), V(21.55f, F3CorridorTop), V(25.4f, F3UpperTop), -99);
            Floor(root, "Floor_Hall", SpriteFactory.WoodFloor(), V(7.25f, 0f), V(29.4f, F3CorridorBottom), -99);
            Floor(root, "Floor_HallRight", SpriteFactory.ConcreteFloor(), V(29.4f, 0f), V(W, F3CorridorTop), -99);
            Floor(root, "Floor_Cab_Top", SpriteFactory.FloorTileSprite(), V(F3CabX, F3UpperTop), V(W, F3CabTop), -99);
            Floor(root, "Floor_Cab_Low", SpriteFactory.FloorTileSprite(), V(F3LowCabX, F3CorridorTop), V(W, F3UpperBottom), -99);

            // Стены
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(root);
            Wall(walls, "Outer_Top", -t, H, F3CabX, H);
            Block(walls, "Shaft", new Vector2(F3CabX, F3CabTop), new Vector2(W + t, H + t));
            Wall(walls, "Outer_Right", W, -t, W, F3CabTop);
            Wall(walls, "Outer_Bottom", 7.25f - t, 0f, W + t, 0f);
            Wall(walls, "Outer_Left", 0f, F3CorridorBottom - t, 0f, H + t);
            Wall(walls, "Outer_HallLeft", 7.25f, -t, 7.25f, F3CorridorBottom);
            // y 5.85: коридор / нижний зал — дверь 9.3..11.9, стекло 15.55..25.55
            Wall(walls, "H_CorrBottom_1", -t, F3CorridorBottom, 9.3f, F3CorridorBottom);
            Wall(walls, "H_CorrBottom_2", 11.9f, F3CorridorBottom, 15.55f, F3CorridorBottom);
            Wall(walls, "H_CorrBottom_3", 25.55f, F3CorridorBottom, 29.4f + t, F3CorridorBottom);
            // y 9.15: коридор / комнаты — двери в офис 5.6..8.15 и в комнату 17.6..20
            Wall(walls, "H_CorrTop_1", -t, F3CorridorTop, 5.6f, F3CorridorTop);
            Wall(walls, "H_CorrTop_2", 8.15f, F3CorridorTop, 17.6f, F3CorridorTop);
            Wall(walls, "H_CorrTop_3", 20f, F3CorridorTop, 25.4f, F3CorridorTop);
            Wall(walls, "H_HallRightTop", 29.4f, F3CorridorTop, W, F3CorridorTop);
            // x 29.4: коридор / правый зал (дверь 6.95..8.3); x 29.4..34.6, y 9.15..13.4 — глухой блок
            Wall(walls, "V_HallRight_1", 29.4f, -t, 29.4f, 6.95f);
            Wall(walls, "V_HallRight_2", 29.4f, 8.3f, 29.4f, F3CorridorTop);
            Block(walls, "Solid", new Vector2(29.4f, F3CorridorTop), new Vector2(F3LowCabX, F3UpperBottom));
            // Офис / комната: сплошная стена с выступом комнаты в верхний правый угол офиса
            Wall(walls, "V_Office", 13.75f, F3CorridorTop, 13.75f, 19.85f + t);
            Wall(walls, "H_Notch", 10.55f, 19.85f, 13.75f, 19.85f);
            Wall(walls, "V_Notch", 10.55f, 19.85f - t, 10.55f, H);
            // x 21.55: комната / склад
            Wall(walls, "V_Store", 21.55f, F3CorridorTop - t, 21.55f, H);
            // x 25.4: склад / подъём коридора (стекло 10.4..13.4)
            Wall(walls, "V_Store_Low_1", 25.4f, F3CorridorTop - t, 25.4f, 10.4f);
            Wall(walls, "V_Store_Low_2", 25.4f, F3UpperBottom, 25.4f, F3UpperTop);
            // y 17: склад / коридор к лифтам (дверь 28.4..30.4), верхняя кабина (двери 36.4..38.7)
            Wall(walls, "H_UpperTop_1", 25.4f - t, F3UpperTop, 28.4f, F3UpperTop);
            Wall(walls, "H_UpperTop_2", 30.4f, F3UpperTop, 36.4f, F3UpperTop);
            Wall(walls, "H_UpperTop_3", 38.7f, F3UpperTop, W, F3UpperTop);
            Wall(walls, "V_TopCab", F3CabX, F3UpperTop, F3CabX, F3CabTop);
            // y 13.4: коридор к лифтам / нижняя кабина (двери 36.4..38.7 — засада)
            Wall(walls, "H_UpperBottom_1", F3LowCabX, F3UpperBottom, 36.4f, F3UpperBottom);
            Wall(walls, "H_UpperBottom_2", 38.7f, F3UpperBottom, W, F3UpperBottom);

            Transform doors = new GameObject("Doors").transform;
            doors.SetParent(root);
            AutoDoorAt(doors, "Door_Office", true, 5.6f, 8.15f, F3CorridorTop);
            AutoDoorAt(doors, "Door_Room", true, 17.6f, 20f, F3CorridorTop);
            AutoDoorAt(doors, "Door_Hall", true, 9.3f, 11.9f, F3CorridorBottom);
            AutoDoorAt(doors, "Door_HallRight", false, 6.95f, 8.3f, 29.4f);
            AutoDoorAt(doors, "Door_Store", true, 28.4f, 30.4f, F3UpperTop);
            AutoDoorAt(doors, "Lift_Top", true, 36.4f, 38.7f, F3UpperTop);
            // Нижняя кабина заперта, пока герой не выйдет из своей: обычная дверь на слое Door — NavGrid её не видит,
            // поэтому, когда откроется, охранник сразу найдёт путь
            Door ambushDoor = CreateDoor(doors, "Lift_Low_Ambush", V(37.55f, F3UpperBottom), SpriteFactory.ElevatorDoor(), V(39.3f, F3UpperBottom + 0.6f), 0f, 2.3f * S);
            ambushDoor.gameObject.layer = DoorLayer;

            // Стекло: нижний зал ↔ коридор и склад ↔ подъём коридора
            Transform windows = new GameObject("Windows").transform;
            windows.SetParent(root);
            Sprite pane = SpriteFactory.WindowPane();
            Sprite broken = SpriteFactory.WindowBroken();
            Sprite shards = SpriteFactory.GlassShards();
            Mission02SceneBuilder.WindowAt(windows, "Window_Hall", true, 15.55f, 25.55f, F3CorridorBottom, pane, broken, shards, glassFx);
            Mission02SceneBuilder.WindowAt(windows, "Window_Store", false, 10.4f, F3UpperBottom, 25.4f, pane, broken, shards, glassFx);

            // Обстановка
            Transform furniture = new GameObject("Furniture").transform;
            furniture.SetParent(root);
            Sprite sofa = SpriteFactory.ModernSofa();
            Sprite desk = SpriteFactory.BossDesk();
            // Офис: два кресла-дивана и два стола
            Prop(furniture, "Sofa_1", sofa, new Vector2(3.25f, 19.45f), new Vector2(4.75f, 21.8f), 3, GlassLayer, true, 90f);
            Prop(furniture, "Desk_1", desk, new Vector2(4.9f, 19.45f), new Vector2(8.1f, 21.1f), 3, GlassLayer);
            Prop(furniture, "Sofa_2", sofa, new Vector2(3.1f, 14.8f), new Vector2(4.75f, 17.85f), 3, GlassLayer, true, 90f);
            Prop(furniture, "Desk_2", desk, new Vector2(7.4f, 14.5f), new Vector2(10.7f, 15.8f), 3, GlassLayer);
            // Комната: две скамьи вдоль правой стены
            Prop(furniture, "Bench_1", sofa, new Vector2(20.7f, 15.9f), new Vector2(21.2f, 19.1f), 3, GlassLayer, true, 90f);
            Prop(furniture, "Bench_2", sofa, new Vector2(20.7f, 11.6f), new Vector2(21.2f, 15f), 3, GlassLayer, true, 90f);
            // Склад: ящики в углу у кабины
            Sprite crate = SpriteFactory.Crate();
            Prop(furniture, "Crate_1", crate, new Vector2(30.9f, 17.6f), new Vector2(32.1f, 19.6f), 3, WallLayer);
            Prop(furniture, "Crate_2", crate, new Vector2(32.2f, 18.7f), new Vector2(33.5f, 19.9f), 3, WallLayer);
            Prop(furniture, "Crate_3", crate, new Vector2(32.2f, 17.5f), new Vector2(33.5f, 18.6f), 3, WallLayer);
            Sprite plant = SpriteFactory.Plant();
            foreach (Vector2 p in new[] { new Vector2(0.3f, 6.2f), new Vector2(26f, 13.6f), new Vector2(39.5f, 4f) })
            {
                Prop(furniture, "Plant", plant, p, p + new Vector2(1f, 1f), 3);
            }
            NeonSign(furniture, SpriteFactory.NeonSign(), new Vector2(5f, 24.55f), Teal);
            NeonSign(furniture, SpriteFactory.NeonSign(), new Vector2(17f, 24.55f), Magenta);
            NeonSign(furniture, SpriteFactory.NeonSign(), new Vector2(35.2f, 15.2f), Cyan, 90f);
            Decor(furniture, "Clutter", SpriteFactory.Clutter(), new Vector2(18f, 2.5f), 1.4f);

            // Свет
            Transform lights = new GameObject("Lights").transform;
            lights.SetParent(root);
            Color warm = new Color(1f, 0.72f, 0.45f);
            Lamp(lights, "Office", 6.5f, 17f, warm, 15f, 0.6f);
            Pulsing(Lamp(lights, "Room", 17.6f, 17f, Magenta, 14f, 0.55f), 0.8f, 0.45f, 0.7f);
            Lamp(lights, "Store", 28f, 21f, warm, 12f, 0.55f);
            Lamp(lights, "Corridor", 14f, 7.5f, Cold, 16f, 0.55f);
            Lamp(lights, "Upper", 32f, 15.2f, Cold, 10f, 0.55f);
            Lamp(lights, "Hall", 18f, 3f, Teal, 16f, 0.5f);
            Lamp(lights, "HallRight", 35f, 4.5f, warm, 12f, 0.55f);
            Lamp(lights, "Lift_Top", 37.4f, 19f, Cyan, 5f, 0.7f);
            Pulsing(Lamp(lights, "Lift_Low", 37.6f, 11.3f, new Color(1f, 0.2f, 0.25f), 4f, 0.6f), 1.5f, 0.3f, 0.8f);
            Light2D marker = AddLight(lights, "LiftMarker", V(37.4f, 19f), Cyan, 2.5f, 1.2f);
            Pulsing(marker, 3f, 0.4f, 1.4f);

            Transform spawn = new GameObject("Floor3Spawn").transform;
            spawn.SetParent(root);
            spawn.position = V(37.4f, 19.3f);

            // Охрана
            Transform guards = new GameObject("Guards_Floor3").transform;
            guards.SetParent(root);
            var enemies = new List<Health>();
            Health Post(GameObject prefab, Vector2 p, float facing)
            {
                Health h = SpawnEnemy(prefab, guards, V(p.x, p.y), facing, Vector2.zero, 0f).GetComponent<Health>();
                enemies.Add(h);
                return h;
            }
            void Line(GameObject prefab, float speed, Vector2 from, Vector2 to)
            {
                Vector2 dir = to - from;
                enemies.Add(SpawnEnemy(prefab, guards, V(from.x, from.y), Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, dir * S, speed).GetComponent<Health>());
            }
            // Офис: по углам и у выступа
            Post(guard, new Vector2(1.4f, 23.6f), -45f);
            Post(guard, new Vector2(9.2f, 23.6f), -120f);
            Post(guard, new Vector2(1.4f, 10.5f), 45f);
            Post(guard, new Vector2(12.4f, 10.5f), 150f);
            Post(guard, new Vector2(12.4f, 18.4f), -150f);
            // Комната с выступом
            Post(guard, new Vector2(12.15f, 21.3f), -50f);
            Post(guard, new Vector2(20f, 23.6f), -110f);
            Post(guard, new Vector2(15.45f, 10.5f), 70f);
            // Склад: дрон вдоль левой части, двое у дальней стены
            Line(drone, DroneSpeed, new Vector2(23.5f, 10.5f), new Vector2(23.5f, 23.6f));
            Post(guard, new Vector2(26.5f, 23.6f), -45f);
            Post(guard, new Vector2(32.6f, 23.6f), -135f);
            // Нижний зал: двое навстречу друг другу по верхней линии, один по нижней
            Line(guard, PatrolSpeed, new Vector2(8.6f, 4.5f), new Vector2(28.2f, 4.5f));
            Line(guard, PatrolSpeed, new Vector2(28.2f, 4.5f), new Vector2(8.6f, 4.5f));
            Line(guard, PatrolSpeed, new Vector2(8.6f, 1.4f), new Vector2(28.2f, 1.4f));
            // Зал справа
            Post(guard, new Vector2(39.4f, 7.9f), -135f);
            Post(guard, new Vector2(30.8f, 1.4f), 45f);
            Post(guard, new Vector2(39.4f, 1.4f), 135f);
            // Засада в нижней кабине лифта — лицом к дверям
            EnemyAI ambusher = Post(guard, new Vector2(37.6f, 11.1f), 90f).GetComponent<EnemyAI>();

            return new FloorRefs
            {
                Enemies = enemies.ToArray(),
                Spawn = spawn,
                ExitZone = WorldRect(new Vector2(F3CabX + 0.6f, F3UpperTop + 0.8f), new Vector2(W - 0.6f, F3CabTop - 0.5f)),
                ExitMarker = marker,
                AmbushDoor = ambushDoor,
                Ambushers = new[] { ambusher },
            };
        }

        // ---------------------------------------------------------------- Второй этаж

        private struct FloorRefs
        {
            public Health[] Enemies;
            public Transform Spawn;
            public Rect ExitZone;
            public Light2D ExitMarker;
            public Door AmbushDoor;
            public EnemyAI[] Ambushers;
        }

        // Схема «2 floor»: слева длинный коридор во всю высоту (по нему летают два дрона), справа сверху — комната
        // отдыха (диваны, коробки из-под пиццы), посередине — коридор к лифтам, снизу — большой зал буквой Г.
        // Лифты те же: герой приезжает в верхнюю кабину и после зачистки уходит в неё же, нижняя закрыта.
        // Вызывается при SetLayout(Floor2Origin, …) — все координаты схемы
        private static FloorRefs BuildFloor2(Transform parent, GameObject guard, GameObject drone)
        {
            Transform root = new GameObject("Floor2").transform;
            root.SetParent(parent);
            float t = WallThickness * 0.5f / S;

            // За окнами — те же крыши города
            BuildCity(root, V(F2Left - 1f, -1f), V(W + 1f, H + 1f), Floor2Origin + new Vector2(-12f, -22f), Floor2Origin + new Vector2(48f, 38f));

            // Полы: мрамор под всем этажом, ковролин в комнате отдыха, паркет в зале, плитка в кабинах
            Floor(root, "Floor", SpriteFactory.MarbleFloor(), V(F2Left, 0f), V(W, H), -100);
            Floor(root, "Floor_Lounge", SpriteFactory.Carpet(), V(F2CorridorX, F2RoomBottom), V(CabX, H), -99);
            Floor(root, "Floor_Hall", SpriteFactory.WoodFloor(), V(F2CorridorX, 0f), V(W, F2LowCabBottom), -99);
            Floor(root, "Floor_Hall_Top", SpriteFactory.WoodFloor(), V(F2CorridorX, F2LowCabBottom), V(CabX, F2HallTop), -99);
            Floor(root, "Floor_Cab_Low", SpriteFactory.FloorTileSprite(), V(CabX, F2LowCabBottom), V(W, F2HallTop), -99);
            Floor(root, "Floor_Cab_Top", SpriteFactory.FloorTileSprite(), V(CabX, F2RoomBottom), V(W, F2CabTop), -99);

            // Стены
            Transform walls = new GameObject("Walls").transform;
            walls.SetParent(root);
            Wall(walls, "Outer_Bottom", F2Left - t, 0f, W + t, 0f);
            Wall(walls, "Outer_Left", F2Left, -t, F2Left, H + t);
            Wall(walls, "Outer_Top", F2Left - t, H, CabX, H);
            Wall(walls, "Outer_Right", W, -t, W, F2CabTop);
            Block(walls, "Shaft", new Vector2(CabX, F2CabTop), new Vector2(W + t, H + t));
            // x 15.1: коридор / комната отдыха (сплошная), коридор / зал (дверь 2..3.9)
            Wall(walls, "V_Lounge", F2CorridorX, F2RoomBottom, F2CorridorX, H);
            Wall(walls, "V_Hall_1", F2CorridorX, -t, F2CorridorX, 2f);
            Wall(walls, "V_Hall_2", F2CorridorX, 3.9f, F2CorridorX, F2HallTop);
            // y 18.4: комната отдыха (дверь 24.1..26.2), верхняя кабина (двери 36.4..38.9)
            Wall(walls, "H_Lounge_1", F2CorridorX - t, F2RoomBottom, 24.1f, F2RoomBottom);
            Wall(walls, "H_Lounge_2", 26.2f, F2RoomBottom, 36.4f, F2RoomBottom);
            Wall(walls, "H_Lounge_3", 38.9f, F2RoomBottom, W, F2RoomBottom);
            Wall(walls, "V_TopCab", CabX, F2RoomBottom, CabX, F2CabTop);
            // y 13.5: зал (дверь 17.9..20.1), нижняя кабина (закрыта)
            Wall(walls, "H_Hall_1", F2CorridorX - t, F2HallTop, 17.9f, F2HallTop);
            Wall(walls, "H_Hall_2", 20.1f, F2HallTop, 36.4f, F2HallTop);
            Wall(walls, "H_Hall_3", 38.9f, F2HallTop, W, F2HallTop);
            Wall(walls, "V_LowCab", CabX, F2LowCabBottom - t, CabX, F2HallTop);
            Wall(walls, "H_LowCab", CabX, F2LowCabBottom, W, F2LowCabBottom);

            Transform doors = new GameObject("Doors").transform;
            doors.SetParent(root);
            AutoDoorAt(doors, "Door_Lounge", true, 24.1f, 26.2f, F2RoomBottom);
            AutoDoorAt(doors, "Door_Hall_Top", true, 17.9f, 20.1f, F2HallTop);
            AutoDoorAt(doors, "Door_Hall_Corridor", false, 2f, 3.9f, F2CorridorX);
            AutoDoorAt(doors, "Lift_Top", true, 36.4f, 38.9f, F2RoomBottom);
            Block(doors, "Lift_Low_Closed", new Vector2(36.4f, F2HallTop - 0.26f), new Vector2(38.9f, F2HallTop + 0.26f), SpriteFactory.DoorPanelSci());

            // Обстановка: комната отдыха — диван у верхней стены, длинная скамья у нижней, стопка коробок из-под пиццы
            Transform furniture = new GameObject("Furniture").transform;
            furniture.SetParent(root);
            Prop(furniture, "Sofa", SpriteFactory.ModernSofa(), new Vector2(16.2f, 22.8f), new Vector2(23f, 24.6f), 3, GlassLayer);
            Prop(furniture, "Bench", SpriteFactory.ModernSofa(), new Vector2(16.2f, 18.7f), new Vector2(23.1f, 19.5f), 3, GlassLayer);
            Prop(furniture, "PizzaBoxes", SpriteFactory.PizzaBox(), new Vector2(32.1f, 20.9f), new Vector2(34f, 24.5f), 3, GlassLayer, true, 90f);
            Prop(furniture, "Rug", SpriteFactory.Rug(), new Vector2(24.5f, 20.5f), new Vector2(30f, 23.5f), -97);
            Sprite plant = SpriteFactory.Plant();
            foreach (Vector2 p in new[] { new Vector2(15.4f, 13.9f), new Vector2(33.2f, 14f), new Vector2(15.5f, 0.3f), new Vector2(33.2f, 12.4f) })
            {
                Prop(furniture, "Plant", plant, p, p + new Vector2(1f, 1f), 3);
            }
            NeonSign(furniture, SpriteFactory.NeonSign(), new Vector2(27.8f, 24.55f), Magenta);
            NeonSign(furniture, SpriteFactory.NeonSign(), new Vector2(35.2f, 16f), Cyan, 90f);
            Decor(furniture, "Clutter", SpriteFactory.Clutter(), new Vector2(26f, 6f), 1.4f);
            Decor(furniture, "Bottles", SpriteFactory.Bottles(), new Vector2(29f, 21f), 0.8f);

            // Свет
            Transform lights = new GameObject("Lights").transform;
            lights.SetParent(root);
            Pulsing(Lamp(lights, "Corridor_W", 12.6f, 12.5f, Cold, 14f, 0.55f), 1.1f, 0.4f, 0.6f);
            Pulsing(Lamp(lights, "Lounge", 24.7f, 21.7f, Magenta, 13f, 0.6f), 0.8f, 0.45f, 0.7f);
            Lamp(lights, "Corridor", 26f, 16f, Cold, 12f, 0.55f);
            Lamp(lights, "Hall", 25f, 6f, new Color(1f, 0.72f, 0.45f), 18f, 0.6f);
            Lamp(lights, "Lift_Top", 37.6f, 20.6f, Cyan, 5f, 0.7f);
            Pulsing(Lamp(lights, "Lift_Low", 37.6f, 11.6f, new Color(1f, 0.2f, 0.25f), 4f, 0.5f), 0.6f, 0.2f, 0.6f);
            Light2D marker = AddLight(lights, "LiftMarker", V(37.6f, 20.6f), Cyan, 2.5f, 1.2f);
            Pulsing(marker, 3f, 0.4f, 1.4f);

            // Герой приезжает в верхнюю кабину лицом к дверям
            Transform spawn = new GameObject("Floor2Spawn").transform;
            spawn.SetParent(root);
            spawn.position = V(37.6f, 21f);

            return new FloorRefs
            {
                Enemies = SpawnFloor2Guards(guard, drone, root),
                Spawn = spawn,
                ExitZone = WorldRect(new Vector2(CabX + 0.6f, F2RoomBottom + 0.8f), new Vector2(W - 0.6f, F2CabTop - 0.5f)),
                ExitMarker = marker,
            };
        }

        // Ромбы схемы: стрелки в левом коридоре — два дрона летают вдоль него туда-обратно; в комнате отдыха — трое на постах;
        // в зале — петля на пятерых (одна из точек — дрон). Отступ от стен — как на первом этаже, 1.6 юнита схемы
        private static Health[] SpawnFloor2Guards(GameObject guard, GameObject drone, Transform parent)
        {
            Transform root = new GameObject("Guards_Floor2").transform;
            root.SetParent(parent);
            var guards = new List<Health>();

            Vector2 down = new Vector2(0f, -21.6f) * S;
            guards.Add(SpawnEnemy(drone, root, V(11.8f, 23.3f), -90f, down, DroneSpeed).GetComponent<Health>());
            guards.Add(SpawnEnemy(drone, root, V(13.5f, 23.3f), -90f, down, DroneSpeed).GetComponent<Health>());

            guards.Add(SpawnEnemy(guard, root, V(17.2f, 21.2f), 0f, Vector2.zero, 0f).GetComponent<Health>());
            guards.Add(SpawnEnemy(guard, root, V(30.6f, 23.3f), -150f, Vector2.zero, 0f).GetComponent<Health>());
            guards.Add(SpawnEnemy(guard, root, V(31f, 20f), -135f, Vector2.zero, 0f).GetComponent<Health>());

            // Зал буквой Г: петля вдоль стен в обход нижней кабины, по часовой
            Vector2[] hall = { new(16.8f, 11.9f), new(32.8f, 11.9f), new(32.8f, 7.8f), new(39.2f, 7.8f), new(39.2f, 1.7f), new(16.8f, 1.7f) };
            foreach (int i in new[] { 0, 1, 4, 5 })
            {
                guards.Add(LoopFrom(guard, PatrolSpeed, root, hall, i));
            }
            guards.Add(LoopFrom(drone, DroneSpeed, root, hall, 3));
            return guards.ToArray();
        }

        // Патруль по петле от её вершины from (координаты схемы — через текущий SetLayout)
        private static Health LoopFrom(GameObject prefab, float speed, Transform parent, Vector2[] loop, int from)
        {
            Vector2 start = loop[from];
            var perimeter = new Vector2[loop.Length];
            for (int i = 0; i < loop.Length; i++)
                perimeter[i] = (loop[(from + 1 + i) % loop.Length] - start) * S;
            Vector2 next = loop[(from + 1) % loop.Length];
            float facing = Mathf.Atan2(next.y - start.y, next.x - start.x) * Mathf.Rad2Deg;
            return SpawnPerimeter(prefab, parent, ToWorld(start), facing, speed, perimeter).GetComponent<Health>();
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

        // Группа солдат корпорации на постах (координаты схемы — через текущий SetLayout), лицом вправо, к лифтам.
        // Неактивна до сценария (MissionDirector включает её после отказа отдать relic)
        private static GameObject SpawnCorp(Transform parent, GameObject soldier, string name, params Vector2[] posts)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent);
            foreach (Vector2 p in posts)
            {
                SpawnEnemy(soldier, root, V(p.x, p.y), 0f, Vector2.zero, 0f);
            }
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static void SetObjects(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
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
