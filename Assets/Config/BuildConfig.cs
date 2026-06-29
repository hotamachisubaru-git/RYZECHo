using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// T026 - 開発ビルドの設定
/// 開発ビルドのパラメータ、デバッグオプション、ロギングレベルを管理する
/// バージョン管理: 変更ごとに +0.0.1 インクリメント、開発ビルドには -dev サフィックス付与
/// </summary>
[Serializable]
public class BuildConfig
{
    // ============================================================
    // 開発ビルドのパラメータ
    // ============================================================

    [Header("ビルドタイプ")]
    [Tooltip("開発ビルドを有効にする")]
    public bool EnableDevelopmentBuild = true;

#if UNITY_EDITOR
    [Tooltip("開発ビルドに含めるオプション")]
    public BuildOptions BuildOptions = BuildOptions.Development | BuildOptions.StrictMode;
#endif

    [Tooltip("テストアセンブリを含める")]
    public bool IncludeTestAssemblies = false;

    // ============================================================
    // デバッグオプションの設定
    // ============================================================

    [Header("デバッグオプション")]
    [Tooltip("デバッグログを有効にする")]
    public bool EnableDebugLogs = true;

    [Tooltip("アサートを有効にする")]
    public bool EnableAssertions = true;

    [Tooltip("スクリプトデバッグを有効にする")]
    public bool EnableScriptDebugging = true;

    [Tooltip("シンボルファイルを生成する")]
    public bool GenerateSymbolFiles = true;

    [Tooltip("追加のデバッグチェックを有効にする")]
    public bool EnableExtraChecks = true;

    [Tooltip("エラー時にゲームを一時停止する")]
    public bool PauseOnErrors = true;

    // ============================================================
    // ロギングレベルの設定
    // ============================================================

    [Header("ロギングレベル")]
    [Tooltip("開発ビルドのロギングレベル")]
    public LogType DevelopmentLogFilter = LogType.Log;

    [Tooltip("ロギングの出力先パス")]
    public string LogFilePath = "logs/development.log";

    [Tooltip("ログファイルの最大サイズ (MB)")]
    public int MaxLogFileSizeMB = 10;

    [Tooltip("ログファイルの最大世代数")]
    public int MaxLogFiles = 5;

    [Tooltip("ログにタイムスタンプを含める")]
    public bool IncludeTimestamp = true;

    [Tooltip("ログにファイル名と行番号を含める")]
    public bool IncludeFileLocation = true;

    // ============================================================
    // 開発ビルド固有の設定
    // ============================================================

    [Header("開発ビルド固有")]
    [Tooltip("開発ビルドの目標FPS (0=無制限)")]
    public int DevelopmentTargetFPS = 0;

    [Tooltip("スローモーションを有効にする")]
    public bool EnableSlowMotion = true;

    [Tooltip("スローモーション速度 (0.1〜1.0)")]
    [Range(0.1f, 1.0f)]
    public float SlowMotionSpeed = 0.25f;

    [Tooltip("開発ビルドでコンソールを自動表示する")]
    public bool AutoShowConsole = true;

    [Tooltip("開発ビルドでプロファイラーを有効にする")]
    public bool EnableProfilerWindow = true;

    // ============================================================
    // バージョン管理
    // ============================================================

    [Header("バージョン管理")]
    [Tooltip("現在のバージョン番号")]
    public string CurrentVersion = "0.1.0";

    [Tooltip("開発ビルド名に -dev サフィックスを付与する")]
    public bool EnableDevSuffix = true;

    [Tooltip("現在のビルド名（-dev付き/なし）")]
    public string BuildName => EnableDevSuffix ? $"{CurrentVersion}-dev" : CurrentVersion;

    /// <summary>バージョンを +0.0.1 インクリメントする</summary>
    public void IncrementVersion()
    {
        var parts = CurrentVersion.Split('.');
        if (parts.Length == 3 && int.TryParse(parts[2], out var minor))
        {
            parts[2] = (minor + 1).ToString();
            CurrentVersion = string.Join(".", parts);
            Debug.Log($"[BuildConfig] バージョンをインクリメント: {CurrentVersion}");
        }
        else
        {
            Debug.LogWarning($"[BuildConfig] バージョン形式が不正: {CurrentVersion}");
        }
    }

    /// <summary>PlayerSettings の productName と bundleVersion を更新する</summary>
#if UNITY_EDITOR
    public void ApplyToPlayerSettings()
    {
        var name = EnableDevSuffix ? $"{CurrentVersion}-dev" : CurrentVersion;
        PlayerSettings.productName = $"RYZECHo {name}";
        PlayerSettings.bundleVersion = CurrentVersion;
        Debug.Log($"[BuildConfig] PlayerSettings を更新: productName={PlayerSettings.productName}, bundleVersion={PlayerSettings.bundleVersion}");
    }
#endif

    // ============================================================
    // シングルトン
    // ============================================================

    private static BuildConfig _instance;
    public static BuildConfig Instance
    {
        get
        {
            if (_instance == null) _instance = new BuildConfig();
            return _instance;
        }
    }

    // ============================================================
    // メソッド
    // ============================================================

    /// <summary>現在のビルドが開発ビルドかどうか</summary>
    public bool IsDevelopmentBuild
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return EnableDevelopmentBuild;
#else
            return false;
#endif
        }
    }

    /// <summary>ログを出力すべきか判定</summary>
    public bool ShouldLog(LogType type)
    {
        if (!EnableDebugLogs) return false;
        return (int)type <= (int)DevelopmentLogFilter;
    }

    /// <summary>開発ビルド用設定を適用する</summary>
    public void ApplyDevelopmentSettings()
    {
        Application.logMessageReceived += HandleLogMessage;
        if (DevelopmentTargetFPS > 0) Application.targetFrameRate = DevelopmentTargetFPS;
        QualitySettings.vSyncCount = 0;
        Debug.unityLogger.logEnabled = EnableDebugLogs;
    }

    private void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
        if (!ShouldLog(type)) return;
        string ts = IncludeTimestamp ? $"[{DateTime.Now:HH:mm:ss.fff}] " : "";
        string loc = IncludeFileLocation && !string.IsNullOrEmpty(stackTrace)
            ? $" ({stackTrace.Split('\n')[0].Trim()})" : "";
        Debug.Log($"{ts}{condition}{loc}");
    }
}
