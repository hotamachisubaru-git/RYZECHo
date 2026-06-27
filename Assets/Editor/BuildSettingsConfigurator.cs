using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System.IO;
using System.Linq;

/// <summary>
/// T026 - ビルドとデプロイの設定
/// 開発ビルドと本番ビルドのビルド設定を自動構成するエディタ拡張
/// </summary>
public class BuildSettingsConfigurator : EditorWindow
{
    // ============================================================
    // プロジェクトメタデータ
    // ============================================================
    private const string COMPANY_NAME   = "Team RYZECHo";
    private const string PRODUCT_NAME   = "RYZECHo";
    private const string VERSION        = "0.1.0";
    private const string BUNDLE_ID      = "com.ryzecho.game";

    // ============================================================
    // スクリーン解像度
    // ============================================================
    private const int DEFAULT_WIDTH  = 1920;
    private const int DEFAULT_HEIGHT = 1080;

    // ============================================================
    // 開発ビルド設定
    // ============================================================
    private static readonly BuildOptions DEV_OPTIONS = BuildOptions.Development | BuildOptions.StrictMode;

    // ============================================================
    // 本番ビルド設定
    // ============================================================
    private static readonly BuildOptions RELEASE_OPTIONS = BuildOptions.None;

    // ============================================================
    // ビルド出力先
    // ============================================================
    private static readonly string BUILD_OUTPUT_DIR = "Builds";

    // ============================================================
    // Player Settings 一括設定
    // ============================================================
    [MenuItem("Tools/RYZECHo/Build/Configure Project Settings")]
    public static void ConfigureProjectSettings()
    {
        // --- Company / Product / Version ---
        PlayerSettings.companyName = COMPANY_NAME;
        PlayerSettings.productName = PRODUCT_NAME;
        PlayerSettings.bundleVersion = VERSION;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_Standard_2_0);

        // --- Bundle ID ---
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, BUNDLE_ID);

        // --- Rendering ---
        PlayerSettings.colorSpace = ColorSpace.Gamma;

        // --- Resolution ---
        PlayerSettings.defaultScreenWidth  = DEFAULT_WIDTH;
        PlayerSettings.defaultScreenHeight = DEFAULT_HEIGHT;
        PlayerSettings.fullScreenMode       = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow      = true;
        PlayerSettings.runInBackground      = true;

        // --- Other ---
        PlayerSettings.usePlayerLog = true;
        PlayerSettings.visibleInBackground = false;
        PlayerSettings.allowFullscreenSwitch = true;

        // --- Android ---
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

        // --- iOS ---
        PlayerSettings.iOS.targetOSVersionString = "15.0";

        // --- Scripting Define Symbols ---
        AddDefineSymbol("RYZECHO_BUILD");

        EditorUtility.DisplayDialog("Build Settings",
            "Project settings configured successfully!\n\n" +
            "Company: "    + COMPANY_NAME + "\n" +
            "Product: "    + PRODUCT_NAME + "\n" +
            "Version: "    + VERSION + "\n" +
            "Bundle ID: "  + BUNDLE_ID + "\n" +
            "Resolution: " + DEFAULT_WIDTH + "x" + DEFAULT_HEIGHT,
            "OK");

        Debug.Log("[BuildSettingsConfigurator] Project settings configured.");
    }

    // ============================================================
    // 開発ビルド実行
    // ============================================================
    [MenuItem("Tools/RYZECHo/Build/Dev Build (Windows64)")]
    public static void DevBuild()
    {
        ConfigureProjectSettings();

        var buildPath = GetBuildPath("RYZECHo_Dev");

        BuildPipeline.BuildPlayer(
            GetScenePaths(),
            buildPath,
            BuildTarget.StandaloneWindows64,
            DEV_OPTIONS
        );

        Debug.Log("[BuildSettingsConfigurator] Dev build completed: " + buildPath);
    }

    // ============================================================
    // 本番ビルド実行
    // ============================================================
    [MenuItem("Tools/RYZECHo/Build/Release Build (Windows64)")]
    public static void ReleaseBuild()
    {
        ConfigureProjectSettings();

        var buildPath = GetBuildPath("RYZECHo_Release");

        BuildPipeline.BuildPlayer(
            GetScenePaths(),
            buildPath,
            BuildTarget.StandaloneWindows64,
            RELEASE_OPTIONS
        );

        Debug.Log("[BuildSettingsConfigurator] Release build completed: " + buildPath);
    }

    // ============================================================
    // Android 開発ビルド
    // ============================================================
    [MenuItem("Tools/RYZECHo/Build/Dev Build (Android APK)")]
    public static void DevBuildAndroid()
    {
        ConfigureProjectSettings();

        var buildPath = GetBuildPath("RYZECHo_Dev_Android");

        BuildPipeline.BuildPlayer(
            GetScenePaths(),
            buildPath,
            BuildTarget.Android,
            DEV_OPTIONS
        );

        Debug.Log("[BuildSettingsConfigurator] Android dev build completed: " + buildPath);
    }

    // ============================================================
    // Android 本番ビルド
    // ============================================================
    [MenuItem("Tools/RYZECHo/Build/Release Build (Android APK)")]
    public static void ReleaseBuildAndroid()
    {
        ConfigureProjectSettings();

        var buildPath = GetBuildPath("RYZECHo_Release_Android");

        BuildPipeline.BuildPlayer(
            GetScenePaths(),
            buildPath,
            BuildTarget.Android,
            RELEASE_OPTIONS
        );

        Debug.Log("[BuildSettingsConfigurator] Android release build completed: " + buildPath);
    }

    // ============================================================
    // シーン一覧をビルド対象に追加
    // ============================================================
    [MenuItem("Tools/RYZECHo/Build/Add All Scenes to Build")]
    public static void AddAllScenesToBuild()
    {
        var scenes = AssetDatabase.FindAssets("t:Scene")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => !path.Contains("/Library/"));

        var scenesInBuild = EditorBuildSettings.scenes
            .Select(s => s.path)
            .ToHashSet();

        int added = 0;
        foreach (var scene in scenes)
        {
            if (!scenesInBuild.Contains(scene))
            {
                scenesInBuild.Add(scene);
                added++;
            }
        }

        var settings = scenesInBuild.Select((path, index) =>
            new EditorBuildSettingsScene(path, true)).ToArray();
        EditorBuildSettings.scenes = settings;

        EditorUtility.DisplayDialog("Scenes Added",
            added + " scenes added to build settings.", "OK");

        Debug.Log("[BuildSettingsConfigurator] Added " + added + " scenes to build settings.");
    }

    // ============================================================
    // ビルドパス生成
    // ============================================================
    private static string GetBuildPath(string name)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), BUILD_OUTPUT_DIR);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, name);
    }

    // ============================================================
    // シーンパス取得
    // ============================================================
    private static string[] GetScenePaths()
    {
        return EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
    }

    // ============================================================
    // Define Symbol 追加（重複防止）
    // ============================================================
    private static void AddDefineSymbol(string symbol)
    {
        var current = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
        if (!current.Split(';').Contains(symbol))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone,
                current + ";" + symbol
            );
        }
    }
}
