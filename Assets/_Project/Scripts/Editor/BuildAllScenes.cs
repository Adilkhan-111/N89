using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace N89.EditorTools
{
    // Пересобирает все сцены разом, в порядке прохождения: меню, туториал, интерлюдии и миссии.
    // Пересборка затирает ручные правки сцен — перед запуском спрашивает подтверждение.
    // Запуск: N89 > Build All Scenes.
    public static class BuildAllScenes
    {
        [MenuItem("N89/Build All Scenes", priority = 0)]
        public static void Build()
        {
            if (!EditorUtility.DisplayDialog("N89 — пересобрать все сцены",
                    "MainMenu, Tutorial, Interlude01, Mission01, Interlude02, Mission02 будут пересозданы.\n" +
                    "Ручные правки в этих сценах пропадут.", "Пересобрать", "Отмена"))
            {
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            (string name, System.Action build)[] steps =
            {
                ("MainMenu", MainMenuSceneBuilder.Build),
                ("Tutorial", TutorialSceneBuilder.Build),
                ("Interlude01", Interlude01SceneBuilder.Build),
                ("Mission01", Mission01SceneBuilder.Build),
                ("Interlude02", Interlude02SceneBuilder.Build),
                ("Mission02", Mission02SceneBuilder.Build),
            };

            try
            {
                for (int i = 0; i < steps.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("N89 — пересборка сцен", steps[i].name, (float)i / steps.Length);
                    steps[i].build();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[N89] Все сцены пересобраны");
        }
    }
}
