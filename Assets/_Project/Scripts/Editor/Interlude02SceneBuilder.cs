using N89.Interlude;
using N89.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Собирает интерлюдию перед миссией 2: та же квартира и все механики интерлюдии 1.
    // Рэй сидит на диване в гарнитуре брейнданса, снимает её и кладёт на столик, оператор пишет
    // ему в мессенджер — кейс с компроматом, — дальше арсенал, дверь, коридор и загрузка миссии 2.
    // Запуск: N89 > Build Interlude 02 Scene. Пересоздаёт Interlude02.unity (и, как интерлюдия 1, префаб Hero и спрайты квартиры).
    public static class Interlude02SceneBuilder
    {
        public const string SceneName = "Interlude02";
        // Фото кейса с компроматом — импортированный арт (в миссии 2 тот же кейс без фона: kompromat_case.png)
        public const string CasePhotoPath = "Assets/_Project/Art/Sprites/Items/kompromat_case_photo.jpg";
        public const string CaseSpritePath = "Assets/_Project/Art/Sprites/Items/kompromat_case.png";

        [MenuItem("N89/Build Interlude 02 Scene")]
        public static void Build()
        {
            InterludeDirector director = Interlude01SceneBuilder.BuildApartment("Interlude02Volume");

            // Телевизор фоном мерцает ярче
            Configure(GameObject.Find("Lights/TV").GetComponent<Pulse>(), so =>
            {
                P(so, "_speed").floatValue = 17f;
                P(so, "_lightMin").floatValue = 0.5f;
                P(so, "_lightMax").floatValue = 1.5f;
            });

            // Камера в начале — диван и столик в одном кадре; на брифинге Рэй справа от панели мессенджера
            Transform gameplay = GameObject.Find("Gameplay").transform;
            Transform openingCamPoint = Interlude01SceneBuilder.Point(gameplay, "OpeningCamPoint", new Vector2(-5.2f, 1.5f), 0f);
            Transform sitPoint = GameObject.Find("Gameplay/SitPoint").transform;
            GameObject.Find("Gameplay/MessageCamPoint").transform.position = new Vector2(-8.8f, 2.8f);

            // Брейнданс: гарнитура на столике (спрятана, пока на голове) и её розовое свечение у головы
            SpriteRenderer headset = CreateSprite("BraindanceHeadset", gameplay, SpriteFactory.BraindanceHeadset(), new Vector2(-5.45f, 1.75f), 6, true, -20f);
            Light2D bdLight = AddLight(gameplay, "BraindanceGlow", (Vector2)sitPoint.position, new Color(1f, 0.3f, 0.7f), 2.2f, 1f);
            BuildBraindanceOverlay(out CanvasGroup overlay, out TMP_Text timecode, out RectTransform progress);

            Configure(director, so =>
            {
                P(so, "_opening").enumValueIndex = (int)InterludeDirector.Opening.Braindance;
                P(so, "_openingCamPoint").objectReferenceValue = openingCamPoint;
                P(so, "_braindanceSprite").objectReferenceValue = SpriteFactory.HeroSittingBraindance();
                P(so, "_headsetOnTable").objectReferenceValue = headset;
                P(so, "_braindanceLight").objectReferenceValue = bdLight;
                P(so, "_braindanceOverlay").objectReferenceValue = overlay;
                P(so, "_braindanceTimecode").objectReferenceValue = timecode;
                P(so, "_braindanceProgress").objectReferenceValue = progress;
                P(so, "_bossName").stringValue = "ОПЕРАТОР";
                P(so, "_title").stringValue = "ИНТЕРЛЮДИЯ II";
                P(so, "_subtitle").stringValue = "Дом";
                P(so, "_mission").intValue = 2;
                P(so, "_missionScene").stringValue = "Mission02";
                P(so, "_missionTitle").stringValue = "МИССИЯ 2";
                P(so, "_missionSubtitle").stringValue = "АРКА СИСТЕМС  //  ЗАБРАТЬ КЕЙС С КОМПРОМАТОМ";
                P(so, "_briefingTopic").stringValue = "ЗАДАНИЕ: КЕЙС С КОМПРОМАТОМ";
                P(so, "_briefingFile").stringValue = "АРКА_СИСТЕМС.loc";
                P(so, "_briefingPhoto").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(CasePhotoPath);
                P(so, "_briefingPhotoCaption").stringValue =
                    "ЦЕЛЬ: КЕЙС\nС КОМПРОМАТОМ\n\nВесь компромат на Корпорацию Е — внутри.\n\nДоставить целым. Не вскрывать.";
                SetStrings(P(so, "_briefing"),
                    // Фото и файл занимают полленты — сообщений влезает два, ленту мессенджер не прокручивает
                    "Рэй. «Арка Системс» собрала компромат на Корпорацию Е.",
                    "Всё лежит в этом кейсе. Найди его и принеси. Без кейса не возвращайся.");
            });

            Interlude01SceneBuilder.SaveScene(SceneName, "Mission01");
        }

        // Экран записи брейнданса: розовый фильтр, по краям тёмная рамка, сверху подпись, снизу таймлайн.
        // Лежит под остальным HUD (летербокс и затемнение — поверх)
        private static void BuildBraindanceOverlay(out CanvasGroup group, out TMP_Text timecode, out RectTransform progress)
        {
            Color pink = new Color(1f, 0.3f, 0.7f);
            TMP_FontAsset mono = MainMenuSceneBuilder.GetOrCreateFontAsset("RobotoMono-Regular");
            RectTransform canvas = GameObject.Find("HUD").GetComponent<RectTransform>();

            RectTransform root = MainMenuSceneBuilder.CreateRect("Braindance", canvas);
            MainMenuSceneBuilder.Stretch(root);
            root.SetAsFirstSibling();
            group = Group(root.gameObject, 0f);
            group.blocksRaycasts = false;

            MainMenuSceneBuilder.Stretch(Img("Tint", root, new Color(1f, 0.15f, 0.55f, 0.16f)).rectTransform);
            // Рамка-виньетка из четырёх полос
            Anchor(Img("Top", root, new Color(0.25f, 0f, 0.15f, 0.55f)).rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(4000f, 70f));
            Anchor(Img("Bottom", root, new Color(0.25f, 0f, 0.15f, 0.55f)).rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(4000f, 70f));
            Anchor(Img("Left", root, new Color(0.25f, 0f, 0.15f, 0.45f)).rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(60f, 4000f));
            Anchor(Img("Right", root, new Color(0.25f, 0f, 0.15f, 0.45f)).rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(60f, 4000f));

            Anchor(Text("Title", root, mono, "BRAINDANCE", 40f, pink, TextAlignmentOptions.TopLeft).rectTransform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -150f), new Vector2(700f, 50f));
            timecode = Text("Timecode", root, mono, string.Empty, 20f, new Color(1f, 0.7f, 0.9f), TextAlignmentOptions.TopLeft);
            Anchor(timecode.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(90f, -205f), new Vector2(800f, 30f));

            // Таймлайн записи
            Image track = Img("Track", root, new Color(0.35f, 0.05f, 0.22f, 0.9f));
            Anchor(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1200f, 8f));
            Image fill = Img("Fill", track.rectTransform, pink);
            progress = fill.rectTransform;
            progress.anchorMin = Vector2.zero;
            progress.anchorMax = new Vector2(0.24f, 1f);
            progress.offsetMin = Vector2.zero;
            progress.offsetMax = Vector2.zero;
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
