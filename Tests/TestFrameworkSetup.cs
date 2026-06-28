using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace RYZECHo.Tests;

/// <summary>
/// Unity Test Framework のセットアップとテスト実行環境の構築。
/// エディタ拡張としてテストフレームワークを初期化する。
/// </summary>
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace RYZECHo.Tests
{
    /// <summary>
    /// エディタ上でテストフレームワークを初期化するクラス。
    /// </summary>
    internal static class TestFrameworkSetup
    {
        /// <summary>
        /// エディタ起動時にテストフレームワークを設定する。
        /// </summary>
        [InitializeOnLoadMethod]
        internal static void Initialize()
        {
            var settings = ScriptableObject.CreateInstance<TestSettings>();
            settings.testFilterType = TestLauncherFilterType.RunAll;
            Debug.Log("[RYZECHo] Test Framework initialized.");
        }

        /// <summary>
        /// テストランナーをエディタ上で実行する。
        /// </summary>
        [MenuItem("Tools/RYZECHo/Run Tests")]
        internal static void RunAllTests()
        {
            var runner = new TestRunner();
            var aiSuite = new AITestSuite(runner);
            var economySuite = new EconomyTestSuite(runner);

            aiSuite.RunAll();
            economySuite.RunAll();

            var report = runner.GenerateReport();
            Debug.Log(report);
        }

        /// <summary>
        /// テスト結果をエディタに出力する。
        /// </summary>
        [MenuItem("Tools/RYZECHo/Show Test Results")]
        internal static void ShowResults()
        {
            var runner = new TestRunner();
            if (runner.TotalTests == 0)
            {
                Debug.LogWarning("[RYZECHo] No tests have been run yet. Run tests first.");
                return;
            }

            var report = runner.GenerateReport();
            Debug.Log(report);
        }
    }
}
#else
namespace RYZECHo.Tests
{
    /// <summary>
    /// Unity エディタ外でのスタブ実装。
    /// ゲームビルド時はテストフレームワークのセットアップをスキップする。
    /// </summary>
    internal static class TestFrameworkSetup
    {
        internal static void Initialize() { }

        [System.Obsolete("Not available in game build")]
        internal static void RunAllTests() { }
    }
}
#endif
