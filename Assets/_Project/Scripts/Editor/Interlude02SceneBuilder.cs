using N89.Interlude;
using N89.World;
using UnityEditor;
using UnityEngine;
using static N89.EditorTools.TutorialSceneBuilder;

namespace N89.EditorTools
{
    // Собирает интерлюдию перед миссией 2: та же квартира и все механики интерлюдии 1.
    // Рэй сидит на диване и смотрит телевизор, босс пишет ему в мессенджер — компромат на
    // конкурентов, — дальше арсенал, дверь, коридор и загрузка миссии 2.
    // Запуск: N89 > Build Interlude 02 Scene. Пересоздаёт Interlude02.unity (и, как интерлюдия 1, префаб Hero и спрайты квартиры).
    public static class Interlude02SceneBuilder
    {
        public const string SceneName = "Interlude02";

        [MenuItem("N89/Build Interlude 02 Scene")]
        public static void Build()
        {
            InterludeDirector director = Interlude01SceneBuilder.BuildApartment("Interlude02Volume");

            // Телевизор мерцает ярче: он единственное, что сейчас происходит в квартире
            Configure(GameObject.Find("Lights/TV").GetComponent<Pulse>(), so =>
            {
                P(so, "_speed").floatValue = 17f;
                P(so, "_lightMin").floatValue = 0.5f;
                P(so, "_lightMax").floatValue = 1.5f;
            });

            // Камера в начале — диван и телевизор в одном кадре; на брифинге Рэй справа от панели мессенджера
            Transform gameplay = GameObject.Find("Gameplay").transform;
            Transform tvCamPoint = Interlude01SceneBuilder.Point(gameplay, "TvCamPoint", new Vector2(-5.2f, 0f), 0f);
            GameObject.Find("Gameplay/MessageCamPoint").transform.position = new Vector2(-8.8f, 2.8f);

            Configure(director, so =>
            {
                P(so, "_opening").enumValueIndex = (int)InterludeDirector.Opening.WatchTv;
                P(so, "_tvCamPoint").objectReferenceValue = tvCamPoint;
                P(so, "_title").stringValue = "ИНТЕРЛЮДИЯ II";
                P(so, "_subtitle").stringValue = "Дом";
                P(so, "_mission").intValue = 2;
                P(so, "_missionScene").stringValue = "Mission02";
                P(so, "_missionTitle").stringValue = "МИССИЯ 2";
                P(so, "_missionSubtitle").stringValue = "АРКА СИСТЕМС  //  ДОБЫТЬ КОМПРОМАТ";
                P(so, "_briefingTopic").stringValue = "ЗАДАНИЕ: КОМПРОМАТ";
                P(so, "_briefingFile").stringValue = "АРКА_СИСТЕМС.loc";
                SetStrings(P(so, "_briefing"),
                    // Влезает 4 сообщения средней длины — ленту мессенджер не прокручивает
                    "Рэй. Есть дело.",
                    "«Арка Системс», дочка второго монополиста синей крови, копает под Корпорацию Е. Нужен компромат с их серверов.",
                    "Здание посреди города. На вид — обычный офис, но защита там серьёзнее, чем у нас в штабе.",
                    "Местоположение — в файле. И, Рэй... что бы ты там ни увидел — не отвлекайся.");
            });

            Interlude01SceneBuilder.SaveScene(SceneName, "Mission01");
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
