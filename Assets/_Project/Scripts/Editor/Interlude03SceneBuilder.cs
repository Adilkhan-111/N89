using N89.Interlude;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Собирает интерлюдию перед миссией 3: та же квартира и все механики интерлюдий 1–2.
    // Рэй у раковины смотрит в зеркало, разбивает его кулаком, босс пишет в мессенджер —
    // реликвия «Арка Системс», — дальше арсенал (Сандевистан + имплант на руки на выбор),
    // дверь, подъезд, улица и загрузка миссии 3.
    // Запуск: N89 > Build Interlude 03 Scene. Пересоздаёт Interlude03.unity (и, как интерлюдия 1, префаб Hero и спрайты квартиры).
    public static class Interlude03SceneBuilder
    {
        public const string SceneName = "Interlude03";

        [MenuItem("N89/Build Interlude 03 Scene")]
        public static void Build()
        {
            InterludeDirector director = Interlude01SceneBuilder.BuildApartment("Interlude03Volume");

            // Рэй у раковины лицом к зеркалу (зеркало на восточной стене ванной)
            Transform gameplay = GameObject.Find("Gameplay").transform;
            Transform mirrorPoint = Interlude01SceneBuilder.Point(gameplay, "MirrorPoint", new Vector2(7.45f, -2.6f), 0f);
            SpriteRenderer mirror = GameObject.Find("SinkMirror").GetComponent<SpriteRenderer>();
            Light2D glitch = AddLight(GameObject.Find("Lights").transform, "MirrorGlitch", new Vector2(7.6f, -2.6f), new Color(1f, 0.1f, 0.2f), 3.5f, 1.6f);
            glitch.enabled = false;

            Configure(director, so =>
            {
                P(so, "_opening").enumValueIndex = (int)InterludeDirector.Opening.Mirror;
                P(so, "_mirrorPoint").objectReferenceValue = mirrorPoint;
                P(so, "_mirror").objectReferenceValue = mirror;
                P(so, "_brokenMirrorSprite").objectReferenceValue = SpriteFactory.SinkMirrorBroken();
                P(so, "_shardSprite").objectReferenceValue = SpriteFactory.MirrorShard();
                P(so, "_glitchLight").objectReferenceValue = glitch;
                P(so, "_bossName").stringValue = "БОСС";
                P(so, "_title").stringValue = "ИНТЕРЛЮДИЯ III";
                P(so, "_subtitle").stringValue = "Отражение";
                P(so, "_mission").intValue = 3;
                P(so, "_missionScene").stringValue = "Mission03";
                P(so, "_missionTitle").stringValue = "МИССИЯ 3";
                P(so, "_missionSubtitle").stringValue = "АРКА СИСТЕМС  //  ЗАБРАТЬ РЕЛИКВИЮ, УБИТЬ ВСЕХ";
                P(so, "_briefingTopic").stringValue = "ЗАДАНИЕ: РЕЛИКВИЯ";
                P(so, "_briefingFile").stringValue = "АРКА_СИСТЕМС.loc";
                SetStrings(P(so, "_briefing"),
                    "Рэй. Работа. Самая важная из всех, что у тебя были.",
                    "«Арка Системс» — дочка наших конкурентов — достала реликвию. Внутри конструкт. Чей — не знаем даже мы.",
                    "Реликвия в хранилище лаборатории, на верхнем этаже их башни. Четыре этажа охраны, камер и дронов. Свидетелей не оставляй.",
                    "Взлом там не поможет. В арсенале тебя ждёт Сандевистан. Не сгори.");
            });

            Interlude01SceneBuilder.SaveScene(SceneName, "Mission02");
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
