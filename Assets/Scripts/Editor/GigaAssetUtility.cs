#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Buildings;

namespace Game.Editor
{
    internal static class GigaAssetUtility
    {
        public const int BuildingLayer = 7;

        public static void EnsureFolder(string assetFolderPath)
        {
            string[] parts = assetFolderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        public static Sprite EnsureGeneratedSprite(string assetPath, bool circle, Color fillColor)
        {
            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir)) EnsureFolder(dir);

            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null)
            {
                if (AssetImporter.GetAtPath(assetPath) is TextureImporter existingImp)
                {
                    var existingSettings = new TextureImporterSettings();
                    existingImp.ReadTextureSettings(existingSettings);
                    if (existingSettings.spriteMeshType != SpriteMeshType.FullRect)
                    {
                        existingSettings.spriteMeshType = SpriteMeshType.FullRect;
                        existingImp.SetTextureSettings(existingSettings);
                        existingImp.SaveAndReimport();
                    }
                }
                return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = (size - 1) * 0.5f;
            float maxR = size * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = Color.clear;
                    if (circle)
                    {
                        float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                        if (d <= maxR)
                        {
                            c = d >= maxR - 3f ? new Color(1f, 1f, 1f, 0.9f) : fillColor;
                        }
                    }
                    else
                    {
                        bool border = x < 3 || x >= size - 3 || y < 3 || y >= size - 3;
                        bool innerBorder = x == 3 || x == size - 4 || y == 3 || y == size - 4;
                        if (border) c = new Color(0.12f, 0.14f, 0.18f, 1f);
                        else if (innerBorder) c = new Color(1f, 1f, 1f, 0.75f);
                        else c = fillColor;
                    }
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply();
            string fullDiskPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            File.WriteAllBytes(fullDiskPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        public static T GetOrCreateSO<T>(string assetPath) where T : ScriptableObject
        {
            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir)) EnsureFolder(dir);

            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        public static void GetReferenceSpriteMaterialAndSorting(out Material mat, out int sortingLayerId, out int sortingOrder)
        {
            mat = null;
            sortingLayerId = 0;
            sortingOrder = 10;

            var refPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Building_GoldMine.prefab");
            if (refPrefab != null && refPrefab.TryGetComponent<SpriteRenderer>(out var sr))
            {
                mat = sr.sharedMaterial;
                sortingLayerId = sr.sortingLayerID;
                sortingOrder = sr.sortingOrder;
            }
        }

        public static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        public static T GetOrAddComponent<T>(Component host) where T : Component =>
            host != null ? GetOrAddComponent<T>(host.gameObject) : null;

        public static BuildingData SaveBuildingAndPrefab<TBuilding>(
            string dataPath,
            string prefabPath,
            string id,
            string displayName,
            string description,
            BuildingCategory category,
            Vector2Int size,
            BuildingUpgradeType upgradeType,
            bool isDestructible,
            BuildingLevelStats[] levels,
            Sprite baseSprite,
            Color tint,
            System.Action<TBuilding> configureComponent = null) where TBuilding : Building
        {
            BuildingData data = GetOrCreateSO<BuildingData>(dataPath);

            EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/') ?? "Assets/Prefabs/Buildings");
            GetReferenceSpriteMaterialAndSorting(out Material refMat, out int sortingLayerId, out int sortingOrder);

            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject workingRoot = existingPrefab != null
                ? PrefabUtility.LoadPrefabContents(prefabPath)
                : new GameObject(Path.GetFileNameWithoutExtension(prefabPath));

            TBuilding buildingComp;
            try
            {
                workingRoot.layer = BuildingLayer;

                var sr = GetOrAddComponent<SpriteRenderer>(workingRoot);
                sr.sprite = baseSprite;
                sr.color = tint;
                if (refMat != null) sr.sharedMaterial = refMat;
                sr.sortingLayerID = sortingLayerId;
                sr.sortingOrder = sortingOrder;

                var box = GetOrAddComponent<BoxCollider2D>(workingRoot);
                box.offset = Vector2.zero;
                box.size = new Vector2(size.x * 0.9f, size.y * 0.9f);

                // Ensure desired concrete Building subclass is present
                var existingBuildings = workingRoot.GetComponents<Building>();
                buildingComp = workingRoot.GetComponent<TBuilding>();
                if (buildingComp == null)
                {
                    for (int i = 0; i < existingBuildings.Length; i++)
                    {
                        Object.DestroyImmediate(existingBuildings[i]);
                    }
                    buildingComp = workingRoot.AddComponent<TBuilding>();
                }

                // Scale visual child cleanly for 2x2 vs 1x1 without distorting colliders (or scale sprite root cleanly)
                float visualScale = size.x > 1 || size.y > 1 ? 1.85f : 0.92f;
                workingRoot.transform.localScale = Vector3.one;
                sr.drawMode = SpriteDrawMode.Simple;

                configureComponent?.Invoke(buildingComp);

                var bSo = new SerializedObject(buildingComp);
                bSo.FindProperty("_data").objectReferenceValue = data;
                bSo.FindProperty("_spriteRenderer").objectReferenceValue = sr;
                bSo.FindProperty("_modelTransform").objectReferenceValue = workingRoot.transform;
                bSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(workingRoot, prefabPath);
            }
            finally
            {
                if (existingPrefab != null) PrefabUtility.UnloadPrefabContents(workingRoot);
                else Object.DestroyImmediate(workingRoot);
            }

            GameObject savedPrefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Building savedPrefabComp = savedPrefabGo != null ? savedPrefabGo.GetComponent<TBuilding>() : null;

            data.InitializeRuntime(id, displayName, description, category, size, upgradeType, levels, savedPrefabComp, baseSprite);
            data.SetDestructible(isDestructible);

            var dataSo = new SerializedObject(data);
            dataSo.FindProperty("_placedSaleRefundRatio").floatValue = 0.5f;
            dataSo.FindProperty("_unplacedSaleRefundRatio").floatValue = 0.5f;
            dataSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);

            return data;
        }

        // --- UI Helpers ---

        public static TMP_FontAsset FindCyrillicFont(Transform root)
        {
            TMP_Text sample = root != null ? root.GetComponentInChildren<TMP_Text>(true) : null;
            return sample != null && sample.font != null ? sample.font : TMP_Settings.defaultFontAsset;
        }

        public static void SetAnchorY(Transform target, float anchorY)
        {
            if (target is RectTransform rt)
            {
                rt.anchorMin = new Vector2(rt.anchorMin.x, anchorY);
                rt.anchorMax = new Vector2(rt.anchorMax.x, anchorY);
                rt.anchoredPosition = Vector2.zero;
            }
        }

        public static RectTransform GetOrCreateRect(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing is RectTransform rt ? rt : GetOrAddComponent<RectTransform>(existing.gameObject);
            }
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static TMP_Text CreateOrUpdateLabel(
            Transform parent,
            string name,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            float fontSize,
            Color color,
            TMP_FontAsset font,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            RectTransform rt = GetOrCreateRect(parent, name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var tmp = GetOrAddComponent<TextMeshProUGUI>(rt);
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static (Button button, TMP_Text label) CreateOrUpdateButton(
            Transform parent,
            string name,
            string text,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPos,
            Vector2 sizeDelta,
            float fontSize,
            Color bgColor,
            TMP_FontAsset font)
        {
            RectTransform rt = GetOrCreateRect(parent, name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;

            var img = GetOrAddComponent<Image>(rt);
            img.color = bgColor;
            var btn = GetOrAddComponent<Button>(rt);
            btn.targetGraphic = img;

            TMP_Text tmp = CreateOrUpdateLabel(rt, "Text", text, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, fontSize, Color.white, font);
            return (btn, tmp);
        }

        public static Slider CreateOrUpdateSlider(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Color fillColor)
        {
            RectTransform rt = GetOrCreateRect(parent, name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = sizeDelta;

            var slider = GetOrAddComponent<Slider>(rt);
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // Background
            RectTransform bgRt = GetOrCreateRect(rt, "Background");
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = GetOrAddComponent<Image>(bgRt);
            bgImg.color = new Color(0.16f, 0.18f, 0.22f, 1f);

            // Fill Area -> Fill
            RectTransform fillArea = GetOrCreateRect(rt, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = new Vector2(4f, 0f);
            fillArea.offsetMax = new Vector2(-4f, 0f);

            RectTransform fillRt = GetOrCreateRect(fillArea, "Fill");
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = GetOrAddComponent<Image>(fillRt);
            fillImg.color = fillColor;

            // Handle Slide Area -> Handle
            RectTransform handleArea = GetOrCreateRect(rt, "Handle Slide Area");
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(10f, 0f);
            handleArea.offsetMax = new Vector2(-10f, 0f);

            RectTransform handleRt = GetOrCreateRect(handleArea, "Handle");
            handleRt.anchorMin = new Vector2(0.5f, 0.5f);
            handleRt.anchorMax = new Vector2(0.5f, 0.5f);
            handleRt.anchoredPosition = Vector2.zero;
            handleRt.sizeDelta = new Vector2(20f, 28f);
            var handleImg = GetOrAddComponent<Image>(handleRt);
            handleImg.color = Color.white;

            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            return slider;
        }
    }
}
#endif
