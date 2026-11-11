#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Game.Buildings;
using Game.Core;
using Game.Data;
using Game.Spells;
using Game.UI;
using Game.UI.MainMenu;
using Game.UI.Shop;
using Game.Upgrades;

namespace Game.Editor
{
    public static class GigaContentGenerator
    {
        [MenuItem("Tools/GIGA Generate All Content")]
        public static string GenerateAllContent()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("=== GIGA Generate All Content ===");

            GigaContentDataGenerator.GeneratedContent data = GigaContentDataGenerator.GenerateAllData(log);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireHudPrefab(data, log);
            AssetDatabase.SaveAssets();

            WireSampleScene(data, log);
            WireMainMenuScene(data, log);

            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            log.AppendLine("=== GIGA Content Generation Complete ===");
            Debug.Log(log.ToString());
            return log.ToString();
        }

        private static void WireHudPrefab(GigaContentDataGenerator.GeneratedContent data, System.Text.StringBuilder log)
        {
            const string hudPath = "Assets/Prefabs/HUD.prefab";
            GameObject hudRoot = PrefabUtility.LoadPrefabContents(hudPath);
            if (hudRoot == null)
            {
                log.AppendLine($"[WARN] Could not load {hudPath}");
                return;
            }

            try
            {
                Transform ui = hudRoot.transform.Find("UI");
                if (ui == null)
                {
                    log.AppendLine("[WARN] HUD/UI child missing!");
                    return;
                }

                TMP_FontAsset font = GigaAssetUtility.FindCyrillicFont(ui);

                WireWaveTimerButton(ui, font, log);
                WireUpgradeSelectionModal(ui, font, log);
                WirePauseAudioSliders(ui, font, log);
                WireSpellPanel(ui, font, log);
                WireInRunShopModal(ui, data.PoolConfig, font, log);

                PrefabUtility.SaveAsPrefabAsset(hudRoot, hudPath);
                log.AppendLine("[HUD] Saved updated HUD.prefab.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hudRoot);
            }
        }

        private static void WireWaveTimerButton(Transform ui, TMP_FontAsset font, System.Text.StringBuilder log)
        {
            var waveTimerUi = ui.GetComponentInChildren<WaveTimerUI>(true);
            if (waveTimerUi == null) return;

            var (startBtn, _) = GigaAssetUtility.CreateOrUpdateButton(
                ui,
                "StartWaveBtn",
                "Начать волну",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-210f, -34f),
                new Vector2(260f, 44f),
                20f,
                new Color(0.18f, 0.56f, 0.28f, 1f),
                font);

            var so = new SerializedObject(waveTimerUi);
            so.FindProperty("_startWaveButton").objectReferenceValue = startBtn;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(waveTimerUi);
            log.AppendLine("[HUD] Wired WaveTimerUI._startWaveButton top-right.");
        }

        private static void WireUpgradeSelectionModal(Transform ui, TMP_FontAsset font, System.Text.StringBuilder log)
        {
            Transform panel = ui.Find("UpgradeSelectionPanel");
            if (panel == null) return;

            var selUi = panel.GetComponent<UpgradeSelectionUI>();
            if (selUi == null) return;

            // Keep UpgradeSelectionPanel active at root, controlled cleanly via CanvasGroup (alpha=0 when closed)
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            RectTransform cardsStage = GigaAssetUtility.GetOrCreateRect(panel, "CardsStage");
            cardsStage.anchorMin = Vector2.zero;
            cardsStage.anchorMax = Vector2.one;
            cardsStage.offsetMin = Vector2.zero;
            cardsStage.offsetMax = Vector2.zero;

            string[] toReparent = { "Header", "CardsContainer", "SkipButton" };
            for (int i = 0; i < toReparent.Length; i++)
            {
                Transform child = panel.Find(toReparent[i]);
                if (child != null) child.SetParent(cardsStage, false);
            }

            // Chest stage
            RectTransform chestStage = GigaAssetUtility.GetOrCreateRect(panel, "ChestStage");
            chestStage.anchorMin = new Vector2(0.5f, 0.5f);
            chestStage.anchorMax = new Vector2(0.5f, 0.5f);
            chestStage.anchoredPosition = Vector2.zero;
            chestStage.sizeDelta = new Vector2(480f, 240f);
            var chestImg = GigaAssetUtility.GetOrAddComponent<Image>(chestStage);
            chestImg.color = new Color(0.28f, 0.18f, 0.08f, 0.96f);
            var chestBtn = GigaAssetUtility.GetOrAddComponent<Button>(chestStage);
            chestBtn.targetGraphic = chestImg;
            TMP_Text chestPrompt = GigaAssetUtility.CreateOrUpdateLabel(
                chestStage,
                "PromptText",
                "НАГРАДА ЗАМКА!\n<size=68%>Нажмите на сундук, чтобы открыть</size>",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                new Vector2(-24f, -24f),
                26f,
                new Color(1f, 0.88f, 0.35f, 1f),
                font);
            chestStage.gameObject.SetActive(false);

            UpgradeCardUI[] cards = cardsStage.GetComponentsInChildren<UpgradeCardUI>(true);
            Transform skipTf = cardsStage.Find("SkipButton");
            Button skipBtn = skipTf != null ? skipTf.GetComponent<Button>() : null;

            var so = new SerializedObject(selUi);
            so.FindProperty("_panelRoot").objectReferenceValue = null; // CanvasGroup handles hiding cleanly without unsubscribing Awake
            so.FindProperty("_chestStageRoot").objectReferenceValue = chestStage.gameObject;
            so.FindProperty("_chestButton").objectReferenceValue = chestBtn;
            so.FindProperty("_chestPromptText").objectReferenceValue = chestPrompt;
            so.FindProperty("_cardsStageRoot").objectReferenceValue = cardsStage.gameObject;
            if (skipBtn != null) so.FindProperty("_skipButton").objectReferenceValue = skipBtn;
            var cardsProp = so.FindProperty("_cards");
            cardsProp.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(selUi);
            log.AppendLine($"[HUD] Wired UpgradeSelectionUI ChestStage + CardsStage ({cards.Length} cards).");
        }

        private static void WirePauseAudioSliders(Transform ui, TMP_FontAsset font, System.Text.StringBuilder log)
        {
            Transform pausePanel = ui.Find("PausePanel");
            if (pausePanel == null) return;
            var pauseUi = pausePanel.GetComponent<PauseUI>();
            if (pauseUi == null) return;

            GigaAssetUtility.CreateOrUpdateLabel(pausePanel, "MusicLabel", "Музыка",
                new Vector2(0.37f, 0.24f), new Vector2(0.37f, 0.24f), Vector2.zero, new Vector2(140f, 34f), 18f, Color.white, font, TextAlignmentOptions.Right);
            Slider musicSlider = GigaAssetUtility.CreateOrUpdateSlider(pausePanel, "MusicSlider",
                new Vector2(0.52f, 0.24f), new Vector2(0.52f, 0.24f), new Vector2(240f, 28f), new Color(0.25f, 0.72f, 0.95f, 1f));
            TMP_Text musicVal = GigaAssetUtility.CreateOrUpdateLabel(pausePanel, "MusicValue", "80%",
                new Vector2(0.64f, 0.24f), new Vector2(0.64f, 0.24f), Vector2.zero, new Vector2(70f, 34f), 18f, Color.white, font, TextAlignmentOptions.Left);

            GigaAssetUtility.CreateOrUpdateLabel(pausePanel, "SfxLabel", "Звуки",
                new Vector2(0.37f, 0.16f), new Vector2(0.37f, 0.16f), Vector2.zero, new Vector2(140f, 34f), 18f, Color.white, font, TextAlignmentOptions.Right);
            Slider sfxSlider = GigaAssetUtility.CreateOrUpdateSlider(pausePanel, "SfxSlider",
                new Vector2(0.52f, 0.16f), new Vector2(0.52f, 0.16f), new Vector2(240f, 28f), new Color(0.95f, 0.68f, 0.22f, 1f));
            TMP_Text sfxVal = GigaAssetUtility.CreateOrUpdateLabel(pausePanel, "SfxValue", "80%",
                new Vector2(0.64f, 0.16f), new Vector2(0.64f, 0.16f), Vector2.zero, new Vector2(70f, 34f), 18f, Color.white, font, TextAlignmentOptions.Left);

            var so = new SerializedObject(pauseUi);
            so.FindProperty("_musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("_sfxSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("_musicValueText").objectReferenceValue = musicVal;
            so.FindProperty("_sfxValueText").objectReferenceValue = sfxVal;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pauseUi);
            log.AppendLine("[HUD] Added Music & SFX sliders to PauseUI.");
        }

        private static void WireSpellPanel(Transform ui, TMP_FontAsset font, System.Text.StringBuilder log)
        {
            RectTransform rt = GigaAssetUtility.GetOrCreateRect(ui, "SpellPanel");
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-20f, 16f);
            rt.sizeDelta = new Vector2(560f, 62f);

            var bg = GigaAssetUtility.GetOrAddComponent<Image>(rt);
            bg.color = new Color(0.08f, 0.09f, 0.12f, 0.72f);

            var layout = GigaAssetUtility.GetOrAddComponent<HorizontalLayoutGroup>(rt);
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var panel = GigaAssetUtility.GetOrAddComponent<SpellPanelUI>(rt);
            var so = new SerializedObject(panel);
            so.FindProperty("_slotsContainer").objectReferenceValue = rt;
            if (font != null) so.FindProperty("_fontOverride").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
            log.AppendLine("[HUD] Wired SpellPanelUI bottom-right.");
        }

        private static void WireInRunShopModal(Transform ui, BuildingPoolConfig poolConfig, TMP_FontAsset font, System.Text.StringBuilder log)
        {
            RectTransform controllerRt = GigaAssetUtility.GetOrCreateRect(ui, "InRunShopSystem");
            controllerRt.anchorMin = Vector2.zero;
            controllerRt.anchorMax = Vector2.one;
            controllerRt.offsetMin = Vector2.zero;
            controllerRt.offsetMax = Vector2.zero;
            var shopUi = GigaAssetUtility.GetOrAddComponent<InRunShopUI>(controllerRt);

            var (openBtn, openBtnText) = GigaAssetUtility.CreateOrUpdateButton(
                controllerRt, "OpenShopBtn", "Магазин",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-210f, -182f), new Vector2(220f, 44f),
                20f, new Color(0.68f, 0.48f, 0.14f, 1f), font);

            RectTransform modal = GigaAssetUtility.GetOrCreateRect(controllerRt, "ShopModalPanel");
            modal.anchorMin = new Vector2(0.5f, 0.5f);
            modal.anchorMax = new Vector2(0.5f, 0.5f);
            modal.pivot = new Vector2(0.5f, 0.5f);
            modal.anchoredPosition = Vector2.zero;
            modal.sizeDelta = new Vector2(1060f, 640f);
            var modalImg = GigaAssetUtility.GetOrAddComponent<Image>(modal);
            modalImg.color = new Color(0.10f, 0.11f, 0.15f, 0.96f);

            GigaAssetUtility.CreateOrUpdateLabel(modal, "Title", "МАГАЗИН ПЕРЕРЫВА",
                new Vector2(0.5f, 0.94f), new Vector2(0.5f, 0.94f), Vector2.zero, new Vector2(520f, 42f),
                28f, new Color(1f, 0.85f, 0.35f, 1f), font);

            GigaAssetUtility.CreateOrUpdateLabel(modal, "BuildingsSectionHeader", "ПЛАНЫ ПОСТРОЕК (мгновенное пополнение)",
                new Vector2(0.5f, 0.86f), new Vector2(0.5f, 0.86f), Vector2.zero, new Vector2(700f, 28f),
                17f, new Color(0.8f, 0.88f, 1f, 1f), font);

            var buildingCards = new InRunShopUI.OfferCardWidgets[4];
            for (int i = 0; i < 4; i++)
            {
                float anchorX = 0.145f + i * 0.236f;
                buildingCards[i] = CreateShopOfferCard(modal, $"BuildingOffer_{i}", new Vector2(anchorX, 0.64f), new Vector2(230f, 220f), new Color(0.16f, 0.20f, 0.28f, 1f), font);
            }

            GigaAssetUtility.CreateOrUpdateLabel(modal, "BonusesSectionHeader", "МАЛЫЕ БОНУСЫ",
                new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(500f, 28f),
                17f, new Color(1f, 0.82f, 0.45f, 1f), font);

            var bonusCards = new InRunShopUI.OfferCardWidgets[2];
            for (int i = 0; i < 2; i++)
            {
                float anchorX = i == 0 ? 0.32f : 0.68f;
                bonusCards[i] = CreateShopOfferCard(modal, $"BonusOffer_{i}", new Vector2(anchorX, 0.26f), new Vector2(340f, 145f), new Color(0.22f, 0.18f, 0.14f, 1f), font);
            }

            var (closeBtn, _) = GigaAssetUtility.CreateOrUpdateButton(
                modal, "CloseBtn", "Закрыть",
                new Vector2(0.5f, 0.07f), new Vector2(0.5f, 0.07f), Vector2.zero, new Vector2(220f, 44f),
                20f, new Color(0.45f, 0.20f, 0.20f, 1f), font);

            modal.gameObject.SetActive(false);

            shopUi.InitializeReferences(poolConfig, modal.gameObject, openBtn, openBtnText, closeBtn, buildingCards, bonusCards);
            EditorUtility.SetDirty(shopUi);
            log.AppendLine("[HUD] Wired InRunShopUI (OpenBtn + 4 Building + 2 Bonus cards).");
        }

        private static InRunShopUI.OfferCardWidgets CreateShopOfferCard(Transform parent, string name, Vector2 anchor, Vector2 size, Color cardColor, TMP_FontAsset font)
        {
            RectTransform rt = GigaAssetUtility.GetOrCreateRect(parent, name);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;

            var bg = GigaAssetUtility.GetOrAddComponent<Image>(rt);
            bg.color = cardColor;

            RectTransform iconRt = GigaAssetUtility.GetOrCreateRect(rt, "Icon");
            iconRt.anchorMin = new Vector2(0.5f, 0.78f);
            iconRt.anchorMax = new Vector2(0.5f, 0.78f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(44f, 44f);
            var iconImg = GigaAssetUtility.GetOrAddComponent<Image>(iconRt);
            iconImg.preserveAspect = true;

            TMP_Text title = GigaAssetUtility.CreateOrUpdateLabel(rt, "Title", "Название",
                new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(size.x - 16f, 28f),
                15f, Color.white, font);

            TMP_Text desc = GigaAssetUtility.CreateOrUpdateLabel(rt, "Desc", "Описание",
                new Vector2(0.5f, 0.40f), new Vector2(0.5f, 0.40f), Vector2.zero, new Vector2(size.x - 16f, 48f),
                12f, new Color(0.82f, 0.85f, 0.90f, 1f), font);

            var (buyBtn, priceText) = GigaAssetUtility.CreateOrUpdateButton(
                rt, "BuyBtn", "50 мон.",
                new Vector2(0.5f, 0.14f), new Vector2(0.5f, 0.14f), Vector2.zero, new Vector2(size.x - 28f, 34f),
                15f, new Color(0.18f, 0.48f, 0.28f, 1f), font);

            return new InRunShopUI.OfferCardWidgets
            {
                BuyButton = buyBtn,
                TitleText = title,
                DescriptionText = desc,
                PriceText = priceText,
                IconImage = iconImg
            };
        }

        private static void WireSampleScene(GigaContentDataGenerator.GeneratedContent data, System.Text.StringBuilder log)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            var rpm = Object.FindFirstObjectByType<RunProgressionManager>(FindObjectsInactive.Include);
            var upgDb = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>("Assets/ScriptbleObjects/Upgrades/UpgradeDatabase.asset");
            if (rpm != null && upgDb != null)
            {
                var so = new SerializedObject(rpm);
                so.FindProperty("_upgradeDatabase").objectReferenceValue = upgDb;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rpm);
            }

            // Ensure SpellScrollManager & SpellCaster exist cleanly on Managers or Bootstrapper root
            var bootstrapper = Object.FindFirstObjectByType<SceneBootstrapper>(FindObjectsInactive.Include);
            GameObject host = rpm != null ? rpm.gameObject : (bootstrapper != null ? bootstrapper.gameObject : null);
            if (host == null) host = new GameObject("SpellSystems");

            if (Object.FindFirstObjectByType<SpellScrollManager>(FindObjectsInactive.Include) == null)
            {
                host.AddComponent<SpellScrollManager>();
                log.AppendLine("[SampleScene] Added SpellScrollManager.");
            }
            if (Object.FindFirstObjectByType<SpellCaster>(FindObjectsInactive.Include) == null)
            {
                host.AddComponent<SpellCaster>();
                log.AppendLine("[SampleScene] Added SpellCaster.");
            }

            var inRunShop = Object.FindFirstObjectByType<InRunShopUI>(FindObjectsInactive.Include);
            if (inRunShop != null && data.PoolConfig != null && inRunShop.PoolConfig == null)
            {
                var so = new SerializedObject(inRunShop);
                so.FindProperty("_poolConfig").objectReferenceValue = data.PoolConfig;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(inRunShop);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("[SampleScene] Saved SampleScene.unity.");
        }

        private static void WireMainMenuScene(GigaContentDataGenerator.GeneratedContent data, System.Text.StringBuilder log)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                log.AppendLine("[WARN] MainMenu Canvas not found!");
                return;
            }

            Transform canvas = canvasGo.transform;
            TMP_FontAsset font = GigaAssetUtility.FindCyrillicFont(canvas);
            var menuMgr = canvasGo.GetComponent<MainMenuManager>();

            // 1. Update Knight & Weapon lists in Selection UIs
            var kui = canvas.GetComponentInChildren<KnightSelectionUI>(true);
            if (kui != null)
            {
                SetObjectListProperty(kui, "_allKnights", data.Knights);
                EditorUtility.SetDirty(kui);
            }
            var wui = canvas.GetComponentInChildren<WeaponSelectionUI>(true);
            if (wui != null)
            {
                SetObjectListProperty(wui, "_allWeapons", data.Weapons);
                EditorUtility.SetDirty(wui);
            }

            // 2. TitlePanel -> Settings button + lower Quit button
            Transform titlePanel = canvas.Find("TitlePanel");
            Button settingsBtn = null;
            if (titlePanel != null)
            {
                GigaAssetUtility.SetAnchorY(titlePanel.Find("QuitBtn"), 0.18f);
                (settingsBtn, _) = GigaAssetUtility.CreateOrUpdateButton(
                    titlePanel, "SettingsBtn", "Настройки",
                    new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(340f, 60f),
                    24f, new Color(0.22f, 0.30f, 0.42f, 1f), font);
            }

            // 3. SettingsPanel
            RectTransform settingsPanel = GigaAssetUtility.GetOrCreateRect(canvas, "SettingsPanel");
            settingsPanel.anchorMin = Vector2.zero;
            settingsPanel.anchorMax = Vector2.one;
            settingsPanel.offsetMin = Vector2.zero;
            settingsPanel.offsetMax = Vector2.zero;
            var spBg = GigaAssetUtility.GetOrAddComponent<Image>(settingsPanel);
            spBg.color = new Color(0.08f, 0.09f, 0.13f, 0.96f);

            GigaAssetUtility.CreateOrUpdateLabel(settingsPanel, "Header", "НАСТРОЙКИ ЗВУКА",
                new Vector2(0.5f, 0.85f), new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(600f, 55f), 34f, Color.white, font);
            GigaAssetUtility.CreateOrUpdateLabel(settingsPanel, "MusicLabel", "Громкость музыки",
                new Vector2(0.32f, 0.62f), new Vector2(0.32f, 0.62f), Vector2.zero, new Vector2(260f, 40f), 22f, Color.white, font, TextAlignmentOptions.Right);
            Slider mSlider = GigaAssetUtility.CreateOrUpdateSlider(settingsPanel, "MusicSlider",
                new Vector2(0.52f, 0.62f), new Vector2(0.52f, 0.62f), new Vector2(320f, 32f), new Color(0.25f, 0.72f, 0.95f, 1f));
            TMP_Text mVal = GigaAssetUtility.CreateOrUpdateLabel(settingsPanel, "MusicValue", "80%",
                new Vector2(0.68f, 0.62f), new Vector2(0.68f, 0.62f), Vector2.zero, new Vector2(90f, 40f), 22f, Color.white, font, TextAlignmentOptions.Left);

            GigaAssetUtility.CreateOrUpdateLabel(settingsPanel, "SfxLabel", "Громкость звуков",
                new Vector2(0.32f, 0.48f), new Vector2(0.32f, 0.48f), Vector2.zero, new Vector2(260f, 40f), 22f, Color.white, font, TextAlignmentOptions.Right);
            Slider sSlider = GigaAssetUtility.CreateOrUpdateSlider(settingsPanel, "SfxSlider",
                new Vector2(0.52f, 0.48f), new Vector2(0.52f, 0.48f), new Vector2(320f, 32f), new Color(0.95f, 0.68f, 0.22f, 1f));
            TMP_Text sVal = GigaAssetUtility.CreateOrUpdateLabel(settingsPanel, "SfxValue", "80%",
                new Vector2(0.68f, 0.48f), new Vector2(0.68f, 0.48f), Vector2.zero, new Vector2(90f, 40f), 22f, Color.white, font, TextAlignmentOptions.Left);

            var (spBackBtn, _) = GigaAssetUtility.CreateOrUpdateButton(
                settingsPanel, "BackBtn", "Назад",
                new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(260f, 55f),
                24f, new Color(0.30f, 0.32f, 0.38f, 1f), font);

            var settingsUi = GigaAssetUtility.GetOrAddComponent<SettingsUI>(settingsPanel);
            settingsUi.ConfigureReferences(mSlider, sSlider, mVal, sVal, spBackBtn, menuMgr);
            EditorUtility.SetDirty(settingsUi);
            settingsPanel.gameObject.SetActive(false);

            // Wire MainMenuManager Settings links
            if (menuMgr != null)
            {
                var mgrSo = new SerializedObject(menuMgr);
                mgrSo.FindProperty("_settingsPanel").objectReferenceValue = settingsPanel.gameObject;
                if (settingsBtn != null) mgrSo.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;
                mgrSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menuMgr);
            }

            // 4. ShopPanel 3-tab rebuild
            WireMainMenuShopPanel(canvas.Find("ShopPanel"), data, menuMgr, font, log);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("[MainMenu] Saved MainMenu.unity.");
        }

        private static void WireMainMenuShopPanel(Transform shopPanel, GigaContentDataGenerator.GeneratedContent data, MainMenuManager mgr, TMP_FontAsset font, System.Text.StringBuilder log)
        {
            if (shopPanel == null) return;
            var shopUi = shopPanel.GetComponent<ShopUI>();
            if (shopUi == null) return;

            GigaAssetUtility.SetAnchorY(shopPanel.Find("Header"), 0.90f);
            GigaAssetUtility.SetAnchorY(shopPanel.Find("Balance"), 0.81f);
            GigaAssetUtility.SetAnchorY(shopPanel.Find("BackBtn"), 0.09f);

            Transform addDia = shopPanel.Find("AddTestDiaBtn");
            if (addDia != null) addDia.gameObject.SetActive(false);

            var (bonusesBtn, _) = GigaAssetUtility.CreateOrUpdateButton(shopPanel, "TabBonusesBtn", "Бонусы",
                new Vector2(0.28f, 0.71f), new Vector2(0.28f, 0.71f), Vector2.zero, new Vector2(220f, 46f), 20f, new Color(0.24f, 0.27f, 0.34f, 1f), font);
            var (knightsBtn, _) = GigaAssetUtility.CreateOrUpdateButton(shopPanel, "TabKnightsBtn", "Рыцари",
                new Vector2(0.50f, 0.71f), new Vector2(0.50f, 0.71f), Vector2.zero, new Vector2(220f, 46f), 20f, new Color(0.24f, 0.27f, 0.34f, 1f), font);
            var (weaponsBtn, _) = GigaAssetUtility.CreateOrUpdateButton(shopPanel, "TabWeaponsBtn", "Орудия",
                new Vector2(0.72f, 0.71f), new Vector2(0.72f, 0.71f), Vector2.zero, new Vector2(220f, 46f), 20f, new Color(0.24f, 0.27f, 0.34f, 1f), font);

            // ScrollRect container for spawned rows
            RectTransform scrollRt = GigaAssetUtility.GetOrCreateRect(shopPanel, "ShopScrollView");
            scrollRt.anchorMin = new Vector2(0.14f, 0.16f);
            scrollRt.anchorMax = new Vector2(0.86f, 0.65f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            var scrollBg = GigaAssetUtility.GetOrAddComponent<Image>(scrollRt);
            scrollBg.color = new Color(0.08f, 0.09f, 0.12f, 0.75f);
            var scrollRect = GigaAssetUtility.GetOrAddComponent<ScrollRect>(scrollRt);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            RectTransform vp = GigaAssetUtility.GetOrCreateRect(scrollRt, "Viewport");
            vp.anchorMin = Vector2.zero;
            vp.anchorMax = Vector2.one;
            vp.offsetMin = new Vector2(8f, 8f);
            vp.offsetMax = new Vector2(-8f, -8f);
            GigaAssetUtility.GetOrAddComponent<RectMask2D>(vp);
            scrollRect.viewport = vp;

            RectTransform content = GigaAssetUtility.GetOrCreateRect(vp, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 400f);

            var vlg = GigaAssetUtility.GetOrAddComponent<VerticalLayoutGroup>(content);
            vlg.spacing = 10f;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = GigaAssetUtility.GetOrAddComponent<ContentSizeFitter>(content);
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = content;

            var so = new SerializedObject(shopUi);
            so.FindProperty("_bonusesTabButton").objectReferenceValue = bonusesBtn;
            so.FindProperty("_knightsTabButton").objectReferenceValue = knightsBtn;
            so.FindProperty("_weaponsTabButton").objectReferenceValue = weaponsBtn;
            so.FindProperty("_itemsContainer").objectReferenceValue = content;
            if (mgr != null) so.FindProperty("_mainMenuManager").objectReferenceValue = mgr;
            so.ApplyModifiedPropertiesWithoutUndo();

            SetObjectListProperty(shopUi, "_allKnights", data.Knights);
            SetObjectListProperty(shopUi, "_allWeapons", data.Weapons);
            EditorUtility.SetDirty(shopUi);
            log.AppendLine("[MainMenu] Wired ShopUI 3 tabs + ScrollView + 5 Knights & 5 Weapons.");
        }

        private static void SetObjectListProperty<T>(Object target, string propertyName, List<T> items) where T : Object
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.isArray)
            {
                prop.ClearArray();
                for (int i = 0; i < items.Count; i++)
                {
                    prop.InsertArrayElementAtIndex(i);
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
