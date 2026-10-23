using System.Collections.Generic;
using System.IO;
using N89.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace N89.EditorTools
{
    // Собирает сцену главного меню с нуля: шрифты, материалы, пост-обработку, Canvas и меню.
    // Запуск: N89 > Build Main Menu Scene. Пересоздаёт MainMenu.unity — ручные правки сцены затрутся.
    public static class MainMenuSceneBuilder
    {
        private const string Root = "Assets/_Project";
        private const string FontsDir = Root + "/Art/Fonts";
        private const string MaterialsDir = Root + "/Art/Materials";
        private const string SettingsDir = Root + "/Settings";
        private const string ScenePath = Root + "/Scenes/MainMenu.unity";

        private static readonly Vector2 RefResolution = new Vector2(1920, 1080);

        [MenuItem("N89/Build Main Menu Scene")]
        public static void Build()
        {
            EnsureFolder(MaterialsDir);
            EnsureFolder(SettingsDir);
            EnsureFolder(Root + "/Scenes");

            TMP_FontAsset monoton = GetOrCreateFontAsset("Monoton-Regular");
            TMP_FontAsset audiowide = GetOrCreateFontAsset("Audiowide-Regular");
            TMP_FontAsset marker = GetOrCreateFontAsset("PermanentMarker-Regular");

            Material bgMaterial = GetOrCreateMaterial("MenuBackground", new Material(Shader.Find("N89/UI/MenuBackground")));
            Material titleNeon = CreateNeonMaterial("TitleNeon", monoton, new Color(1f, 0.15f, 0.75f, 0.55f), 1.6f);
            Material markerNeon = CreateNeonMaterial("MarkerNeon", marker, new Color(1f, 0.3f, 0.85f, 0.6f), 1.4f);
            VolumeProfile profile = CreateVolumeProfile();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera cam = CreateCamera();
            CreateVolume(profile);
            CreateEventSystem();

            Canvas canvas = CreateCanvas(cam);
            RectTransform canvasRect = (RectTransform)canvas.transform;

            // Фон
            RawImage bg = Create<RawImage>("Background", canvasRect);
            Stretch(bg.rectTransform);
            bg.material = bgMaterial;
            bg.raycastTarget = false;

            // Заголовок: слой тени, цветное эхо и основной неон
            RectTransform titleGroup = CreateRect("Title", canvasRect);
            titleGroup.anchoredPosition = new Vector2(0f, 190f);
            titleGroup.sizeDelta = new Vector2(1200f, 420f);
            CanvasGroup titleCanvasGroup = titleGroup.gameObject.AddComponent<CanvasGroup>();
            titleCanvasGroup.blocksRaycasts = false;

            CreateTitleLayer("Shadow", titleGroup, monoton, marker, null, null,
                new Color(0.04f, 0f, 0.07f, 0.85f), new Vector2(16f, -16f), -0.3f, false);
            CreateTitleLayer("Echo", titleGroup, monoton, marker, null, null,
                new Color(0.15f, 0.9f, 1f, 0.3f), new Vector2(-10f, 8f), -0.12f, false);
            CreateTitleLayer("Neon", titleGroup, monoton, marker, titleNeon, markerNeon,
                Color.white, Vector2.zero, 0f, true);

            // Меню
            RectTransform menuRect = CreateRect("Menu", canvasRect);
            menuRect.anchoredPosition = new Vector2(0f, -230f);
            menuRect.sizeDelta = new Vector2(560f, 240f);
            CanvasGroup menuCanvasGroup = menuRect.gameObject.AddComponent<CanvasGroup>();

            Image highlight = Create<Image>("Highlight", menuRect);
            highlight.rectTransform.sizeDelta = new Vector2(560f, 60f);
            highlight.color = new Color(0f, 0f, 0f, 0.5f);
            highlight.raycastTarget = false;

            var items = new List<MenuItemView>
            {
                CreateMenuItem(menuRect, "NEW GAME", MenuAction.NewGame, 0f, audiowide),
                CreateMenuItem(menuRect, "OPTIONS", MenuAction.Options, -66f, audiowide),
                CreateMenuItem(menuRect, "EXIT", MenuAction.Exit, -132f, audiowide),
            };
            // Пункты центрируем вокруг середины блока
            foreach (MenuItemView item in items)
            {
                item.Rect.anchoredPosition += new Vector2(0f, 66f);
            }

            // Версия в правом нижнем углу
            TextMeshProUGUI version = CreateText("Version", canvasRect, audiowide, "<N89>\nv0.1", 20f, new Color(1f, 1f, 1f, 0.45f));
            version.alignment = TextAlignmentOptions.BottomRight;
            version.rectTransform.anchorMin = version.rectTransform.anchorMax = new Vector2(1f, 0f);
            version.rectTransform.pivot = new Vector2(1f, 0f);
            version.rectTransform.anchoredPosition = new Vector2(-24f, 20f);
            version.rectTransform.sizeDelta = new Vector2(300f, 60f);

            MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>();
            var so = new SerializedObject(controller);
            SerializedProperty itemsProp = so.FindProperty("_items");
            itemsProp.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
            so.FindProperty("_highlight").objectReferenceValue = highlight.rectTransform;
            so.FindProperty("_titleGroup").objectReferenceValue = titleGroup;
            so.FindProperty("_titleCanvasGroup").objectReferenceValue = titleCanvasGroup;
            so.FindProperty("_menuCanvasGroup").objectReferenceValue = menuCanvasGroup;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettingsFirst(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[N89] Главное меню собрано: {ScenePath}");
        }

        private static void CreateTitleLayer(string name, RectTransform parent, TMP_FontAsset monoton, TMP_FontAsset marker,
            Material digitsMaterial, Material letterMaterial, Color color, Vector2 offset, float phase, bool isMain)
        {
            RectTransform layer = CreateRect(name, parent);
            layer.sizeDelta = parent.sizeDelta;
            layer.anchoredPosition = offset;

            TitleSway sway = layer.gameObject.AddComponent<TitleSway>();
            var swaySo = new SerializedObject(sway);
            swaySo.FindProperty("_phaseOffset").floatValue = phase;
            swaySo.FindProperty("_glitch").boolValue = isMain;
            swaySo.ApplyModifiedPropertiesWithoutUndo();

            TextMeshProUGUI digits = CreateText("89", layer, monoton, "89", 300f, color);
            digits.rectTransform.sizeDelta = new Vector2(900f, 400f);
            digits.rectTransform.anchoredPosition = new Vector2(110f, -10f);
            digits.characterSpacing = -10f;
            if (digitsMaterial != null)
            {
                digits.fontSharedMaterial = digitsMaterial;
            }

            NeonTextEffect digitsFx = digits.gameObject.AddComponent<NeonTextEffect>();
            var fxSo = new SerializedObject(digitsFx);
            fxSo.FindProperty("_animateColors").boolValue = isMain;
            fxSo.FindProperty("_flickerChancePerSecond").floatValue = isMain ? 0.8f : 0f;
            fxSo.ApplyModifiedPropertiesWithoutUndo();

            TextMeshProUGUI letter = CreateText("N", layer, marker, "N", 280f, isMain ? new Color(1f, 0.88f, 0.98f) : color);
            letter.rectTransform.sizeDelta = new Vector2(400f, 400f);
            letter.rectTransform.anchoredPosition = new Vector2(-235f, 45f);
            letter.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            if (letterMaterial != null)
            {
                letter.fontSharedMaterial = letterMaterial;
            }

            NeonTextEffect letterFx = letter.gameObject.AddComponent<NeonTextEffect>();
            var letterSo = new SerializedObject(letterFx);
            letterSo.FindProperty("_animateColors").boolValue = false;
            letterSo.FindProperty("_waveAmplitude").floatValue = 0f;
            letterSo.FindProperty("_flickerChancePerSecond").floatValue = isMain ? 0.5f : 0f;
            letterSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MenuItemView CreateMenuItem(RectTransform parent, string label, MenuAction action, float y, TMP_FontAsset font)
        {
            Image hitArea = Create<Image>(label, parent);
            hitArea.color = new Color(0f, 0f, 0f, 0f);
            hitArea.rectTransform.sizeDelta = new Vector2(560f, 60f);
            hitArea.rectTransform.anchoredPosition = new Vector2(0f, y);

            TextMeshProUGUI text = CreateText("Label", hitArea.rectTransform, font, label, 38f, Color.white);
            Stretch(text.rectTransform);
            text.raycastTarget = false;
            text.characterSpacing = 6f;

            MenuItemView view = hitArea.gameObject.AddComponent<MenuItemView>();
            var so = new SerializedObject(view);
            so.FindProperty("_action").enumValueIndex = (int)action;
            so.FindProperty("_label").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            go.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = go.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            return cam;
        }

        private static void CreateVolume(VolumeProfile profile)
        {
            var go = new GameObject("Global Volume");
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas CreateCanvas(Camera cam)
        {
            var go = new GameObject("MainMenu", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = RefResolution;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static VolumeProfile CreateVolumeProfile()
        {
            string path = SettingsDir + "/MainMenuVolume.asset";
            AssetDatabase.DeleteAsset(path);
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.8f);
            bloom.intensity.Override(1.6f);
            bloom.scatter.Override(0.75f);

            ChromaticAberration ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.15f);

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.3f);
            vignette.smoothness.Override(0.5f);

            FilmGrain grain = profile.Add<FilmGrain>(true);
            grain.intensity.Override(0.3f);

            foreach (VolumeComponent component in profile.components)
            {
                component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            EditorUtility.SetDirty(profile);
            return profile;
        }

        internal static TMP_FontAsset GetOrCreateFontAsset(string fontName)
        {
            string assetPath = $"{FontsDir}/{fontName} SDF.asset";
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset == null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontsDir}/{fontName}.ttf");
                fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                fontAsset.name = fontName + " SDF";
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                fontAsset.atlasTexture.name = fontName + " Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
                fontAsset.material.name = fontName + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            // Заранее запекаем латиницу и цифры, чтобы текст сразу был виден в редакторе
            fontAsset.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789<>.,:!?-' ");
            EditorUtility.SetDirty(fontAsset);
            return fontAsset;
        }

        private static Material CreateNeonMaterial(string name, TMP_FontAsset font, Color glow, float faceBoost)
        {
            var mat = new Material(font.material);
            mat.EnableKeyword("GLOW_ON");
            mat.SetColor("_GlowColor", glow);
            mat.SetFloat("_GlowOffset", 0f);
            mat.SetFloat("_GlowInner", 0.05f);
            mat.SetFloat("_GlowOuter", 0.45f);
            mat.SetFloat("_GlowPower", 0.75f);
            // Цвет лица больше единицы — в HDR буквы светятся сильнее и цепляют Bloom
            mat.SetColor("_FaceColor", new Color(faceBoost, faceBoost, faceBoost, 1f));
            return GetOrCreateMaterial(name, mat);
        }

        internal static Material GetOrCreateMaterial(string name, Material template)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = template.shader;
                existing.CopyPropertiesFromMaterial(template);
                existing.shaderKeywords = template.shaderKeywords;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(template, path);
            return template;
        }

        private static void AddSceneToBuildSettingsFirst(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        internal static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, string value, float size, Color color)
        {
            TextMeshProUGUI text = Create<TextMeshProUGUI>(name, parent);
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        internal static T Create<T>(string name, RectTransform parent) where T : Component
        {
            RectTransform rect = CreateRect(name, parent);
            return rect.gameObject.AddComponent<T>();
        }

        internal static RectTransform CreateRect(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
