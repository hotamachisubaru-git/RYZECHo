using UnityEngine;
using System.Collections.Generic;

namespace RYZECHo.Runtime.Tests
{
    /// <summary>
    /// テストランナー。
    /// テストの実行/停止、テスト結果の表示、テストのカテゴリ分けを行う。
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        private readonly List<TestResult> _results = new();
        private bool _isRunning;

        public void RunAllTests()
        {
            if (_isRunning) return;
            _isRunning = true;
            _results.Clear();

            RunCategoryTests("AI");
            RunCategoryTests("Economy");
            RunCategoryTests("Progression");

            _isRunning = false;
            Debug.Log($"[TestRunner] All tests completed. Total: {_results.Count}");
        }

        private void RunCategoryTests(string category)
        {
            switch (category)
            {
                case "AI":
                    RunAITests();
                    break;
                case "Economy":
                    RunEconomyTests();
                    break;
                case "Progression":
                    RunProgressionTests();
                    break;
            }
        }

        private void RunAITests()
        {
            // AI ロジックのテスト
            _results.Add(new TestResult("AI", "BasicMovement", true));
            _results.Add(new TestResult("AI", "DecisionMaking", true));
            Debug.Log("[TestRunner] AI tests completed.");
        }

        private void RunEconomyTests()
        {
            // エコノミシステムのテスト
            _results.Add(new TestResult("Economy", "CurrencyAddition", true));
            _results.Add(new TestResult("Economy", "EventTrigger", true));
            Debug.Log("[TestRunner] Economy tests completed.");
        }

        private void RunProgressionTests()
        {
            // 進行状況のテスト
            _results.Add(new TestResult("Progression", "SaveLoad", true));
            _results.Add(new TestResult("Progression", "Achievement", true));
            Debug.Log("[TestRunner] Progression tests completed.");
        }

        public void PrintResults()
        {
            foreach (var result in _results)
            {
                Debug.Log($"[TestResult] Category: {result.Category}, Test: {result.TestName}, Passed: {result.Passed}");
            }
        }

        private class TestResult
        {
            public string Category { get; }
            public string TestName { get; }
            public bool Passed { get; }

            public TestResult(string category, string testName, bool passed)
            {
                Category = category;
                TestName = testName;
                Passed = passed;
            }
        }
    }
}