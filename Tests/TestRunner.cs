using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;

namespace RYZECHo.Tests;

/// <summary>
/// テスト結果の集計と表示を行うテストランナー。
/// </summary>
internal sealed class TestRunner
{
    private readonly List<TestResult> _results = new();
    private int _totalTests;
    private int _passed;
    private int _failed;
    private int _skipped;
    private bool _running;

    public bool IsRunning => _running;
    public int TotalTests => _totalTests;
    public int Passed => _passed;
    public int Failed => _failed;
    public int Skipped => _skipped;
    public IReadOnlyList<TestResult> Results => _results;

    public void StartRun()
    {
        _results.Clear();
        _totalTests = 0;
        _passed = 0;
        _failed = 0;
        _skipped = 0;
        _running = true;
    }

    public void StopRun()
    {
        _running = false;
    }

    public void AddResult(TestResult result)
    {
        _results.Add(result);
        _totalTests++;
        switch (result.Status)
        {
            case TestStatus.Passed: _passed++; break;
            case TestStatus.Failed: _failed++; break;
            case TestStatus.Skipped: _skipped++; break;
        }
    }

    public void RunTest(string name, Action testAction, string category = "General")
    {
        if (!_running) return;

        try
        {
            testAction();
            AddResult(new TestResult(name, TestStatus.Passed, category, null));
        }
        catch (Exception ex)
        {
            AddResult(new TestResult(name, TestStatus.Failed, category, ex.Message));
        }
    }

    public void RunTest(string name, Func<bool> testFunc, string category = "General")
    {
        if (!_running) return;

        try
        {
            if (!testFunc())
            {
                AddResult(new TestResult(name, TestStatus.Failed, category, "Assertion failed"));
            }
            else
            {
                AddResult(new TestResult(name, TestStatus.Passed, category, null));
            }
        }
        catch (Exception ex)
        {
            AddResult(new TestResult(name, TestStatus.Failed, category, ex.Message));
        }
    }

    public string GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Test Report ===");
        sb.AppendLine($"Total: {_totalTests} | Passed: {_passed} | Failed: {_failed} | Skipped: {_skipped}");
        sb.AppendLine();

        var categories = _results.GroupBy(r => r.Category).OrderBy(g => g.Key).ToList();
        foreach (var group in categories)
        {
            sb.AppendLine($"--- [{group.Key}] ---");
            foreach (var result in group)
            {
                var icon = result.Status switch
                {
                    TestStatus.Passed => "[PASS]",
                    TestStatus.Failed => "[FAIL]",
                    TestStatus.Skipped => "[SKIP]",
                    _ => "[??]"
                };
                sb.AppendLine($"  {icon} {result.Name}");
                if (result.Status == TestStatus.Failed && !string.IsNullOrEmpty(result.ErrorMessage))
                {
                    sb.AppendLine($"        Error: {result.ErrorMessage}");
                }
            }
            sb.AppendLine();
        }

        var overall = _failed == 0 ? "ALL PASSED" : $"{_failed} test(s) FAILED";
        sb.AppendLine($"=== {overall} ===");
        return sb.ToString();
    }

    public string GenerateCategorySummary()
    {
        var sb = new StringBuilder();
        var categories = _results.GroupBy(r => r.Category).OrderBy(g => g.Key).ToList();
        foreach (var group in categories)
        {
            var passed = group.Count(r => r.Status == TestStatus.Passed);
            var failed = group.Count(r => r.Status == TestStatus.Failed);
            sb.AppendLine($"  [{group.Key}] {passed}/{group.Count()} passed");
        }
        return sb.ToString();
    }
}

/// <summary>
/// テスト結果のステータス。
/// </summary>
internal enum TestStatus
{
    Passed,
    Failed,
    Skipped,
}

/// <summary>
/// 単一テストの実行結果。
/// </summary>
internal readonly record struct TestResult(
    string Name,
    TestStatus Status,
    string Category,
    string? ErrorMessage);
