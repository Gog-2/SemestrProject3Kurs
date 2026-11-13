using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Enemies.Data;
using Game.WaveSystem;

namespace Game.Editor.Tests
{
    [TestFixture]
    [Category("WavesAndScene")]
    public class WavesAndSceneTests
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void EnemyDataAssets_AllFiveExistWithValidStats()
        {
            string[] enemyPaths =
            {
                "Assets/ScriptbleObjects/Enemy/Enemy.asset",
                "Assets/ScriptbleObjects/Enemy/Enemy_Fast.asset",
                "Assets/ScriptbleObjects/Enemy/Enemy_Tank.asset",
                "Assets/ScriptbleObjects/Enemy/Enemy_Archer.asset",
                "Assets/ScriptbleObjects/Enemy/Enemy_Boss.asset"
            };

            foreach (string path in enemyPaths)
            {
                EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                Assert.NotNull(data, $"Missing EnemyData asset: {path}");
                Assert.IsFalse(string.IsNullOrEmpty(data.Id), $"{path} has empty Id");
                Assert.Greater(data.MaxHP, 0, $"{path} has invalid MaxHP");
                Assert.Greater(data.MoveSpeed, 0f, $"{path} has invalid MoveSpeed");
                Assert.Greater(data.Damage, 0, $"{path} has invalid Damage");
                Assert.NotNull(data.Sprite, $"{path} is missing Sprite");
            }
        }

        [Test]
        public void WaveConfigAssets_Waves1To5ConfiguredValidly()
        {
            for (int i = 1; i <= 5; i++)
            {
                string path = i == 5
                    ? "Assets/ScriptbleObjects/WaveConfig/Wave_05_Boss.asset"
                    : $"Assets/ScriptbleObjects/WaveConfig/Wave_0{i}.asset";

                WaveConfig wc = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
                Assert.NotNull(wc, $"Missing WaveConfig asset: {path}");
                Assert.AreEqual(i, wc.WaveNumber, $"{path} has unexpected WaveNumber");
                Assert.Greater(wc.SpawnGroups.Count, 0, $"{path} has no SpawnGroups");
                Assert.Greater(wc.TotalEnemiesToSpawn, 0, $"{path} spawns 0 enemies");

                if (i == 5)
                {
                    Assert.IsTrue(wc.IsBossWave, "Wave 5 must be marked IsBossWave");
                    Assert.IsTrue(wc.IsFinalWave, "Wave 5 must be marked IsFinalWave");
                }
            }
        }

        [Test]
        public void GamePrefabs_ContainNoDangerousHierarchyHeaders()
        {
            string[] prefabs =
            {
                "Assets/Prefabs/MainGame.prefab",
                "Assets/Prefabs/HUD.prefab",
                "Assets/Prefabs/Movement.prefab"
            };

            foreach (string path in prefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.NotNull(prefab, $"Missing prefab: {path}");
                var headers = prefab.GetComponentsInChildren<Alchemy.Hierarchy.HierarchyObject>(true);
                Assert.AreEqual(0, headers.Length, $"{path} contains Alchemy.Hierarchy.HierarchyObject components that strip GameObjects at build time.");
            }
        }

        [Test]
        public void SampleScene_AllCoreSystemsAndUiBindingsPassValidation()
        {
            Scene existing = SceneManager.GetSceneByPath(SampleScenePath);
            bool openedTemporarily = false;
            if (!existing.isLoaded)
            {
                existing = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
                openedTemporarily = true;
            }

            try
            {
                string report = SceneSetupCLI.ValidateScene();
                StringAssert.Contains("=== ALL SCENE CHECKS PASSED ===", report, report);
                StringAssert.DoesNotContain("[FAIL]", report, report);
            }
            finally
            {
                if (openedTemporarily && existing.IsValid() && existing.isLoaded)
                {
                    EditorSceneManager.CloseScene(existing, removeScene: true);
                }
            }
        }
    }
}
