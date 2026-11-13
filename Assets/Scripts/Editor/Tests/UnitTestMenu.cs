using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Game.Editor.Tests
{
    public static class UnitTestMenu
    {
        private const string TestsGroupRegex = @"^Game\.Editor\.Tests\.";

        [MenuItem("Tools/Unit Tests", false, 1)]
        public static void RunAllUnitTests()
        {
            RunTests("All Unit Tests", category: null);
        }

        [MenuItem("Tools/Unit Tests - By Scope/Enemies & State Machine", false, 2)]
        public static void RunEnemiesTests()
        {
            RunTests("Enemies & State Machine", "Enemies");
        }

        [MenuItem("Tools/Unit Tests - By Scope/Object Pools (Enemy & Ally)", false, 3)]
        public static void RunPoolsTests()
        {
            RunTests("Object Pools (Enemy & Ally)", "Pools");
        }

        [MenuItem("Tools/Unit Tests - By Scope/Spells (Strategy & Scrolls)", false, 4)]
        public static void RunSpellsTests()
        {
            RunTests("Spells (Strategy & Scrolls)", "Spells");
        }

        [MenuItem("Tools/Unit Tests - By Scope/Upgrades & Modifiers", false, 5)]
        public static void RunUpgradesTests()
        {
            RunTests("Upgrades & Modifiers", "Upgrades");
        }

        [MenuItem("Tools/Unit Tests - By Scope/Buildings, Wallet & Grid", false, 6)]
        public static void RunBuildingsAndCoreTests()
        {
            RunTests("Buildings, Wallet & Grid", "BuildingsAndCore");
        }

        [MenuItem("Tools/Unit Tests - By Scope/Waves, Data & Scene Integrity", false, 7)]
        public static void RunWavesAndSceneTests()
        {
            RunTests("Waves, Data & Scene Integrity", "WavesAndScene");
        }

        [MenuItem("Tools/Unit Tests - By Scope/Open Unity Test Runner", false, 30)]
        public static void OpenUnityTestRunner()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
        }

        public static void RunTests(string scopeLabel, string category)
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var collector = new ResultCollector(scopeLabel);
            api.RegisterCallbacks(collector);

            try
            {
                var filter = new Filter
                {
                    testMode = TestMode.EditMode,
                    groupNames = new[] { TestsGroupRegex },
                    categoryNames = string.IsNullOrEmpty(category) ? null : new[] { category }
                };

                var settings = new ExecutionSettings(filter)
                {
                    runSynchronously = true
                };

                api.Execute(settings);
            }
            finally
            {
                api.UnregisterCallbacks(collector);
                Object.DestroyImmediate(api);
            }
        }

        private sealed class ResultCollector : ICallbacks
        {
            private readonly string _scopeLabel;
            private readonly List<ITestResultAdaptor> _failures = new();

            public ResultCollector(string scopeLabel)
            {
                _scopeLabel = scopeLabel;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
                var sb = new StringBuilder();
                sb.AppendLine($"=== Unit Tests [{_scopeLabel}] ===");
                sb.AppendLine($"Passed: {result.PassCount}/{total} | Failed: {result.FailCount} | Skipped: {result.SkipCount} | Duration: {result.Duration:0.000}s");

                for (int i = 0; i < _failures.Count; i++)
                {
                    ITestResultAdaptor fail = _failures[i];
                    sb.AppendLine($"[FAIL] {fail.Test.FullName}");
                    if (!string.IsNullOrEmpty(fail.Message))
                    {
                        sb.AppendLine($"       {fail.Message}");
                    }
                }

                if (result.FailCount > 0 || total == 0)
                {
                    if (total == 0)
                    {
                        sb.AppendLine("[FAIL] No matching unit tests were executed.");
                    }
                    Debug.LogError(sb.ToString());
                }
                else
                {
                    sb.AppendLine("=== ALL UNIT TESTS PASSED ===");
                    Debug.Log(sb.ToString());
                }
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.Test.IsSuite && result.TestStatus == TestStatus.Failed)
                {
                    _failures.Add(result);
                }
            }
        }
    }
}
